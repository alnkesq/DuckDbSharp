using DuckDbSharp.Bindings;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace DuckDbSharp.Reflection
{
    public unsafe class TypeGenerationContext
    {
        public readonly ModuleBuilder ModuleBuilder;
        public readonly AssemblyBuilder AssemblyBuilder;
        private readonly HashSet<string> UsedNames = new();
        internal ConcurrentDictionary<TypeKey, Type>? ClrTypeCache;
        public string Namespace = "GeneratedDuckDbTypes";
        private bool AllowInvalidCSharpIdentifiers;
        public TypeGenerationContext(bool forNullnessCheck = false, bool allowInvalidCSharpIdentifiers = false)
        {
            var asmnameStr = "DuckDbDynamic_";
            var asmname = new AssemblyName(asmnameStr);
            AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder = AssemblyBuilder.DefineDynamicModule(asmnameStr);
            this.AllowInvalidCSharpIdentifiers = allowInvalidCSharpIdentifiers;
            if (forNullnessCheck) Paths = new();
            else ClrTypeCache = new();
        }

        internal List<(Type Type, TypeKey TypeKey, TypePath Path)>? Paths;
        internal Dictionary<TypeKey, List<string>> NeverNullFields { get; init; }

        public string GetNewTypeName(string hint, IEnumerable<string>? forbiddenNames, string? disambiguationSuffix)
        {
            hint = DuckDbUtils.ToPascalCase(hint);
            if (disambiguationSuffix != null && forbiddenNames.Any(x => x == hint))
                hint += "Type";  // Field names can't have the same name of the declaring type.
            var name = hint;

            int num = 1;
            while (UsedNames.Contains(name))
            {
                name = hint + ++num;
            }
            UsedNames.Add(name);
            return string.IsNullOrEmpty(Namespace) ? name : Namespace + "." + name;
        }

        private Type CreateOrReuseType(string? nameHint, DuckDbStructuralType type, Func<Type> factory)
        {
            if (ClrTypeCache == null) return factory(); // nullness check phase: we don't want to reuse types.
            return ClrTypeCache.GetOrAdd(type.GetTypeKey(nameHint), _ => factory());
        }


        internal Type CreateClrType(DuckDbStructuralType lt, string? nameHint, TypePath? path)
        {
            if (lt.Kind == DUCKDB_TYPE.DUCKDB_TYPE_ENUM)
                return CreateOrReuseType(nameHint, lt, () => CreateClrEnumTypeCore(lt, nameHint));
            if (lt.Kind == DUCKDB_TYPE.DUCKDB_TYPE_STRUCT || lt.Kind == default /* result */)
                return CreateOrReuseType(nameHint, lt, () => CreateClrStructureTypeCore(lt, nameHint, path));
            if (lt.Kind == DUCKDB_TYPE.DUCKDB_TYPE_LIST)
                return CreateClrListTypeCore(lt, nameHint, path);
            if (lt.Kind == DUCKDB_TYPE.DUCKDB_TYPE_ARRAY)
                return CreateOrReuseType(nameHint, lt, () => CreateClrFixedArrayTypeCore(lt, nameHint, path));
            var t = lt.Kind;
            var primitive = SerializerCreationContext.PrimitiveConverters.FirstOrDefault(x => x.Kind == t);
            if (primitive == null) throw new NotSupportedException($"DuckDb type is not supported: {t}");
            return primitive.ClrType;
        }

        private Type CreateClrListTypeCore(DuckDbStructuralType lt, string? nameHint, TypePath path)
        {
            return CreateClrType(lt.ElementType, nameHint, new TypePath { ListElement = true, Parent = path }).MakeArrayType();

        }
        private Type CreateClrFixedArrayTypeCore(DuckDbStructuralType lt, string? nameHint, TypePath path)
        {            
            throw new NotImplementedException();
        }

        private Type CreateClrEnumTypeCore(DuckDbStructuralType structural, string? nameHint)
        {

            var members = structural.EnumMembers;
            var underlyingType =
                members.Count <= byte.MaxValue ? typeof(byte) :
                members.Count <= ushort.MaxValue ? typeof(ushort) :
                typeof(uint);
            var enumBuilder = ModuleBuilder.DefineEnum(GetNewTypeName(nameHint ?? "AnonymousEnum", null, null), TypeAttributes.Public, underlyingType);

            for (int i = 0; i < members.Count; i++)
            {
                var name = members[i];
                if (name == DuckDbPrimitiveTypeConverter.AnonymousEnumMemberPrefix + i) continue;
                enumBuilder.DefineLiteral(DuckDbUtils.ToPascalCase(name), Convert.ChangeType(i, underlyingType));
            }

            var t = enumBuilder.CreateType();
            return t;
        }


        private Type CreateClrStructureTypeCore(DuckDbStructuralType structural, string? nameHint, TypePath path)
        {
            var members = structural.StructureFields;
            var classBuilder = ModuleBuilder.DefineType(GetNewTypeName(nameHint ?? "AnonymousType", members.Select(x => x.Name), "Type"), TypeAttributes.Public);

            var typeKey = structural.GetTypeKey(nameHint);
            var neverNullFields = NeverNullFields != null && NeverNullFields.TryGetValue(typeKey, out var n) ? n : null;

            foreach (var member in members)
            {
                var name = member.Name;
                if (!IsValidCSharpIdentifier(name, AllowInvalidCSharpIdentifiers)) throw new NotSupportedException($"Detected column or field with an invalid name: '{name}'. Consider adding column aliases.");
                var couldBeNull = neverNullFields == null || !neverNullFields.Contains(name);
                var type = CreateClrType(member.FieldType, name, new TypePath { Parent = path, StructField = name });
                if (couldBeNull && type.IsValueType && !SerializerCreationContext.IsDefaultIsNullishValueType(type))
                {
                    type = typeof(Nullable<>).MakeGenericType(type);
                }
                var fieldBuilder = classBuilder.DefineField(name, type, FieldAttributes.Public);
                if (!type.IsValueType)
                {
                    if (couldBeNull) fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(MaybeNullAttribute).GetConstructor(Array.Empty<Type>()), Array.Empty<object>()));
                    else fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(NotNullAttribute).GetConstructor(Array.Empty<Type>()), Array.Empty<object>()));
                }
                fieldBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(DuckDbIncludeAttribute).GetConstructor(new[] { typeof(string) }), new object[] { name }));
            }
            var t = classBuilder.CreateType();
            Paths?.Add((t, typeKey, path));
            return t;
        }

        private static bool IsValidCSharpIdentifier(string? name, bool allowInvalidCSharpIdentifiers)
        {
            return !string.IsNullOrEmpty(name) && (allowInvalidCSharpIdentifiers || Regex.IsMatch(name, @"^[\p{L}_]\w*$"));
        }

        internal (Type Type, DuckDbStructuralType StructuralType) GenerateCSharpTypeForQuery(TypedDuckDbConnectionBase conn, string? nameHint, string sql, QueryParameterInfo[]? parameters, CodeGenerationOptions options, SerializerSpecification spec)
        {
            var structuralType = TryGetPossiblyCachedResultStructuralType(conn, sql, parameters, options);
            if (structuralType is null) return default;

            var type = CreateClrType(structuralType, nameHint, new TypePath { RootSql = sql, RootSqlName = nameHint, RootParameters = parameters, RootSpec = spec });
            return (type, structuralType);
        }

        internal static DuckDbStructuralType TryGetPossiblyCachedResultStructuralType(TypedDuckDbConnectionBase conn, string sql, QueryParameterInfo[]? parameters, CodeGenerationOptions options)
        {
            var parameterTypeNames = parameters?.Select(x => options.ResolveType(x.Type).FullName).ToArray() ?? Array.Empty<string>();
            var cached = options.QueryTypeCache.Queries.SingleOrDefault(x => x.Sql == sql && (x.ParameterTypes ?? Array.Empty<string>()).SequenceEqual(parameterTypeNames));

            DuckDbStructuralType structuralType;

            if (cached != null && options.CacheBehavior == QueryInfoCacheBehavior.PreferCache)
            {
                structuralType = ToStructuralType(cached.JsonStructuralTypeReference, options.QueryTypeCache);
            }
            else
            {
                if(options.LogToStderr)
                    Console.Error.WriteLine($"Determining result type for {DuckDbUtils.ToSingleLineSql(sql)}");

                try
                {
                    var args = DuckDbUtils.GetExampleParameterValues(parameters, options, sql);
                    using var result = conn.ExecuteUnsafe($"select * from ({sql}) limit 0", args);
                    structuralType = DuckDbStructuralType.CreateStructuralTypeForResult(result);
                }
                catch (Exception ex)
                {
                    if (options.CacheBehavior == QueryInfoCacheBehavior.ForbidCache || cached == null)
                    {
                        WriteWarning($"Could not get result type for {sql}: {ex}, and no previously cached type exists.");
                        return null;
                    }

                    WriteWarning($"Could not get result type for {sql}: {ex}. Using previously cached type.");
                    structuralType = ToStructuralType(cached.JsonStructuralTypeReference, options.QueryTypeCache);
                }
            }
            if (cached == null)
            {
                cached = new();
                options.QueryTypeCache.Queries.Add(cached);
            }
            cached.ParameterTypes = parameterTypeNames is { Length: > 0 } ? parameterTypeNames : null;
            cached.Sql = sql;
            cached.JsonStructuralTypeReference = ToJsonStructuralType(structuralType, options.QueryTypeCache);
            return structuralType;
        }

        private static void WriteWarning(string v)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(v);
            Console.ResetColor();
        }

        private static DuckDbStructuralType ToStructuralType(string id, QueryTypeCache queryTypeCache)
        {
            var s = queryTypeCache.TypesById[id];
            var hash = StructuralTypeHash.Parse(id);
            return DuckDbStructuralType.CreateOrReuseStructuralType(hash, () =>
            {
                if (s.Kind == DUCKDB_TYPE.DUCKDB_TYPE_ENUM) return new DuckDbStructuralType(hash, s.Kind)
                {
                    EnumMembers = s.EnumMembers,
                };
                if (s.Kind == DUCKDB_TYPE.DUCKDB_TYPE_LIST) return new DuckDbStructuralType(hash, s.Kind)
                {
                    ElementType = ToStructuralType(s.ElementTypeId, queryTypeCache)
                };
                if (s.Kind == DUCKDB_TYPE.DUCKDB_TYPE_STRUCT) return new DuckDbStructuralType(hash, s.Kind)
                {
                    StructureFields = s.StructureFields.Select(x => new StructuralTypeStructureField(x.Name, ToStructuralType(x.FieldTypeId, queryTypeCache))).ToList()
                };
                return new DuckDbStructuralType(hash, s.Kind);
            });

        }

        private static string ToJsonStructuralType(DuckDbStructuralType s, QueryTypeCache typeCache)
        {
            var id = s.Hash.ToString();
            if (!typeCache.TypesById.TryGetValue(id, out var jsonType))
            {
                jsonType = new JsonStructuralType
                {
                    Id = id,
                    Kind = s.Kind,
                    ElementTypeId = s.ElementType is not null ? ToJsonStructuralType(s.ElementType, typeCache) : null,
                    EnumMembers = s.EnumMembers,
                    StructureFields = s.StructureFields?.Select(x => new JsonStructuralTypeStructureField(x.Name, ToJsonStructuralType(x.FieldType, typeCache))).ToList()
                };
                typeCache.TypesById.Add(id, jsonType);
                typeCache.Types.Add(jsonType);
            }
            return id;
        }

        public void AddKnownClrType(Type type)
        {
            var structural = DuckDbStructuralType.CreateStructuralType(type);
            this.ClrTypeCache!.TryAdd(structural.GetTypeKey(), type);
        }

        public readonly static TypeGenerationContext Global = new(allowInvalidCSharpIdentifiers: true);

    }
}

