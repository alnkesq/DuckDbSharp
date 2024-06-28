using DuckDbSharp.Bindings;
using DuckDbSharp.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Functions
{
    internal unsafe static class DuckDbTypeCreator
    {

        public static _duckdb_logical_type* CreateLogicalType(Type t, List<Type>? typesBeingCreated)
        {
            if (LogicalTypesCache.TryGetValue(t, out var v)) return (_duckdb_logical_type*)v;

            return CreateLogicalTypeSlow(t, typesBeingCreated);
        }


        private static _duckdb_logical_type* CreateLogicalTypeSlow(Type t, List<Type>? typesBeingCreated)
        {
            if (typesBeingCreated != null)
            {
                var idx = typesBeingCreated.IndexOf(t);
                if (idx != -1)
                {
                    var cycle = CollectionsMarshal.AsSpan(typesBeingCreated).Slice(idx).ToArray();
                    throw new RecursiveTypeException($"Recursive types are not supported: {string.Join(" -> ", cycle.Select(x => x.Name))}. Consider adding a [DuckDbIgnore] attribute.", cycle);
                }
            }

            var d = LogicalTypesCache.TryGetValue(t, out var v) ? v : LogicalTypesCache.GetOrAdd(t, _ =>
            {
                typesBeingCreated ??= new();
                typesBeingCreated.Add(t);
                nint r;
                try
                {
                    r = (nint)CreateLogicalTypeCore(t, typesBeingCreated);
                }
                catch (Exception ex) when (!(ex is RecursiveTypeException))
                {
                    throw new Exception($"Error while creating DuckDB type for {t}: {ex.Message}", ex);
                }
                typesBeingCreated.RemoveAt(typesBeingCreated.Count - 1);
                return r;
            });
            return (_duckdb_logical_type*)d;
        }

        private readonly static ConcurrentDictionary<Type, nint> LogicalTypesCache = new();


        private static _duckdb_logical_type* CreateLogicalTypeCore(Type t, List<Type> typesBeingCreated)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;


            GetDuckDbType(t, null, out var primitiveType, out var sublistElementType, out _, out var structureFields, out var arrayFixedLength);
            if (primitiveType != null)
            {
                if (primitiveType.IsEnum)
                {
                    var enumInfo = primitiveType.EnumInfo;
                    fixed (byte** a = BindingUtils.ToPointerArray<ScopedString, byte>(enumInfo.Members, x => x.Pointer))
                    {
                        return Methods.duckdb_create_enum_type(a, (ulong)enumInfo.Members.Length);
                    }
                }
                else
                {
                    return Methods.duckdb_create_logical_type(primitiveType.Kind);
                }
            }


            if (arrayFixedLength != null)
            {
                return Methods.duckdb_create_array_type(CreateLogicalType(sublistElementType, typesBeingCreated), (ulong)arrayFixedLength.Value);
            }

            if (sublistElementType != null)
            {
                return Methods.duckdb_create_list_type(CreateLogicalType(sublistElementType, typesBeingCreated));
            }

            var fields = structureFields.Select(x =>
            {/*
                try
                {*/
                return (Field: x, DuckFieldType: (nint)CreateLogicalType(x.FieldType, typesBeingCreated));
                /*}
                catch (RecursiveTypeException)
                {
                    Console.Error.WriteLine("Ignoring field " + x.DuckDbFieldName + " of recursive type: " + string.Join(" -> ", typesBeingCreated));
                    return default;
                }*/
            }).Where(x => x.DuckFieldType != 0).ToArray();

            var names = fields.Select(x => (ScopedString)x.Field.DuckDbFieldName).ToArray();
            var types = fields.Select(x => x.DuckFieldType).ToArray();

            var typesArray = BindingUtils.ToPointerArray<(StructureFieldInfo Field, nint DuckDbType), _duckdb_logical_type>(fields, x => CreateLogicalType(x.Field.FieldType, typesBeingCreated));
            var namesPtrArray = BindingUtils.ToPointerArray<ScopedString, byte>(names, x => (byte*)x);
            try
            {
                fixed (_duckdb_logical_type** pTypes = typesArray)
                fixed (byte** namesPtr = namesPtrArray)
                {
                    return Methods.duckdb_create_struct_type(pTypes, namesPtr, (ulong)fields.Length);
                }
            }
            finally
            {
                foreach (var item in names)
                {
                    item.Dispose();
                }
            }

        }
        private readonly static ConcurrentDictionary<(Type ClrType, DuckDbStructuralType? StructuralType), List<FieldInfo2>> FieldsCache = new();

        internal static List<FieldInfo2> GetFields(Type t)
        {
            return GetFields(t, null)!;
        }

        internal static List<FieldInfo2?> GetFields(Type t, DuckDbStructuralType? structuralType)
        {
            if (t.IsEnum) throw new ArgumentException();
            return FieldsCache.GetOrAdd((t, structuralType), pair =>
            {
                if (SerializerCreationContext.IsWrappedSingleColumn(pair.ClrType)) throw new ArgumentException();
                var t = pair.ClrType;
                if (t == typeof(object)) throw new ArgumentException($"Type System.Object is not supported.");
                var structuralType = pair.StructuralType;

                var isTuple = t.IsAssignableTo(typeof(ITuple));
                if (isTuple && structuralType is not null)
                {
                    var argCount = t.GetGenericTypeDefinition().GetGenericArguments().Length;
                    if (argCount != structuralType.StructureFields.Count)
                        throw new ArgumentException($"Attempting to select {structuralType.StructureFields.Count} columns or fields onto a tuple type with {argCount} fields.");

                }

                var isProtoContract = t.GetCustomAttributes().Any(x => x.GetType().FullName == "ProtoBuf.ProtoContractAttribute");

                var fields = t
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => ShouldIncludeField(x, isProtoContract, x.IsPublic))
                    .Select(x => new FieldInfo2(DuckDbUtils.ToDuckCaseField(x.Name), x.FieldType, x));

                var properties = t
                    .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.GetMethod != null && ShouldIncludeField(x, isProtoContract, x.GetMethod.IsPublic) && x.GetIndexParameters().Length == 0)
                    .Select(x => new FieldInfo2(DuckDbUtils.ToDuckCaseField(x.Name), x.PropertyType, x));
                var allFields = fields.Concat(properties).ToArray();
                if (structuralType is not null)
                {
                    var byName = allFields.ToDictionary(x =>
                    {
                        var name = x.Name;
                        if (name[0] == '<' && name.EndsWith(">k__BackingField", StringComparison.Ordinal))
                        {
                            name = name.Substring(1, name.Length - 1 - ">k__BackingField".Length);
                        }
                        return name;
                    }); // TODO match proper case
                    return structuralType.StructureFields.Select((x, i) =>
                    {
                        FieldInfo2 fi;
                        if (isTuple) fi = allFields[i];
                        else if (!byName.TryGetValue(x.Name, out fi)) return null;

                        fi.FieldStructuralType = x.FieldType;
                        CheckTypeCompatibility(fi.FieldType, fi.FieldStructuralType);
                        return fi;
                    }).ToList();
                }
                else
                {
                    return allFields.OrderBy(x => x.Member.MetadataToken).ToList();
                }
            });
        }

        private static bool ShouldIncludeField(MemberInfo fieldOrProperty, bool isProtoContract, bool isPublic)
        {
            if (fieldOrProperty.GetCustomAttribute<DuckDbIncludeAttribute>() != null) return true;
            if (fieldOrProperty.GetCustomAttribute<DuckDbIgnoreAttribute>() != null) return false;
            if (isProtoContract)
            {
                return fieldOrProperty.GetCustomAttributes().Any(x => x.GetType().FullName == "ProtoBuf.ProtoMemberAttribute");
            }
            return isPublic;
        }


        internal static StructureFieldInfo[] GetFlagsEnumFields(Type nonNullableFlagsEnumType)
        {
            var processed = new bool[64];
            var result = new List<StructureFieldInfo>();
            var clrUnderlyingType = Enum.GetUnderlyingType(nonNullableFlagsEnumType);
            foreach (var item in Enum.GetValues(nonNullableFlagsEnumType))
            {
                var asulong = Convert.ToUInt64(item);
                if (!BitOperations.IsPow2(asulong)) continue;
                var shift = BitOperations.Log2(asulong);
                if (processed[shift]) continue;
                processed[shift] = true;
                var flagAsUnderlyingType = Convert.ChangeType(item, clrUnderlyingType);
                result.Add(new StructureFieldInfo(
                    DuckDbUtils.ToDuckCaseField(item.ToString()),
                    typeof(bool),
DuckDbStructuralType.BooleanStructuralType,
                    // Flags x => bool
                    (x, _) => Expression.NotEqual(Expression.And(Expression.Convert(x, clrUnderlyingType), Expression.Constant(flagAsUnderlyingType)), Expression.Default(clrUnderlyingType)),
                    // Flags x, bool val
                    (destArr, destIdx, val, _) =>
                    {
                        var dest = Expression.ArrayAccess(destArr, destIdx);
                        var ored = Expression.Or(Expression.Convert(Expression.ArrayIndex(destArr, destIdx), clrUnderlyingType), Expression.Constant(flagAsUnderlyingType));
                        return Expression.IfThen(val, Expression.Assign(dest, Expression.Convert(ored, nonNullableFlagsEnumType)));
                    },
                    new GetterKey(null, asulong), null)
                {
                    FlagEnumShift = shift
                });
            }
            return result.ToArray();

        }


        public static void GetDuckDbType(Type nonNullableType, DuckDbStructuralType? structuralType, out DuckDbPrimitiveTypeConverter? primitiveType, out Type? sublistElementType, out DuckDbStructuralType? sublistElementStructuralType, out StructureFieldInfo?[]? structureFields, out int? arrayFixedLength)
        {
            if (Nullable.GetUnderlyingType(nonNullableType) != null) throw new ArgumentException();
            if (nonNullableType == typeof(object)) throw new NotSupportedException("Type should be statically known, but it's 'System.Object'.");
            primitiveType = null;
            sublistElementType = null;
            sublistElementStructuralType = null;
            structureFields = null;
            arrayFixedLength = null;

            if (structuralType is not null)
                CheckTypeCompatibility(nonNullableType, structuralType);

            if (DuckDbUtils.IsEnum(nonNullableType))
            {
                if (nonNullableType.GetCustomAttribute<FlagsAttribute>() != null)
                {
                    if (structuralType is { Kind: DUCKDB_TYPE.DUCKDB_TYPE_VARCHAR }) throw new NotImplementedException();
                    structureFields = GetFlagsEnumFields(nonNullableType);
                    return;
                }
                //var enumConverter = GetDuckDbEnumUnderlyingType(nonNullableType);
                //var enumUnderlyingType = Enum.GetUnderlyingType(nonNullableType);
                if (structuralType is { Kind: DUCKDB_TYPE.DUCKDB_TYPE_VARCHAR })
                {
                    primitiveType = new DuckDbPrimitiveTypeConverter(nonNullableType, new EnumSerializationInfo(default, null, SerializeEnumAsStringMethod.MakeGenericMethod(nonNullableType), DeserializeEnumFromStringMethod.MakeGenericMethod(nonNullableType), null));
                    return;
                }
                var enumInfo = DuckDbPrimitiveTypeConverter.CreateEnumInfo(nonNullableType);
                primitiveType = new DuckDbPrimitiveTypeConverter(nonNullableType, enumInfo);
                return;
            }



            primitiveType = SerializerCreationContext.TryGetPrimitiveConverter(nonNullableType);
            if (primitiveType != null) return;

            sublistElementType = TypeSniffedEnumerable.TryGetEnumerableElementType(nonNullableType);
            if (sublistElementType != null)
            {
                if (structuralType is not null)
                {
                    if (structuralType.ElementType is null) throw new Exception();
                    sublistElementStructuralType = structuralType.ElementType;
                }
                return;
            }

            var fixedSizeArrayAttribute = nonNullableType.GetCustomAttribute<System.Runtime.CompilerServices.InlineArrayAttribute>();
            if (fixedSizeArrayAttribute != null)
            {
                sublistElementType = nonNullableType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single().FieldType;
                arrayFixedLength = fixedSizeArrayAttribute.Length;
                if (structuralType is not null)
                {
                    if (structuralType.ElementType is null || structuralType.FixedSizeArrayLength != fixedSizeArrayAttribute.Length) throw new Exception();
                    sublistElementStructuralType = structuralType.ElementType;
                }
                return;
            }



            var structureFieldInfos = DuckDbTypeCreator.GetFields(nonNullableType, structuralType);
            structureFields = structureFieldInfos.Select((x, i) =>
            {
                return x != null ? CreateGetter(x) : null;
            }).ToArray();

        }

        internal readonly static MethodInfo SerializeEnumAsStringMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.SerializeEnumAsString), BindingFlags.Public | BindingFlags.Static);
        internal readonly static MethodInfo DeserializeEnumFromStringMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.DeserializeEnumFromString), BindingFlags.Public | BindingFlags.Static);

        private static void ThrowIncompatibleTypesException(Type a, DuckDbStructuralType b)
        {
            throw new Exception($"Incompatible field types: {a} vs {b}.");
        }

        private static void CheckTypeCompatibility(Type a, DuckDbStructuralType b)
        {
            a = Nullable.GetUnderlyingType(a) ?? a;
            if (b.Kind is DUCKDB_TYPE.DUCKDB_TYPE_ENUM or DUCKDB_TYPE.DUCKDB_TYPE_VARCHAR && DuckDbUtils.IsEnum(a)) return;
            GetDuckDbType(a, null, out var aPrimitive, out var aElementType, out _, out var aStructureFields, out var arrayFixedLength);
            if (aPrimitive != null)
            {
                if (aPrimitive.Kind != b.Kind)
                    ThrowIncompatibleTypesException(a, b);
                return;
            }
            if (aElementType != null && arrayFixedLength == null)
            {
                if (b.Kind != DUCKDB_TYPE.DUCKDB_TYPE_LIST)
                    ThrowIncompatibleTypesException(a, b);
                CheckTypeCompatibility(aElementType, b.ElementType);
                return;
            }
            if (aElementType != null && arrayFixedLength != null)
            {
                if (b.Kind != DUCKDB_TYPE.DUCKDB_TYPE_ARRAY)
                    ThrowIncompatibleTypesException(a, b);
                if (b.FixedSizeArrayLength != arrayFixedLength)
                    ThrowIncompatibleTypesException(a, b);
                CheckTypeCompatibility(aElementType, b.ElementType);
                return;
            }
            if (aStructureFields != null)
            {
                if (b.Kind != DUCKDB_TYPE.DUCKDB_TYPE_STRUCT)
                    ThrowIncompatibleTypesException(a, b);
                if (a.IsEnum)
                {
                    var structFields = b.StructureFields;
                    var enumFlags = DuckDbTypeCreator.GetFlagsEnumFields(a);
                    if (structFields.Count != enumFlags.Length) throw new Exception($"Different number of enum flags between DuckDB structure and [Flags] enum: {a}, {b}");
                    if (structFields.Any(x => x.FieldType.Kind != DUCKDB_TYPE.DUCKDB_TYPE_BOOLEAN)) throw new Exception($"DuckDB structure corresponding to a [Flags] enum {a} should only have boolean fields: {b}");
                    var hasStructNames = structFields.Select(x => x.Name).ToHashSet();
                    var missing = enumFlags.FirstOrDefault(x => !hasStructNames.Contains(x.DuckDbFieldName));
                    if (missing != null) throw new Exception($"Missing boolean field {missing.DuckDbFieldName} for [Flags] enum {a} in DuckDB structure {b}");
                    return;
                }
                GetFields(a, b); // Will implicitly check types.
                return;
            }
            throw new UnreachableException();
        }

        internal static StructureFieldInfo CreateGetter(FieldInfo2 x)
        {
            var fieldType = x.FieldType;
            var f = x.Member as FieldInfo;
            var p = x.Member as PropertyInfo;


            return new StructureFieldInfo(DuckDbUtils.ToDuckCaseField(x.Name), x.FieldType, x.FieldStructuralType, (obj, cilctx) =>
            {

                if (f != null)
                {
                    return Expression.Field(obj, f);
                }
                else if (p != null)
                {
                    return Expression.Property(obj, p);
                }
                else throw new ArgumentException();
            }, (destArr, destIdx, val, ctx) => ctx.CreateFieldSetter(destArr, destIdx, x, val), new GetterKey(x.Member, 0), x.Member);
        }


    }
}

