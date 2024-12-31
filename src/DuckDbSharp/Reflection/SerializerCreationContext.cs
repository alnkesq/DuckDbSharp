using DuckDbSharp.Bindings;
using DuckDbSharp.Functions;
using DuckDbSharp.Types;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DuckDbSharp.Reflection
{
    internal class SerializerCreationContext
    {


        public static List<DuckDbPrimitiveTypeConverter> PrimitiveConverters = new()
        {
            new(typeof(bool), DUCKDB_TYPE.DUCKDB_TYPE_BOOLEAN),

            new(typeof(sbyte), DUCKDB_TYPE.DUCKDB_TYPE_TINYINT),
            new(typeof(byte), DUCKDB_TYPE.DUCKDB_TYPE_UTINYINT),

            new(typeof(short), DUCKDB_TYPE.DUCKDB_TYPE_SMALLINT),
            new(typeof(ushort), DUCKDB_TYPE.DUCKDB_TYPE_USMALLINT),

            new(typeof(int), DUCKDB_TYPE.DUCKDB_TYPE_INTEGER),
            new(typeof(uint), DUCKDB_TYPE.DUCKDB_TYPE_UINTEGER),

            new(typeof(long), DUCKDB_TYPE.DUCKDB_TYPE_BIGINT),
            new(typeof(ulong), DUCKDB_TYPE.DUCKDB_TYPE_UBIGINT),

            new(typeof(Int128), DUCKDB_TYPE.DUCKDB_TYPE_HUGEINT),
            new(typeof(UInt128), DUCKDB_TYPE.DUCKDB_TYPE_UHUGEINT),
            new(typeof(DuckDbUuid), DUCKDB_TYPE.DUCKDB_TYPE_UUID),
            new(typeof(Guid), DUCKDB_TYPE.DUCKDB_TYPE_UUID, nameof(SerializationHelpers.SerializeGuid), nameof(SerializationHelpers.DeserializeGuid)),
            new(typeof(double), DUCKDB_TYPE.DUCKDB_TYPE_DOUBLE),
            new(typeof(float), DUCKDB_TYPE.DUCKDB_TYPE_FLOAT),

            new(typeof(DateTime), DUCKDB_TYPE.DUCKDB_TYPE_TIMESTAMP, nameof(SerializationHelpers.SerializeTimestampMicros), nameof(SerializationHelpers.DeserializeTimestampMicros), nameof(SerializationHelpers.IsDateTimeNullish)),
            new(typeof(DateOnly), DUCKDB_TYPE.DUCKDB_TYPE_DATE, nameof(SerializationHelpers.SerializeDateOnly), nameof(SerializationHelpers.DeserializeDateOnly), nameof(SerializationHelpers.IsDateOnlyNullish)),
            new(typeof(DuckDbTimestampSeconds), DUCKDB_TYPE.DUCKDB_TYPE_TIMESTAMP_S),
            new(typeof(DuckDbTimestampMillis), DUCKDB_TYPE.DUCKDB_TYPE_TIMESTAMP_MS),
            new(typeof(DuckDbTimestampMicros), DUCKDB_TYPE.DUCKDB_TYPE_TIMESTAMP),
            new(typeof(DuckDbTimestampNanos), DUCKDB_TYPE.DUCKDB_TYPE_TIMESTAMP_NS),
            new(typeof(TimeSpan), DUCKDB_TYPE.DUCKDB_TYPE_INTERVAL, nameof(SerializationHelpers.SerializeTimespan), nameof(SerializationHelpers.DeserializeTimespan)),
            new(typeof(DuckDbInterval), DUCKDB_TYPE.DUCKDB_TYPE_INTERVAL),

            new(typeof(HttpStatusCode), DUCKDB_TYPE.DUCKDB_TYPE_INTEGER),
            new(typeof(string), DUCKDB_TYPE.DUCKDB_TYPE_VARCHAR, nameof(SerializationHelpers.SerializeString), nameof(SerializationHelpers.DeserializeString)),
            new(typeof(Uri), DUCKDB_TYPE.DUCKDB_TYPE_VARCHAR, nameof(SerializationHelpers.SerializeUri), nameof(SerializationHelpers.DeserializeUri)),
            new(typeof(byte[]), DUCKDB_TYPE.DUCKDB_TYPE_BLOB, nameof(SerializationHelpers.SerializeByteArray), nameof(SerializationHelpers.DeserializeByteArray)),
            new(typeof(Memory<byte>), DUCKDB_TYPE.DUCKDB_TYPE_BLOB, nameof(SerializationHelpers.SerializeMemory), nameof(SerializationHelpers.DeserializeMemory)),
            new(typeof(ReadOnlyMemory<byte>), DUCKDB_TYPE.DUCKDB_TYPE_BLOB, nameof(SerializationHelpers.SerializeReadOnlyMemory), nameof(SerializationHelpers.DeserializeReadOnlyMemory)),
        };

        private readonly static MethodInfo GetStructureChildVectorMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetStructureChildVector), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetSublistChildVectorMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetSublistChildVector), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetSubarrayChildVectorMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetSubarrayChildVector), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetSublistChildVectorAndReserveMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetSublistChildVectorAndReserve), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo ShowSpanForDebuggingMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.ShowSpanForDebugging), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo ThrowEnumOutOfRangeMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.ThrowEnumOutOfRange), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly MethodInfo GetVectorDataMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetVectorData), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        private static readonly MethodInfo AssignSpanItemMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.AssignSpanItem), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        private static readonly MethodInfo ReadSpanItemMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.ReadSpanItem), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        private static readonly ConstructorInfo OffsetAndCountCtor = typeof(OffsetAndCount).GetConstructor(new[] { typeof(int), typeof(int) })!;
        private readonly static MethodInfo GetChunkSizeMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetChunkSize), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo NewSkipCtorMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.NewSkipCtor), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetTotalItemsMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetTotalItems), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetArraysTotalItemsMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetArraysTotalItems), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetSublistSizeMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetSublistSize), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetSublistOffsetMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetSublistOffset), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;


        private readonly ConcurrentDictionary<SerializerCacheKey, GeneratedMethodInfo> serializerCache = new();
        private readonly ConcurrentDictionary<DeserializerCacheKey, GeneratedMethodInfo> deserializerCache = new();

        internal List<GeneratedMethodInfo>? GeneratedMethods;

        private GeneratedMethodInfo CreateFieldDeserializer(Type outputArrayElementType, DuckDbStructuralType outputArrayStructuralType, StructureFieldInfo? getter)
        {
            var key = new DeserializerCacheKey(outputArrayElementType, outputArrayStructuralType, getter?.CacheKey ?? default, true);
            if (!deserializerCache.TryGetValue(key, out var deleg))
            {
                return deserializerCache.GetOrAdd(key, key =>
                {
                    return CreateFieldDeserializerCore(outputArrayElementType, outputArrayStructuralType, getter);
                });
            }
            return deleg;
        }


        internal GeneratedMethodInfo CreateFieldSerializer(Type inputArrayElementType, StructureFieldInfo? getter)
        {
            var key = new SerializerCacheKey(inputArrayElementType, getter?.CacheKey ?? default, true);
            if (!serializerCache.TryGetValue(key, out var deleg))
            {
                return serializerCache.GetOrAdd(key, key => CreateFieldSerializerCore(inputArrayElementType, getter));
            }
            return deleg;
        }

        private readonly static MethodInfo IsPresentMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.IsPresent), BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo IsDefaultStructValueMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.IsDefaultStructValue), BindingFlags.Public | BindingFlags.Static);

        public (Expression GetValue, Expression? HasValue) UnwrapNullable(Expression? parentValidity, Expression idx, Expression maybeNullable, StructureFieldInfo? getter, MethodInfo? isNullish = null)
        {
            var hasParent = parentValidity != null ? Expression.Call(IsPresentMethod, parentValidity, idx) : null;
            Expression? hasInner = null;
            var (getValue, hasOuter) = UnwrapNullableCore(maybeNullable);
            if (getter != null)
            {
                (getValue, hasInner) = UnwrapNullableCore(getter.CreateGetExpression(getValue, this));
            }
            if (isNullish == null)
            {
                var innerType = getValue.Type;
                if (IsDefaultIsNullishValueType(innerType))
                    isNullish = IsDefaultStructValueMethod.MakeGenericMethod(innerType);
            }

            return (getValue, AndConditions(hasParent, hasOuter, hasInner, isNullish != null ? Expression.Not(Expression.Call(null, isNullish, getValue)) : null));
        }

        internal static bool IsDefaultIsNullishValueType(Type innerType)
        {
            return innerType.IsValueType &&
                (innerType.GetCustomAttribute<DuckDbDefaultValueIsNullishAttribute>() != null ||
                false);
        }

        public static (Expression GetValue, Expression? HasValue) UnwrapNullableCore(Expression maybeNullable)
        {
            Expression? hasValue;
            if (maybeNullable.Type.IsClass)
            {
                hasValue = Expression.Not(Expression.ReferenceEqual(maybeNullable, Expression.Constant(null, maybeNullable.Type)));
            }
            else if (Nullable.GetUnderlyingType(maybeNullable.Type) is { } nonNullableInputType)
            {

                hasValue = Expression.Property(maybeNullable, "HasValue");
                maybeNullable = Expression.Property(maybeNullable, "Value");
            }
            else
            {
                hasValue = null;
            }
            return (maybeNullable, hasValue);
        }


        private static Expression? AndConditions(params Expression?[] conditions)
        {
            conditions = conditions.Where(x => x != null).ToArray();
            return conditions.Length switch
            {
                0 => null,
                1 => conditions[0],
                2 => Expression.AndAlso(conditions[0]!, conditions[1]!),
                3 => Expression.AndAlso(Expression.AndAlso(conditions[0]!, conditions[1]!), conditions[2]!),
                4 => Expression.AndAlso(Expression.AndAlso(Expression.AndAlso(conditions[0]!, conditions[1]!), conditions[2]!), conditions[3]!),
                _ => throw new Exception()
            };
        }

        private GeneratedMethodInfo CreateFieldSerializerCore(Type inputArrayElementType, StructureFieldInfo? getter)
        {
            try
            {
                var inputArrayNonNullableElementType = Nullable.GetUnderlyingType(inputArrayElementType) ?? inputArrayElementType;
                var type = getter != null ? getter.FieldType : inputArrayElementType;
                var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
                DuckDbTypeCreator.GetDuckDbType(nonNullableType, null, out var primitiveType, out var sublistElementType, out _, out var structureFields, out var arrayFixedLength);
                Func<SerializerParameters, Expression> factory;
                if (primitiveType != null)
                {
                    factory = p => CreatePrimitiveFieldSerializer(p, getter, primitiveType);
                }
                else if (sublistElementType != null)
                {
                    factory = p => CreateListFieldSerializer(p, getter, sublistElementType, arrayFixedLength);
                }
                else
                {
                    if (getter != null)
                    {
                        factory = p => CreateStructureSelector(p, getter, structureFields);
                    }
                    else
                    {
                        factory = p => CreateStructureSerializer(p, structureFields);
                    }
                }
                return CreateSerializer(inputArrayElementType, "SerializeField_", factory, getter);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while creating a serializer for {inputArrayElementType}{(getter != null ? ("->" + getter.DuckDbFieldName) : null)}: {ex.Message}", ex);
            }

        }

        private GeneratedMethodInfo CreateFieldDeserializerCore(Type outputArrayElementType, DuckDbStructuralType outputArrayStructuralType, StructureFieldInfo? getter)
        {
            try
            {
                var outputArrayNonNullableElementType = Nullable.GetUnderlyingType(outputArrayElementType) ?? outputArrayElementType;
                var type = getter != null ? getter.FieldType : outputArrayElementType;
                var structuralType = getter != null ? getter.FieldStructuralType : outputArrayStructuralType;
                var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
                DuckDbTypeCreator.GetDuckDbType(nonNullableType, structuralType, out var primitiveType, out var sublistElementType, out var sublistElementStructuralType, out var structureFields, out var arrayFixedLength);
                Func<DeserializerParameters, Expression> factory;
                if (primitiveType != null)
                {
                    factory = p => CreatePrimitiveFieldDeserializer(p, getter, primitiveType);
                }
                else if (sublistElementType != null)
                {
                    factory = p => CreateListFieldDeserializer(p, getter, sublistElementType, sublistElementStructuralType, arrayFixedLength);
                }
                else
                {

                    if (getter != null)
                    {
                        factory = p => CreateStructureDeselector(p, getter, structureFields);
                    }
                    else
                    {
                        factory = p => CreateNonNullStructureDeserializer(p, structureFields);
                    }
                }
                return CreateDeserializer(outputArrayElementType, "DeserializeField_", outputArrayStructuralType, factory, getter);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while creating a deserializer for {outputArrayElementType}{(getter != null ? ("->" + getter.DuckDbFieldName) : null)}: {ex.Message}", ex);
            }

        }

        private Expression CreateNonNullStructureDeserializer(DeserializerParameters p, StructureFieldInfo?[] fields)
        {
            var body = new List<Expression>();

            for (int fieldId = 0; fieldId < fields.Length; fieldId++)
            {
                var field = fields[fieldId];
                if (field != null)
                {
                    var deserializer = CreateFieldDeserializer(p.OutputArrayElementType, p.OutputArrayStructuralType, field);
                    body.Add(CreateCall(deserializer, Expression.Call(null, GetStructureChildVectorMethod, p.VectorPtr, Expression.Constant((ulong)fieldId)), p.Objects, p.ObjectsLength, p.DeserializationContext));
                }

            }
            return Expression.Block(typeof(void), body);
        }

        private static Expression CreateForLoop(Expression idx, Expression length, Expression body)
        {
            var breakLoop = Expression.Label();
            return Expression.Block(Expression.Assign(idx, Expression.Constant(0)), Expression.Loop(
                Expression.Block(
                    Expression.IfThen(Expression.GreaterThanOrEqual(idx, length), Expression.Break(breakLoop)),
                    body,
                    Expression.PostIncrementAssign(idx)
                )
            , breakLoop));
        }

        private Expression CreateListFieldSerializer(SerializerParameters p, StructureFieldInfo? getter, Type sublistElementType, int? arrayFixedLength)
        {
            var isFixedArray = arrayFixedLength != null;
            if (isFixedArray) throw new NotImplementedException("TODO: nullable fixed-size array");
            var sublistType = getter != null ? getter.FieldType : p.InputArrayElementType;
            sublistType = Nullable.GetUnderlyingType(sublistType) ?? sublistType;
            var offsetsAndLengths = isFixedArray ? null : Expression.Variable(typeof(Span<>).MakeGenericType(typeof(OffsetAndCount)), "offsetsAndLengths");
            var totalCount = Expression.Variable(typeof(int), "totalCount");
            var rowId = Expression.Variable(typeof(int), "rowId");
            var innerRelIdx = Expression.Variable(typeof(int), "innerRelIdx");
            var innerAbsIdx = Expression.Variable(typeof(int), "innerAbsIdx");
            var sublist = Expression.Variable(sublistType, "sublist");
            var sublistLength = Expression.Variable(typeof(int), "sublistLength");
            var subobjects = Expression.Variable(sublistElementType.MakeArrayType(), "subobjects");
            var body = new List<Expression>();
            body.Add(Expression.Assign(totalCount, Expression.Constant(0)));
            if (!isFixedArray)
                body.Add(Expression.Assign(offsetsAndLengths, Expression.Call(null, GetVectorDataMethod.MakeGenericMethod(typeof(OffsetAndCount)), p.VectorPtr, p.ObjectsLength)));

            var variables = new[]
            {
                totalCount,
                rowId,
                subobjects,
                innerRelIdx,
                innerAbsIdx
            }.ToList();
            if (!isFixedArray)
            {
                variables.Add(sublist);
                variables.Add(sublistLength);
                variables.Add(offsetsAndLengths);
            }

            var inputObject = Expression.ArrayIndex(p.Objects, rowId);
            var (inputSublistExpr, hasInputSublist) = UnwrapNullable(p.ParentValidity, rowId, inputObject, getter);

            ParameterExpression? validityVector = null;
            if (hasInputSublist != null)
            {
                validityVector = Expression.Variable(typeof(nint), "validityVector");
                variables.Add(validityVector);
                body.Add(SerializerCreationContext.CreateValidityVectorInitialization(validityVector, p.VectorPtr));
            }

            if (isFixedArray)
            {
                var offsetsLoopBody = SerializerCreationContext.MaybeIf(hasInputSublist, Expression.Block(
                        //Expression.Call(null, AssignSpanItemMethod.MakeGenericMethod(typeof(OffsetAndCount)), offsetsAndLengths, rowId, Expression.New(OffsetAndCountCtor, totalCount, sublistLength)),
                        Expression.AddAssignChecked(totalCount, Expression.Constant(arrayFixedLength.Value))
                    ),
                    CreateSetNotPresent(validityVector, rowId));

                body.Add(SerializerCreationContext.CreateForLoop(rowId, p.ObjectsLength, offsetsLoopBody));
            }
            else
            {
                var offsetsLoopBody = SerializerCreationContext.MaybeIf(hasInputSublist, Expression.Block(
                        Expression.Assign(sublist, inputSublistExpr),
                        Expression.Assign(sublistLength, GetListCountExpression(sublist)),
                        Expression.Call(null, AssignSpanItemMethod.MakeGenericMethod(typeof(OffsetAndCount)), offsetsAndLengths, rowId, Expression.New(OffsetAndCountCtor, totalCount, sublistLength)),
                        Expression.AddAssignChecked(totalCount, sublistLength)
                    ),
                    CreateSetNotPresent(validityVector, rowId));

                body.Add(SerializerCreationContext.CreateForLoop(rowId, p.ObjectsLength, offsetsLoopBody));
            }
            body.Add(Expression.Assign(subobjects, CreateRentArrayExpression(sublistElementType, totalCount)));
            body.Add(Expression.Assign(innerAbsIdx, Expression.Constant(0)));

            Expression loopBodyInner;
            if (isFixedArray)
            {

                loopBodyInner = Expression.Block(
                    Expression.Call(CopyFromFixedLengthArrayMethod.MakeGenericMethod(sublistType, sublistElementType), inputSublistExpr, subobjects, innerAbsIdx, Expression.Constant(arrayFixedLength.Value)),
                    Expression.AddAssignChecked(innerAbsIdx, Expression.Constant(arrayFixedLength.Value))
                ); 
            }
            else
            {
                loopBodyInner = Expression.Block(
                    Expression.Assign(sublist, inputSublistExpr),
                    Expression.Assign(sublistLength, GetListCountExpression(sublist)),
                    CreateForLoop(innerRelIdx, sublistLength,
                        Expression.Assign(Expression.ArrayAccess(subobjects, Expression.PostIncrementAssign(innerAbsIdx)), CreateListItemExpression(sublist, innerRelIdx))
                    )
                );
            }

            

            body.Add(SerializerCreationContext.CreateForLoop(rowId, p.ObjectsLength, Expression.Block(
                Expression.IfThen(hasInputSublist, loopBodyInner)
           )));
            var sublistItemSerializer = CreateFieldSerializer(sublistElementType, null);
            body.Add(CreateCall(sublistItemSerializer, Expression.Call(null, GetSublistChildVectorAndReserveMethod, p.VectorPtr, totalCount), subobjects, totalCount, Expression.Constant((nint)0), p.Arena));
            //body.Add(Expression.Call(null, ShowSpanForDebuggingMethod, offsetsAndLengths));
            body.Add(CreateReleaseArrayExpression(subobjects));
            return Expression.Block(variables, body);

        }

        private readonly static MethodInfo RentArrayMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.RentArray), BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo RentArrayZeroedMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.RentArrayZeroed), BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo ReleaseArrayMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.ReleaseArray), BindingFlags.Public | BindingFlags.Static);

        private static Expression CreateRentArrayExpression(Type elementType, Expression minLength, bool zeroed = false)
        {
            return Expression.Call((zeroed ? RentArrayZeroedMethod : RentArrayMethod).MakeGenericMethod(elementType), minLength);
        }


        private static Expression CreateReleaseArrayExpression(Expression array)
        {
            return Expression.Call(ReleaseArrayMethod.MakeGenericMethod(array.Type.GetElementType()), array);
        }

        private readonly static MethodInfo CreateArrayMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.CreateArray), BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo CopyToFixedLengthArrayMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.CopyToFixedLengthArray), BindingFlags.Public | BindingFlags.Static);
        private readonly static MethodInfo CopyFromFixedLengthArrayMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.CopyFromFixedLengthArray), BindingFlags.Public | BindingFlags.Static);

        private Expression CreateListFieldDeserializer(DeserializerParameters p, StructureFieldInfo? getter, Type sublistElementType, DuckDbStructuralType sublistStructuralElementType, int? fixedArrayLength)
        {
            var sublistType = getter != null ? getter.FieldType : p.OutputArrayElementType;
            sublistType = Nullable.GetUnderlyingType(sublistType) ?? sublistType;
            var variables = new List<ParameterExpression>();
            var isFixedArray = fixedArrayLength != null;
            var offsetsAndLengths = isFixedArray ? null : Expression.Variable(typeof(Span<OffsetAndCount>), "offsetsAndLengths");
            //var offsets = isFixedArray ? Expression.Variable(typeof(Span<ulong>), "offsets") : null;
            var allSubItems = Expression.Variable(sublistElementType.MakeArrayType(), "allSubItems");
            var listValidityVector = Expression.Variable(typeof(nint), "listValidityVector");
            var elementValidityVector = Expression.Variable(typeof(nint), "elementValidityVector");
            var rowId = Expression.Variable(typeof(int), "rowId");
            var sublist = Expression.Variable(sublistType, "sublist");
            var j = Expression.Variable(typeof(int), "j");
            var absIdx = Expression.Variable(typeof(int), "absIdx");
            var totalLength = Expression.Variable(typeof(int), "totalLength");
            variables.Add(allSubItems);
            variables.Add(listValidityVector);
            variables.Add(elementValidityVector);
            variables.Add(rowId);
            if (!isFixedArray)
            {
                variables.Add(offsetsAndLengths);
                variables.Add(sublist);
                variables.Add(j);
            }
            variables.Add(absIdx);
            variables.Add(totalLength);
            var sublistVector = Expression.Call(null, isFixedArray ? GetSubarrayChildVectorMethod : GetSublistChildVectorMethod, p.VectorPtr);
            var assignment = CreateAssignment(getter, p.Objects, rowId, sublist);
            var body = new List<Expression>();
            body.Add(Expression.Assign(listValidityVector, Expression.Call(null, GetVectorValidityMethod, p.VectorPtr)));
            body.Add(Expression.Assign(elementValidityVector, Expression.Call(null, GetVectorValidityMethod, sublistVector)));
            if (isFixedArray)
            {
                //body.Add(Expression.Assign(offsets, Expression.Call(null, GetVectorDataMethod.MakeGenericMethod(typeof(ulong)), p.VectorPtr, p.ObjectsLength)));
            }
            else
            {
                body.Add(Expression.Assign(offsetsAndLengths, Expression.Call(null, GetVectorDataMethod.MakeGenericMethod(typeof(OffsetAndCount)), p.VectorPtr, p.ObjectsLength)));
            }
            body.Add(Expression.Assign(totalLength, isFixedArray ? Expression.Call(null, GetArraysTotalItemsMethod, p.ObjectsLength, listValidityVector, Expression.Constant((ulong)fixedArrayLength.Value)) : Expression.Call(null, GetTotalItemsMethod, offsetsAndLengths, listValidityVector)));
            body.Add(Expression.Assign(allSubItems, CreateRentArrayExpression(sublistElementType, totalLength, zeroed: true)));
            var needsInitialization = NeedsInitialization(sublistElementType);
            if (needsInitialization)
                body.Add(CreateInitializationLoop(allSubItems, sublistElementType, rowId, totalLength, Expression.Call(null, IsPresentMethod, elementValidityVector, rowId)));

            var sublistItemDeserializer = CreateFieldDeserializer(sublistElementType, sublistStructuralElementType, null);
            body.Add(CreateCall(sublistItemDeserializer, sublistVector, allSubItems, totalLength, p.DeserializationContext));

            var copyFrom = Expression.ArrayIndex(allSubItems, absIdx);
            var sublistSizeExpr = isFixedArray ? null : Expression.Call(null, GetSublistSizeMethod, offsetsAndLengths, rowId);
            Expression newSublist;
            Expression addToSublist;
            var sublistSize = Expression.Variable(typeof(int), "sublistSize");
            if (!isFixedArray)
                variables.Add(sublistSize);

            if (isFixedArray)
            {
                newSublist = null;// Expression.New(sublist.Type);
                addToSublist = null; // Expression.Call(null, SetFixedLengthArrayItem());
            }
            else if (sublist.Type.IsArray)
            {
                newSublist = Expression.Call(CreateArrayMethod.MakeGenericMethod(sublistElementType), sublistSize);
                addToSublist = Expression.Assign(Expression.ArrayAccess(sublist, j), copyFrom);
            }
            else
            {
                newSublist = Expression.New(sublist.Type.GetConstructor(new[] { typeof(int) })!, sublistSize);
                addToSublist = Expression.Call(sublist, "Add", null, copyFrom);
            }

            Expression loopBodyInner;
            if (isFixedArray)
            {
                loopBodyInner = Expression.Block(
                    CreateAssignment(getter, p.Objects, rowId, Expression.Call(CopyToFixedLengthArrayMethod.MakeGenericMethod(sublistType, sublistElementType), allSubItems, absIdx, Expression.Constant(fixedArrayLength.Value))),
                    Expression.Assign(absIdx, Expression.Add(absIdx, Expression.Constant(fixedArrayLength.Value)))
                    );
            }
            else
            {
                loopBodyInner =
                    Expression.Block(
                            Expression.Assign(sublistSize, sublistSizeExpr),
                            Expression.Assign(sublist, newSublist),
                            Expression.Assign(absIdx, Expression.Call(null, GetSublistOffsetMethod, offsetsAndLengths, rowId)),
                            CreateForLoop(j, sublistSize,
                            Expression.Block(
                                addToSublist,
                                Expression.PostIncrementAssign(absIdx))
                            ),
                            assignment
                );
            }
            var loopBody = Expression.Block(
                    Expression.IfThen(
                        Expression.Call(null, IsPresentMethod, listValidityVector, rowId), loopBodyInner));
            body.Add(CreateForLoop(rowId, p.ObjectsLength, loopBody));
            body.Add(CreateReleaseArrayExpression(allSubItems));
            return Expression.Block(typeof(void), variables, body);

        }

        private static Expression CreateListItemExpression(Expression listOrArray, Expression index)
        {
            if (listOrArray.Type.IsArray) return Expression.ArrayIndex(listOrArray, index);
            return Expression.Property(listOrArray, "Item", index);
        }





        private Expression CreateStructureSerializer(SerializerParameters p, StructureFieldInfo[] fields)
        {
            var body = new List<Expression>();
            var variables = new List<ParameterExpression>();
            var i = Expression.Variable(typeof(int), "i");
            var (_, hasValue) = UnwrapNullable(p.ParentValidity, i, Expression.ArrayIndex(p.Objects, i), null);

            var validityVector = Expression.Variable(typeof(nint), "validityVector");
            body.Add(CreateValidityVectorInitialization(validityVector, p.VectorPtr));
            body.Add(CreateForLoop(i, p.ObjectsLength, MaybeIf(Expression.Not(hasValue!), CreateSetNotPresent(validityVector, i))));
            variables.Add(i);
            variables.Add(validityVector);

            for (int fieldId = 0; fieldId < fields.Length; fieldId++)
            {
                var field = fields[fieldId];
                var serializer = CreateFieldSerializer(p.InputArrayElementType, field);
                body.Add(CreateCall(serializer, Expression.Call(null, GetStructureChildVectorMethod, p.VectorPtr, Expression.Constant((ulong)fieldId)), p.Objects, p.ObjectsLength, validityVector, p.Arena));

            }
            return Expression.Block(typeof(void), variables, body);

        }

        private Expression CreatePrimitiveFieldSerializer(SerializerParameters p, StructureFieldInfo? getter, DuckDbPrimitiveTypeConverter primitiveConverter)
        {

            var vectorSpan = Expression.Variable(typeof(Span<>).MakeGenericType(primitiveConverter.SerializationType), "vectorSpan");
            var i = Expression.Variable(typeof(int), "i");

            var arrayItem = Expression.Variable(p.InputArrayElementType, "arrayItem");
            var variables = new List<ParameterExpression>()
            {
                vectorSpan,
                i,
                arrayItem
            };
            var serializationBlock = new List<Expression>
            {
                Expression.Assign(arrayItem, Expression.ArrayIndex(p.Objects, i))
            };

            var (itemValue, hasValue) = UnwrapNullable(p.ParentValidity, i, arrayItem, getter, primitiveConverter.IsNullishMethod);

            ParameterExpression? validityVector = null;
            if (hasValue != null)
            {
                validityVector = Expression.Variable(typeof(nint), "validityVector");
                variables.Add(validityVector);
            }

            serializationBlock.Add(MaybeIf(hasValue,
                SerializeAndAssignNonNullValue(vectorSpan, i, itemValue, primitiveConverter, p.Arena),
                CreateSetNotPresent(validityVector, i)
                ));
            // Expression.Call(null, AssignSpanItemMethod.MakeGenericMethod(primitiveConverter.SerializationType), vectorSpan, i, Expression.Default(primitiveConverter.SerializationType))

            var body = Expression.Block(
                typeof(void),
                variables,
                CreateValidityVectorInitialization(validityVector, p.VectorPtr),
                Expression.Assign(vectorSpan, Expression.Call(null, GetVectorDataMethod.MakeGenericMethod(primitiveConverter.SerializationType), p.VectorPtr, p.ObjectsLength)),
                Expression.Assign(i, Expression.Constant(0)),
                CreateForLoop(i, Expression.Property(vectorSpan, "Length"), Expression.Block(serializationBlock))
            );
            return body;
        }




        private Expression CreatePrimitiveFieldDeserializer(DeserializerParameters p, StructureFieldInfo? getter, DuckDbPrimitiveTypeConverter primitiveConverter)
        {

            var vectorSpan = Expression.Variable(typeof(Span<>).MakeGenericType(primitiveConverter.SerializationType), "vectorSpan");
            var i = Expression.Variable(typeof(int), "i");

            var validityVector = Expression.Variable(typeof(nint), "validityVector");

            var variables = new List<ParameterExpression>()
            {
                vectorSpan,
                i,
            };

            var hasValue = validityVector != null ? Expression.Call(null, IsPresentMethod, validityVector, i) : null;

            Expression deserializedValue = Expression.Call(null, ReadSpanItemMethod.MakeGenericMethod(primitiveConverter.SerializationType), vectorSpan, i);
            if (primitiveConverter.DeserializeMethod != null)
            {
                deserializedValue = Expression.Call(null, primitiveConverter.DeserializeMethod, primitiveConverter.DeserializeMethod.GetParameters().Length == 2 ? [deserializedValue, p.DeserializationContext] : [deserializedValue]);
            }

            var assign = CreateAssignment(getter, p.Objects, i, deserializedValue);

            if (hasValue != null)
            {
                variables.Add(validityVector);
            }

            var deserializationBlock = MaybeIf(hasValue, assign);

            var body = Expression.Block(
                typeof(void),
                variables,
                CreateValidityVectorExpression(validityVector, p.VectorPtr),
                Expression.Assign(vectorSpan, Expression.Call(null, GetVectorDataMethod.MakeGenericMethod(primitiveConverter.SerializationType), p.VectorPtr, p.ObjectsLength)),
                Expression.Assign(i, Expression.Constant(0)),
                CreateForLoop(i, Expression.Property(vectorSpan, "Length"), Expression.Block(deserializationBlock))
            );
            return body;
        }

        private GeneratedMethodInfo CreateSerializer(Type inputArrayElementType, string prefix, Func<SerializerParameters, Expression> impl, StructureFieldInfo? fieldName)
        {
            var key = new SerializerCacheKey(inputArrayElementType, fieldName?.CacheKey ?? default, false);
            if (!serializerCache.TryGetValue(key, out var deleg))
            {
                return serializerCache.GetOrAdd(key, key =>
                {
                    var paramVectorPtr = Expression.Parameter(typeof(nint), "vectorPtr");
                    var paramObjects = Expression.Parameter(inputArrayElementType.MakeArrayType(), "objects");
                    var paramObjectsLength = Expression.Parameter(typeof(int), "objectsLength");
                    var paramParentValidity = Expression.Parameter(typeof(nint), "parentValidity");
                    var paramArena = Expression.Parameter(typeof(NativeArenaSlim), "arena");
                    var body = impl(new SerializerParameters(inputArrayElementType, paramVectorPtr, paramObjects, paramObjectsLength, paramParentValidity, paramArena));
                    var name = prefix + CreateSpeakableTypeName(inputArrayElementType, null, fieldName);
                    return CreateMethod(name, null, null, null, body, paramVectorPtr, paramObjects, paramObjectsLength, paramParentValidity, paramArena);
                });
            }
            return deleg;
        }

        private static string CreateSpeakableTypeName(Type type, DuckDbStructuralType? structuralType, StructureFieldInfo? field)
        {
            return CreateSpeakableTypeName(type, structuralType) + (field != null ? "_" + field.DuckDbFieldName + (field.FieldStructuralType is not null ? "_" + field.FieldStructuralType.Hash : null) : null);
        }

        private static string CreateSpeakableTypeName(Type type, DuckDbStructuralType? structuralType)
        {
            if (type.IsArray) return "ArrayOf_" + CreateSpeakableTypeName(type.GetElementType(), structuralType);
            if (type.IsGenericType)
            {
                var gen = type.GetGenericTypeDefinition();
                if (gen == typeof(List<>))
                    return "ListOf_" + CreateSpeakableTypeName(type.GetGenericArguments()[0], structuralType);
            }
            var nonNull = Nullable.GetUnderlyingType(type);
            if (nonNull != null) return "Nullable_" + CreateSpeakableTypeName(nonNull, structuralType);
            return type.FullName.Replace(".", "_").Replace('+', '_') + structuralType?.Hash;
        }

        private GeneratedMethodInfo CreateDeserializer(Type outputArrayElementType, string prefix, DuckDbStructuralType outputArrayElementStructuralType, Func<DeserializerParameters, Expression> impl, StructureFieldInfo? field)
        {
            var cacheKey = new DeserializerCacheKey(outputArrayElementType, outputArrayElementStructuralType, field?.CacheKey ?? default, false);
            if (!deserializerCache.TryGetValue(cacheKey, out var deleg))
            {
                return deserializerCache.GetOrAdd(cacheKey, cacheKey =>
                {
                    var paramVectorPtr = Expression.Parameter(typeof(nint), "vectorPtr");
                    var paramObjects = Expression.Parameter(outputArrayElementType.MakeArrayType(), "objects");
                    var paramObjectsLength = Expression.Parameter(typeof(int), "objectsLength");
                    var paramDeserializationContext = Expression.Parameter(typeof(DuckDbDeserializationContext), "deserializationCtx");
                    var body = impl(new DeserializerParameters(outputArrayElementType, outputArrayElementStructuralType, paramVectorPtr, paramObjects, paramObjectsLength, paramDeserializationContext));
                    return CreateMethod(prefix + CreateSpeakableTypeName(outputArrayElementType, outputArrayElementStructuralType, field), null, null, null, body, paramVectorPtr, paramObjects, paramObjectsLength, paramDeserializationContext);
                });
            }
            return deleg;

        }

        private static Expression CreateSetNotPresent(ParameterExpression? validityVector, ParameterExpression rowId)
        {
            if (validityVector == null) return Expression.Empty();
            return Expression.Call(null, SetNotPresentMethod, validityVector, rowId);
        }


        private Expression CreateStructureSelector(SerializerParameters p, StructureFieldInfo? getter, StructureFieldInfo[]? structureFields)
        {
            var substructType = getter != null ? getter.FieldType : p.InputArrayElementType;

            var substructCount = Expression.Variable(typeof(int), "substructCount");
            var rowId = Expression.Variable(typeof(int), "rowId");
            var innerAbsIdx = Expression.Variable(typeof(int), "innerAbsIdx");
            var subobjects = Expression.Variable(substructType.MakeArrayType(), "subobjects");
            var body = new List<Expression>();
            var variables = new[]
            {
                rowId,
                subobjects,
            }.ToList();

            var inputObject = Expression.ArrayIndex(p.Objects, rowId);
            var (inputSubstructExpr, hasInputSubstruct) = UnwrapNullable(p.ParentValidity, rowId, inputObject, getter);

            ParameterExpression? validityVector = null;

            validityVector = Expression.Variable(typeof(nint), "validityVector");
            variables.Add(validityVector);
            body.Add(CreateValidityVectorInitialization(validityVector, p.VectorPtr));

            body.Add(Expression.Assign(subobjects, CreateRentArrayExpression(substructType, p.ObjectsLength)));
            body.Add(CreateForLoop(rowId, p.ObjectsLength, Expression.Block(
                MaybeIf(hasInputSubstruct,
                    Expression.Assign(Expression.ArrayAccess(subobjects, rowId), MaybeConvertToNullable(inputSubstructExpr, substructType)),
                    CreateSetNotPresent(validityVector, rowId))
           )));
            var substructSerializer = CreateSerializer(substructType, "SerializeStruct_", p => CreateStructureSerializer(p, structureFields), null);
            body.Add(CreateCall(substructSerializer, p.VectorPtr, subobjects, rowId, validityVector, p.Arena));
            body.Add(CreateReleaseArrayExpression(subobjects));
            return Expression.Block(variables, body);

        }



        private Expression CreateStructureDeselector(DeserializerParameters p, StructureFieldInfo? getter, StructureFieldInfo?[] structureFields)
        {
            var substructType = getter != null ? getter.FieldType : p.OutputArrayElementType;
            var substructStructuralType = getter != null ? getter.FieldStructuralType : p.OutputArrayStructuralType;
            var variables = new List<ParameterExpression>();

            var subobjects = Expression.Variable(substructType.MakeArrayType(), "subItems");
            var validityVector = Expression.Variable(typeof(nint), "validityVector");
            var rowId = Expression.Variable(typeof(int), "rowId");

            //var absIdx = Expression.Variable(typeof(int), "absIdx");
            var subobjectsCount = Expression.Variable(typeof(int), "subobjectsCount");

            variables.Add(subobjects);
            variables.Add(validityVector);
            variables.Add(rowId);
            //variables.Add(absIdx);
            //variables.Add(subobjectsCount);
            var assignment = CreateAssignment(getter, p.Objects, rowId, Expression.ArrayIndex(subobjects, rowId));

            var body = new List<Expression>();
            //body.Add(Expression.Assign(rowId, Expression.Constant(0)));
            body.Add(Expression.Assign(validityVector, Expression.Call(null, GetVectorValidityMethod, p.VectorPtr)));
            //body.Add(Expression.Assign(subobjectsCount, Expression.Call(null, GetSubObjectsCountMethod, validityVector, p.ObjectsLength)));
            body.Add(Expression.Assign(subobjects, CreateRentArrayExpression(substructType, p.ObjectsLength, zeroed: true)));
            var isPresent = Expression.Call(null, IsPresentMethod, validityVector, rowId);
            var needsInitialization = NeedsInitialization(substructType);
            if (needsInitialization)
                body.Add(CreateInitializationLoop(subobjects, substructType, rowId, p.ObjectsLength, isPresent));
            var substructDeserializer = CreateDeserializer(substructType, "DeserializeStruct_", substructStructuralType, p => CreateNonNullStructureDeserializer(p, structureFields), null);

            body.Add(CreateCall(substructDeserializer, p.VectorPtr, subobjects, p.ObjectsLength, p.DeserializationContext));
            var loopBody = Expression.Block(
                Expression.IfThen(
                    Expression.Call(null, IsPresentMethod, validityVector, rowId),
                    assignment
                )
            );
            body.Add(CreateForLoop(rowId, p.ObjectsLength, loopBody));
            body.Add(CreateReleaseArrayExpression(subobjects));
            return Expression.Block(typeof(void), variables, body);

        }

        private Expression CreateAssignment(StructureFieldInfo? field, Expression destinationArray, Expression destinationIndex, Expression source)
        {
            if (field == null)
            {
                source = MaybeConvertToNullable(source, destinationArray.Type.GetElementType());
                return Expression.Assign(Expression.ArrayAccess(destinationArray, destinationIndex), source);
            }
            else
            {
                return field.CreateSetExpression(destinationArray, destinationIndex, source, this);
            }
        }

        private static Expression CreateValidityVectorInitialization(ParameterExpression? validityVector, Expression vector)
        {
            if (validityVector == null) return Expression.Empty();
            return Expression.Assign(validityVector, Expression.Call(null, GetVectorValidityAndSetAllMethod, vector));
        }
        private static Expression CreateValidityVectorExpression(ParameterExpression? validityVector, Expression vector)
        {
            if (validityVector == null) return Expression.Empty();
            return Expression.Assign(validityVector, Expression.Call(null, GetVectorValidityMethod, vector));
        }

        private static Expression GetListCountExpression(Expression list)
        {
            var type = list.Type;
            Expression expr;
            if (type.GetProperty("Count", BindingFlags.Public | BindingFlags.Instance) is { } countProperty) expr = Expression.Property(list, countProperty);
            else if (type.GetField("Count", BindingFlags.Public | BindingFlags.Instance) is { } countField) expr = Expression.Field(list, countField);
            else if (type.GetProperty("Length", BindingFlags.Public | BindingFlags.Instance) is { } lengthProperty) expr = Expression.Property(list, lengthProperty);
            else if (type.GetField("Length", BindingFlags.Public | BindingFlags.Instance) is { } lengthField) expr = Expression.Field(list, lengthField);
            else throw new NotSupportedException($"Could not find a Count or Length field/property for {list.Type}");

            if (expr.Type != typeof(int)) expr = Expression.ConvertChecked(expr, typeof(int));
            return expr;
        }

        private static Expression MaybeIf(Expression? condition, Expression ifTrue, Expression? ifFalse = null)
        {
            if (condition != null && condition is UnaryExpression { NodeType: ExpressionType.Not, Operand: UnaryExpression { NodeType: ExpressionType.Not, Operand: { } inner } })
                condition = inner;
            if (condition != null && ifFalse != null && condition.NodeType == ExpressionType.Not)
            {
                return Expression.IfThenElse(((UnaryExpression)condition).Operand, ifFalse, ifTrue);
            }
            return condition != null ? (ifFalse != null ? Expression.IfThenElse(condition, ifTrue, ifFalse) : Expression.IfThen(condition, ifTrue)) : ifTrue;
        }

        private static Expression SerializeAndAssignNonNullValue(Expression span, Expression index, Expression nonNullValue, DuckDbPrimitiveTypeConverter primitiveConverter, ParameterExpression paramArena)
        {
            Expression serializedValue = nonNullValue;
            if (primitiveConverter.SerializeMethod != null)
            {
                serializedValue = Expression.Call(null, primitiveConverter.SerializeMethod, primitiveConverter.NeedsArena ? new[] { nonNullValue, paramArena } : new[] { nonNullValue });
                if (primitiveConverter.IsEnum)
                {
                    var clrUnderlyingType = Enum.GetUnderlyingType(primitiveConverter.ClrType);
                    var underlyingMaximum = Convert.ChangeType(primitiveConverter.EnumInfo.MaximumAllowedValue, clrUnderlyingType);
                    serializedValue = Expression.Condition(Expression.LessThanOrEqual(Expression.Convert(nonNullValue, clrUnderlyingType), Expression.Constant(underlyingMaximum, clrUnderlyingType)), serializedValue, Expression.Call(null, ThrowEnumOutOfRangeMethod.MakeGenericMethod(nonNullValue.Type, primitiveConverter.SerializationType), nonNullValue));
                }
            }
            // ref properties are not supported: Expression.Call(Expression.MakeIndex(vectorSpan, vectorSpan.Type.GetProperty("Item"), new[] { i }), serializedValue)
            var assign = Expression.Call(null, AssignSpanItemMethod.MakeGenericMethod(serializedValue.Type), span, index, serializedValue);
            return assign;
        }


        internal static ConcurrentDictionary<Type, RootSerializer> RootSerializerCache = new();
        internal static ConcurrentDictionary<Type, RootVectorSerializer> RootVectorSerializerCache = new();
        internal static ConcurrentDictionary<(Type ClrType, StructuralTypeHash StructuralType), RootDeserializer> RootDeserializerCache = new();
        internal static ConcurrentDictionary<StructuralTypeHash, Type> StructuralHashToRegisteredClrType = new();

        public RootSerializer CreateRootSerializer(Type elementType)
        {
            if (!RootSerializerCache.TryGetValue(elementType, out var r))
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new NotSupportedException($"Could not find a pre-compiled serializer for CLR type '{elementType}'.");
                r = RootSerializerCache.GetOrAdd(elementType, elementType => (RootSerializer)CreateRootSerializerCore(elementType).Delegate);
            }
            return r;
        }

        public RootVectorSerializer CreateRootVectorSerializer(Type elementType)
        {
            if (!RootVectorSerializerCache.TryGetValue(elementType, out var r))
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new NotSupportedException($"Could not find a pre-compiled vector serializer for CLR type '{elementType}'.");
                r = RootVectorSerializerCache.GetOrAdd(elementType, elementType => (RootVectorSerializer)CreateRootVectorSerializerCore(elementType).Delegate);
            }
            return r;
        }

        public RootDeserializer CreateRootDeserializer(Type elementType, DuckDbStructuralType elementStructuralType)
        {
            if (!RootDeserializerCache.TryGetValue((elementType, elementStructuralType.Hash), out var r))
            {
                if (!RuntimeFeature.IsDynamicCodeSupported)
                    throw new NotSupportedException($"Could not find a pre-compiled deserializer for the pair of CLR type '{elementType}' and the exact DuckDB type returned by the query. Verify that RegisterAll() was called, and that the DuckDB query is returning the same type as when the pre-compiled serializers were generated (including any used or unused extra fields/columns).");
                r = RootDeserializerCache.GetOrAdd((elementType, elementStructuralType.Hash), _ => (RootDeserializer)CreateRootDeserializerCore(elementType, elementStructuralType).Delegate);
            }
            return r;
        }

        internal GeneratedMethodInfo CreateRootDeserializerCore(Type elementType, DuckDbStructuralType elementStructuralType)
        {
            var chunkParam = Expression.Parameter(typeof(nint), "chunk");
            var rowCount = Expression.Variable(typeof(int), "rowCount");
            var result = Expression.Variable(elementType.MakeArrayType(), "result");
            var deserializationContext = Expression.Parameter(typeof(DuckDbDeserializationContext), "deserializationContext");
            var isWrappedSingleColumn = IsWrappedSingleColumn(elementType);

            var needsInitialization = !isWrappedSingleColumn && NeedsInitialization(elementType);

            var i = Expression.Variable(typeof(int), "i");
            var body = new List<Expression>();
            body.Add(Expression.Assign(rowCount, Expression.Call(null, GetChunkSizeMethod, chunkParam)));
            body.Add(Expression.Assign(result, Expression.NewArrayBounds(elementType, rowCount)));
            if (needsInitialization)
                body.Add(CreateInitializationLoop(result, elementType, i, rowCount, null));

            if (isWrappedSingleColumn)
            {
                var deserializer = CreateFieldDeserializer(elementType, elementStructuralType, null);
                body.Add(CreateCall(deserializer, Expression.Call(GetDataChunkVectorMethod, chunkParam, Expression.Constant((ulong)0)), result, rowCount, deserializationContext));
            }
            else
            {
                var cols = DuckDbTypeCreator.GetFields(elementType, elementStructuralType);
                for (int colIdx = 0; colIdx < cols.Count; colIdx++)
                {
                    var col = cols[colIdx];
                    if (col != null)
                    {
                        var deserializer = CreateFieldDeserializer(elementType, elementStructuralType, DuckDbTypeCreator.CreateGetter(col));
                        body.Add(CreateCall(deserializer, Expression.Call(GetDataChunkVectorMethod, chunkParam, Expression.Constant((ulong)colIdx)), result, rowCount, deserializationContext));
                    }
                    else throw new Exception($"A column that was selected doesn't have a corresponding field or property in the specified return type.");
                }
            }
            body.Add(result);
            var bodyBlock = Expression.Block(new[] { rowCount, result, needsInitialization ? i : null }.Where(x => x != null).ToArray(), body);
            return CreateMethod("DeserializeColumns_" + CreateSpeakableTypeName(elementType, elementStructuralType), typeof(RootDeserializer), elementType, elementStructuralType, bodyBlock, chunkParam, deserializationContext);
        }

        internal static bool IsWrappedSingleColumn(Type elementType)
        {
            var nonNull = Nullable.GetUnderlyingType(elementType) ?? elementType;
            if (TypeSniffedEnumerable.TryGetEnumerableElementType(elementType) != null) return true;
            if (elementType.IsEnum && elementType.GetCustomAttribute<FlagsAttribute>() == null) return true;
            var primitiveType = TryGetPrimitiveConverter(nonNull);
            if (primitiveType != null)
            {
                return true;
            }
            return false;
        }

        private static Expression CreateInitializationLoop(Expression array, Type elementType, Expression i, Expression rowCount, Expression? isPresent)
        {
            Expression createObj;
            if (elementType.IsClass)
            {
                createObj = HasEmptyCtor(elementType, out var ctor) ? Expression.New(ctor) : Expression.Call(null, NewSkipCtorMethod.MakeGenericMethod(elementType));
            }
            else
            {
                createObj = Expression.Convert(Expression.Default(Nullable.GetUnderlyingType(elementType)), elementType);
            }
            return CreateForLoop(i, rowCount, MaybeIf(isPresent, Expression.Assign(Expression.ArrayAccess(array, i), createObj)));
        }

        private static bool HasEmptyCtor(Type elementType, out ConstructorInfo ctor)
        {
            ctor = elementType.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Array.Empty<Type>())!;
            if (ctor == null) return false;
            return true;
            /*
            var body = ctor.GetMethodBody();
            if (body == null) return false;
            var il = body.GetILAsByteArray();
            return true;*/
        }

        private static bool NeedsInitialization(Type elementType)
        {
            if (Nullable.GetUnderlyingType(elementType) != null) return true;
            if (elementType.IsValueType) return false;

            if (TypeSniffedEnumerable.TryGetEnumerableElementType(elementType) != null) return false;
            return TryGetPrimitiveConverter(elementType) == null;
        }

        internal GeneratedMethodInfo CreateRootSerializerCore(Type elementType)
        {
            var enumeratorParam = Expression.Parameter(typeof(IEnumerator), "enumerator");
            var chunkParam = Expression.Parameter(typeof(nint), "chunk");
            var arenaParam = Expression.Parameter(typeof(NativeArenaSlim), "arena");
            var itemCount = Expression.Variable(typeof(int), "itemCount");
            var buffer = Expression.Variable(elementType.MakeArrayType(), "buffer");
            var typedEnumerator = Expression.Variable(typeof(IEnumerator<>).MakeGenericType(elementType), "typedEnumerator");
            var body = new List<Expression>();
            body.Add(Expression.Assign(typedEnumerator, Expression.Convert(enumeratorParam, typedEnumerator.Type)));
            body.Add(Expression.Assign(buffer, CreateRentArrayExpression(elementType, Expression.Constant(DuckDbUtils.STANDARD_VECTOR_SIZE))));
            body.Add(Expression.Assign(itemCount, Expression.Constant(0)));
            var breakLabel = Expression.Label();
            body.Add(Expression.Loop(Expression.Block(
                Expression.IfThen(Expression.Not(Expression.Call(enumeratorParam, "MoveNext", null)), Expression.Break(breakLabel)),
                Expression.Assign(Expression.ArrayAccess(buffer, Expression.PostIncrementAssign(itemCount)), Expression.Property(typedEnumerator, "Current")),
                Expression.IfThen(Expression.Equal(itemCount, Expression.Constant(DuckDbUtils.STANDARD_VECTOR_SIZE)), Expression.Break(breakLabel))
            ), breakLabel));

            var cols = DuckDbTypeCreator.GetFields(elementType);
            for (int colIdx = 0; colIdx < cols.Count; colIdx++)
            {
                var col = cols[colIdx];
                var serializer = CreateFieldSerializer(elementType, DuckDbTypeCreator.CreateGetter(col));
                var chunkVector = Expression.Call(GetDataChunkVectorMethod, chunkParam, Expression.Constant((ulong)colIdx));
                body.Add(CreateCall(serializer, chunkVector, buffer, itemCount, Expression.Constant((nint)0), arenaParam));
            }
            body.Add(CreateReleaseArrayExpression(buffer));
            body.Add(itemCount);
            var bodyBlock = Expression.Block(new[] { itemCount, buffer, typedEnumerator }, body);

            return CreateMethod("SerializeColumns_" + CreateSpeakableTypeName(elementType, null), typeof(RootSerializer), elementType, null, bodyBlock, enumeratorParam, chunkParam, arenaParam);

        }

        internal GeneratedMethodInfo CreateRootVectorSerializerCore(Type elementType)
        {
            var untypedArrayParam = Expression.Parameter(typeof(Array), "untypedArray");
            var vectorParam = Expression.Parameter(typeof(nint), "vector");
            var arenaParam = Expression.Parameter(typeof(NativeArenaSlim), "arena");
            var array = Expression.Variable(elementType.MakeArrayType(), "array");
            var serializer = CreateFieldSerializer(elementType, null);
            var body = new List<Expression>
            {
                Expression.Assign(array, Expression.Convert(untypedArrayParam, elementType.MakeArrayType())),
                CreateCall(serializer, vectorParam, array, Expression.ArrayLength(array), Expression.Constant((nint)0), arenaParam)
            };
            
            var bodyBlock = Expression.Block(new[] { array }, body);
            return CreateMethod("SerializeVector_" + CreateSpeakableTypeName(elementType, null), typeof(RootVectorSerializer), elementType, null, bodyBlock, untypedArrayParam, vectorParam, arenaParam);

        }

        private static Expression CreateCall(GeneratedMethodInfo method, params Expression[] arguments)
        {
            return Expression.Invoke(Expression.Constant(method.Delegate), arguments);
        }
        private static Expression CreateIlMethodCall(Delegate deleg, params Expression[] arguments)
        {
            return Expression.Invoke(Expression.Constant(deleg), arguments);
        }

        private GeneratedMethodInfo CreateMethod(string name, Type? delegateType, Type? isRootForType, DuckDbStructuralType? isRootForStructuralType, Expression body, params ParameterExpression[] parameters)
        {
            var lambda = delegateType != null ? Expression.Lambda(delegateType, body, name, parameters) : Expression.Lambda(body, name, parameters);
            var deleg = lambda.Compile();
            var m = new GeneratedMethodInfo(name, deleg, isRootForType, isRootForStructuralType, body, parameters, null, null);
            AddGeneratedMethod(m);
            return m;
        }

        private void AddGeneratedMethod(GeneratedMethodInfo m)
        {
            lock (this)
            {
                GeneratedMethods?.Add(m);
            }
        }

        private static Expression MaybeConvertToNullable(Expression objNonNull, Type destType)
        {
            if (Nullable.GetUnderlyingType(destType) != null) return Expression.Convert(objNonNull, destType);
            return objNonNull;
        }

        private bool IsGeneratingCSharpCode => GeneratedMethods != null;

        internal Expression CreateFieldSetter(Expression destArr, Expression destIdx, FieldInfo2 member, Expression val)
        {
            var destObj = Expression.ArrayAccess(destArr, destIdx);
            var objType = destObj.Type;
            val = MaybeConvertToNullable(val, member.FieldType);
            var valType = val.Type;
            var cilValType = DuckDbUtils.IsEnum(valType) ? Enum.GetUnderlyingType(valType) : valType;

            var f = member.Member as FieldInfo;
            var p = member.Member as PropertyInfo;

            if (p != null && p.SetMethod == null)
            {
                if (objType.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal))
                {
                    f = objType.GetField($"<{p.Name}>i__Field", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (f == null) throw new Exception("Could not find backing field for anonymous type.");
                    p = null;
                }
            }

            if (Nullable.GetUnderlyingType(objType) is { } nonNullableObjType)
            {
                var deleg = CreateIlMethod(CreateSpeakableTypeName(destObj.Type, null) + "_SetFieldInsideNullable_" + member.Name, typeof(void), new[] { destArr.Type, typeof(int), val.Type }, ilgen =>
                {
                    ilgen.Emit(OpCodes.Ldarg_0);
                    ilgen.Emit(OpCodes.Ldarg_1);
                    ilgen.EmitCall(OpCodes.Call, GetReferenceToNullableWrappedValueMethod.MakeGenericMethod(nonNullableObjType), null);
                    ilgen.Emit(OpCodes.Ldarg_2);
                    if (p != null)
                    {
                        ilgen.Emit(OpCodes.Call, p.SetMethod);
                    }
                    else
                    {
                        ilgen.Emit(OpCodes.Stobj, valType);

                    }
                    ilgen.Emit(OpCodes.Ret);
                }, () => $"SerializationHelpers.GetReferenceToNullableWrappedValue(arr, idx).{member.Name} = val;", SetFieldParameterNames);

                return CreateIlMethodCall(deleg, destArr, destIdx, val);
            }

            if (p != null && IsPropertySettable(p))
            {
                return Expression.Assign(Expression.Property(destObj, p), val);
            }
            if (f != null && IsFieldSettable(f))
            {
                return Expression.Assign(Expression.Field(destObj, f), val);
            }

            if (f != null)
            {
                var deleg = CreateIlMethod(CreateSpeakableTypeName(destObj.Type, null) + "_Set_" + member.Name, typeof(void), new[] { destArr.Type, typeof(int), val.Type }, ilgen =>
                {
                    ilgen.Emit(OpCodes.Ldarg_0);
                    ilgen.Emit(OpCodes.Ldarg_1);
                    if (destObj.Type.IsClass)
                        ilgen.Emit(OpCodes.Ldelem_Ref);
                    else
                        ilgen.Emit(OpCodes.Ldelema, objType);
                    ilgen.Emit(OpCodes.Ldflda, f);
                    ilgen.EmitCall(OpCodes.Call, UnsafeAsRefMethod.MakeGenericMethod(valType), null);
                    ilgen.Emit(OpCodes.Ldarg_2);
                    ilgen.Emit(OpCodes.Stobj, valType);
                    ilgen.Emit(OpCodes.Ret);
                }, () => $"System.Runtime.CompilerServices.Unsafe.AsRef(arr[idx].{f.Name}) = val;", SetFieldParameterNames);
                return CreateIlMethodCall(deleg, destArr, destIdx, val);
            }


            if (IsGeneratingCSharpCode && p.SetMethod != null)
            {
                var deleg = CreateIlMethod(CreateSpeakableTypeName(destObj.Type, null) + "_SetPropertyViaReflection_" + member.Name, typeof(void), new[] { destArr.Type, typeof(int), val.Type }, ilgen =>
                {
                    ilgen.ThrowException(typeof(NotSupportedException));
                }, () => "throw new System.NotImplementedException();", SetFieldParameterNames);
                return CreateIlMethodCall(deleg, destArr, destIdx, val);
            }

            throw new NotSupportedException($"Property {objType}.{member.Name} cannot be deserialized because it's not settable, nor a well-known compiler-generated property type.");
        }

        private bool IsPropertySettable(PropertyInfo p)
        {
            if (p.SetMethod == null) return false;
            if (!IsGeneratingCSharpCode) return true;

            return p.SetMethod.IsPublic && !p.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
        }

        private bool IsFieldVisible(FieldInfo f) => !IsGeneratingCSharpCode || f.IsPublic;

        private bool IsFieldSettable(FieldInfo f)
        {
            if (f.IsInitOnly) return false;
            return IsFieldVisible(f);
        }

        private readonly static MethodInfo UnsafeAsRefMethod = typeof(Unsafe).GetMethods(BindingFlags.Static | BindingFlags.Public).Single(x => x.Name == "AsRef" && x.GetParameters()[0].IsIn);

        private readonly static MethodInfo GetReferenceToNullableWrappedValueMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetReferenceToNullableWrappedValue), BindingFlags.Public | BindingFlags.Static);

        private readonly string[] SetFieldParameterNames = new[] { "arr", "idx", "val" };
        private Delegate CreateIlMethod(string name, Type returnType, Type[]? parameterTypes, Action<ILGenerator> emitter, Func<string> csharpVersion, string[] csharpParameterNames)
        {
            /*
            if (moduleBuilder == null)
            {
                var asmnameStr = "DuckDbDynamicSetters";
                new DynamicMethod()
                var asmname = new AssemblyName(asmnameStr);
                assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(asmnameStr);
            }
            

            var typeBuilder = moduleBuilder.DefineType("Dyn" + (++lastIlGeneratedMethodId));
            var methodBuilder = typeBuilder.DefineMethod(nameForDebugging, MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig, returnType, parameterTypes);
            */
            var methodBuilder = new DynamicMethod(name, returnType, parameterTypes);

            var generator = methodBuilder.GetILGenerator();
            emitter(generator);
            var delegateType = GetDelegateType(returnType, parameterTypes);
            var deleg = methodBuilder.CreateDelegate(delegateType);
            AddGeneratedMethod(new GeneratedMethodInfo(name, deleg, null, null, null, null, csharpVersion(), csharpParameterNames));
            return deleg;
            /*
            var type = typeBuilder.CreateType();
            //assemblyBuilder.SaveToFile("c:\\temp\\p.dll");
            var method = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Single();
            return method.CreateDelegate(GetDelegateType(returnType, parameterTypes));
            */
        }

        private static Type GetDelegateType(Type returnType, Type[] parameterTypes)
        {
            var paramLength = parameterTypes.Length;
            if (returnType == typeof(void))
            {
                if (paramLength == 0) return typeof(Action);
                if (paramLength == 1) return typeof(Action<>).MakeGenericType(parameterTypes[0]);
                if (paramLength == 2) return typeof(Action<,>).MakeGenericType(parameterTypes[0], parameterTypes[1]);
                if (paramLength == 3) return typeof(Action<,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], parameterTypes[2]);
            }
            else
            {

                if (paramLength == 0) return typeof(Func<>).MakeGenericType(returnType);
                if (paramLength == 1) return typeof(Func<,>).MakeGenericType(parameterTypes[0], returnType);
                if (paramLength == 2) return typeof(Func<,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], returnType);
                if (paramLength == 3) return typeof(Func<,,,>).MakeGenericType(parameterTypes[0], parameterTypes[1], parameterTypes[2], returnType);
            }
            throw new NotSupportedException();
        }

        internal static DuckDbPrimitiveTypeConverter? TryGetPrimitiveConverter(Type nonNullableType)
        {
            var converter = PrimitiveConverters.FirstOrDefault(x => x.ClrType == nonNullableType);
            if (converter != null) return converter;
            if (nonNullableType.IsEnum)
            {
                var serializeAs = nonNullableType.GetCustomAttribute<DuckDbSerializeAsAttribute>();
                if (serializeAs == null) throw new ArgumentException();
                var serializeAsType = serializeAs.Type;
                if (serializeAsType == typeof(string))
                {
                    return new DuckDbPrimitiveTypeConverter(nonNullableType, DUCKDB_TYPE.DUCKDB_TYPE_VARCHAR, DuckDbTypeCreator.SerializeEnumAsStringMethod.MakeGenericMethod(nonNullableType), DuckDbTypeCreator.DeserializeEnumFromStringMethod.MakeGenericMethod(nonNullableType));
                }
                else if (serializeAsType == Enum.GetUnderlyingType(nonNullableType))
                {
                    return new DuckDbPrimitiveTypeConverter(nonNullableType, TryGetPrimitiveConverter(serializeAsType)!.Kind);
                }
                else
                {
                    throw new ArgumentException($"When [{nameof(DuckDbSerializeAsAttribute)}] is specified on an enum ({nonNullableType}), its value must be either System.String, or the enum underlying type ({Enum.GetUnderlyingType(nonNullableType)}).");
                }
            }
            return null;
        }

        private int lastIlGeneratedMethodId;
        //private AssemblyBuilder? assemblyBuilder;
        //private ModuleBuilder? moduleBuilder;

        private static MethodInfo GetDataChunkVectorMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetDataChunkVector), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetVectorValidityMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetVectorValidity), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        private readonly static MethodInfo GetVectorValidityAndSetAllMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetVectorValidityAndSetAll), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo SetNotPresentMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.SetNotPresent), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;
        private readonly static MethodInfo GetSubObjectsCountMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.GetSubObjectsCount), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;


        public readonly static SerializerCreationContext Global = new();

    }

    record struct SerializerCacheKey(Type Type, GetterKey Getter, bool IsFieldSerializer);
    record struct DeserializerCacheKey(Type Type, DuckDbStructuralType StructuralType, GetterKey Getter, bool IsFieldDeserializer);
    record struct GetterKey(MemberInfo? Member, ulong FlagsAttributeValue);
}

