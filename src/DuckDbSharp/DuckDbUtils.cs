using DuckDbSharp.Bindings;
using DuckDbSharp.Functions;
using DuckDbSharp.Reflection;
using DuckDbSharp.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace DuckDbSharp
{
    public unsafe static class DuckDbUtils
    {
        public const int STANDARD_VECTOR_SIZE = 2048;

        public static void RegisterPrimitiveSerializerBitcast(Type clrType, DUCKDB_TYPE duckDbType)
        {
            var example = SerializerCreationContext.PrimitiveConverters.FirstOrDefault(x => x.Kind == duckDbType);
            if (example != null && Marshal.SizeOf(clrType) != Marshal.SizeOf(example.ClrType))
                throw new ArgumentException("Incompatible size for a bitcast. Consider specifying a serialization and deserialization method.");
            AddCustomSerializer(new DuckDbPrimitiveTypeConverter(clrType, duckDbType));
        }

        public static void RegisterPrimitiveSerializer(Type clrType, DUCKDB_TYPE duckDbType, MethodInfo serialize, MethodInfo deserialize, MethodInfo? isNullish = null)
        {
            AddCustomSerializer(new DuckDbPrimitiveTypeConverter(clrType, duckDbType, serialize, deserialize, isNullish));
        }

        private static void AddCustomSerializer(DuckDbPrimitiveTypeConverter converter)
        {
            while (true)
            {
                var converters = SerializerCreationContext.PrimitiveConverters;
                if (converters.Any(x => x.ClrType == converter.ClrType))
                    throw new InvalidOperationException($"A primitive serializer is already registered for type {converter.ClrType}");
                var converters2 = converters.ToList();

                converters2.Add(converter);
                var original = Interlocked.CompareExchange(ref SerializerCreationContext.PrimitiveConverters, converters2, converters);
                if (original == converters) break;
            }

        }

        internal static OwnedDuckDbDatabase OpenDatabase(string? path)
        {
            DuckDbUtils.LoadDuckDbDll();
            _duckdb_database* db = default;
            using var pathUtf8 = (ScopedString)path;
            _duckdb_config* config = null;
            BindingUtils.CheckState(Methods.duckdb_create_config(&config));
            try
            {
                byte* error = null;
                if (Methods.duckdb_open_ext(pathUtf8, &db, config, &error) != duckdb_state.DuckDBSuccess)
                    throw new DuckDbException(Marshal.PtrToStringUTF8((nint)error));
                return (OwnedDuckDbDatabase)db;
            }
            finally
            {
                Methods.duckdb_destroy_config(&config);
            }

        }

        public static OwnedDuckDbConnection Connect(string? path, out DuckDbDatabase ownerDb)
        {
            return DuckDbDatabase.AcquireConnection(path, out ownerDb);
        }

        internal static OwnedDuckDbConnection ConnectCore(_duckdb_database* db)
        {
            _duckdb_connection* conn = null;
            BindingUtils.CheckState(Methods.duckdb_connect(db, &conn));
            return new OwnedDuckDbConnection(conn);
        }

        internal static OwnedDuckDbConnection ConnectCore(string? path)
        {
            var db = DuckDbUtils.OpenDatabase(path);
            _duckdb_connection* conn = null;
            BindingUtils.CheckState(Methods.duckdb_connect(db, &conn));
            return new(conn, () => db.Dispose());
        }

        private static OwnedDuckDbPreparedStatement CreatePreparedStatement(_duckdb_connection* conn, string sql)
        {
            using var sqlBytes = (ScopedString)sql;
            using var prepared = OwnedDuckDbPreparedStatement.Allocate();
            if (Methods.duckdb_prepare(conn, sqlBytes, &prepared.Pointer) != duckdb_state.DuckDBSuccess)
                throw new DuckDbException(DuckDbUtils.ToStringUtf8(Methods.duckdb_prepare_error(prepared)));
            return prepared.Move();
        }




        private static long lastGeneratedToken;

        internal unsafe static OwnedDuckDbResult ExecuteCore(_duckdb_connection* conn, string sql, object?[]? parameters, List<EnumerableParameterSlot>? enumerableParameterSlots)
        {
            if (parameters != null && parameters.Length != 0)
            {
                var token = new EnumerableParametersInvocationToken(Interlocked.Increment(ref lastGeneratedToken));

                var simpleParameters = new List<object?>();
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (IsEnumerableParameter(parameters[i]))
                    {
                        RegisterQueryParameterFunction(conn, token, i - simpleParameters.Count, enumerableParameterSlots, parameters[i]);
                    }
                    else simpleParameters.Add(parameters[i]);
                }
                using var prepared = CreatePreparedStatement(conn, sql);
                var simpleParamCount = Methods.duckdb_nparams(prepared);

                if (simpleParamCount != (ulong)simpleParameters.Count) throw new DuckDbException($"Expected {simpleParamCount} parameters, but {simpleParameters} were provided.");

                for (ulong i = 1; i <= simpleParamCount; i++)
                {
                    var j = (int)i - 1;
                    var val = simpleParameters[j];
                    if (val == null) BindingUtils.CheckState(Methods.duckdb_bind_null(prepared, i));
                    /* else
                     {
                         var clrType = val.GetType();
                         var primitiveConverter = SerializerCreationContext.PrimitiveConverters.First(x => x.ClrType == clrType);

                     }*/
                    else if (val is bool b) BindingUtils.CheckState(Methods.duckdb_bind_boolean(prepared, i, b ? (byte)1 : (byte)0));
                    else if (val is ulong u64) BindingUtils.CheckState(Methods.duckdb_bind_uint64(prepared, i, u64));
                    else if (val is long i64) BindingUtils.CheckState(Methods.duckdb_bind_int64(prepared, i, i64));
                    else if (val is uint u32) BindingUtils.CheckState(Methods.duckdb_bind_uint32(prepared, i, u32));
                    else if (val is int i32) BindingUtils.CheckState(Methods.duckdb_bind_int32(prepared, i, i32));
                    else if (val is ushort u16) BindingUtils.CheckState(Methods.duckdb_bind_uint16(prepared, i, u16));
                    else if (val is short i16) BindingUtils.CheckState(Methods.duckdb_bind_int16(prepared, i, i16));
                    else if (val is byte u8) BindingUtils.CheckState(Methods.duckdb_bind_uint8(prepared, i, u8));
                    else if (val is sbyte i8) BindingUtils.CheckState(Methods.duckdb_bind_int8(prepared, i, i8));
                    else if (val is float f) BindingUtils.CheckState(Methods.duckdb_bind_float(prepared, i, f));
                    else if (val is double d) BindingUtils.CheckState(Methods.duckdb_bind_double(prepared, i, d));
                    else if (val is DateTime dt) BindingUtils.CheckState(Methods.duckdb_bind_timestamp(prepared, i, SerializationHelpers.BitCast<DuckDbTimestampMicros, duckdb_timestamp>(DuckDbTimestampMicros.FromDateTime(dt))));
                    else if (val is DateOnly don) BindingUtils.CheckState(Methods.duckdb_bind_date(prepared, i, SerializationHelpers.BitCast<int, duckdb_date>(SerializationHelpers.SerializeDateOnly(don))));
                    else if (val is DuckDbTimestampMicros tsus) BindingUtils.CheckState(Methods.duckdb_bind_timestamp(prepared, i, SerializationHelpers.BitCast<DuckDbTimestampMicros, duckdb_timestamp>(tsus)));
                    else if (val is DuckDbInterval iv) BindingUtils.CheckState(Methods.duckdb_bind_interval(prepared, i, SerializationHelpers.BitCast<DuckDbInterval, duckdb_interval>(iv)));
                    else if (val is TimeSpan tspan) BindingUtils.CheckState(Methods.duckdb_bind_interval(prepared, i, SerializationHelpers.BitCast<DuckDbInterval, duckdb_interval>(DuckDbInterval.FromTimeSpan(tspan))));
                    else if (val is Int128 i128) BindingUtils.CheckState(Methods.duckdb_bind_hugeint(prepared, i, SerializationHelpers.BitCast<Int128, duckdb_hugeint>(i128)));
                    else if (val is string str)
                    {
                        using var s = (ScopedString)str;
                        BindingUtils.CheckState(Methods.duckdb_bind_varchar_length(prepared, i, s, (ulong)s.Length));
                    }
                    else if (val is DuckDbUuid uuid)
                    {
                        using var s = (ScopedString)uuid.ToString();
                        BindingUtils.CheckState(Methods.duckdb_bind_varchar_length(prepared, i, s, (ulong)s.Length));
                    }
                    else if (val is byte[] blob)
                    {
                        fixed (byte* ptr = blob)
                        {
                            BindingUtils.CheckState(Methods.duckdb_bind_blob(prepared, i, ptr, (ulong)blob.Length));
                        }
                    }
                    else if (val is Memory<byte> m)
                    {
                        fixed (byte* ptr = m.Span)
                        {
                            BindingUtils.CheckState(Methods.duckdb_bind_blob(prepared, i, ptr, (ulong)m.Length));
                        }
                    }
                    else if (val is ReadOnlyMemory<byte> rom)
                    {
                        fixed (byte* ptr = rom.Span)
                        {
                            BindingUtils.CheckState(Methods.duckdb_bind_blob(prepared, i, ptr, (ulong)rom.Length));
                        }
                    }
                    else if (IsEnum(val.GetType()))
                    {
                        using var s = (ScopedString)val.ToString();
                        BindingUtils.CheckState(Methods.duckdb_bind_varchar_length(prepared, i, s, (ulong)s.Length));
                    }
                    else throw new DuckDbException($"Don't know how to bind parameter of type {val.GetType()}.");
                }

                using var result = OwnedDuckDbResult.Allocate(simpleParamCount != (ulong)parameters.Length ? () =>
                {
                    lock (enumerableParameterSlots)
                    {
                        foreach (var item in enumerableParameterSlots)
                        {
                            item.ValueByQueryToken.Remove(token);
                        }
                    }
                }
                : null);

                if (Methods.duckdb_execute_prepared(prepared, result.Pointer) != duckdb_state.DuckDBSuccess)
                    throw new DuckDbException(DuckDbUtils.ToStringUtf8(Methods.duckdb_result_error(result)));
                return result.Move();
            }
            else
            {
                using var result = OwnedDuckDbResult.Allocate(null);
                using var sqlBytes = (ScopedString)sql;
                if (Methods.duckdb_query(conn, sqlBytes, result) != duckdb_state.DuckDBSuccess)
                    throw new DuckDbException(DuckDbUtils.ToStringUtf8(Methods.duckdb_result_error(result)));
                return result.Move();
            }


        }

        private static void RegisterQueryParameterFunction(_duckdb_connection* conn, EnumerableParametersInvocationToken token, int i, List<EnumerableParameterSlot?> slots, object value)
        {
            var parameterId = i + 1;
            var funcname = "enumerable_parameter_internal_" + parameterId;

            lock (slots)
            {
                while (slots.Count <= i)
                    slots.Add(null);
                var slot = slots[i];
                if (slot == null)
                {
                    slot = new EnumerableParameterSlot { ParameterId = parameterId };

                    slot.Function = FunctionUtils.RegisterFunction(conn, funcname, (long token) =>
                    {
                        lock (slots)
                        {
                            if (slot.ValueByQueryToken.TryGetValue(new(token), out var result))
                            {
                                return result;
                            }
                            throw new ArgumentException("Invalid or null enumerable-valued parameter reference.");
                        }
                    });
                    slots[i] = slot;
                }
                slot.ValueByQueryToken.Add(token, value);
            }
            ExecuteCore(conn, $"create or replace temp macro table_parameter_{parameterId}() as table (select * from {funcname}({token.Id}))", null, null);
            ExecuteCore(conn, $"create or replace temp macro array_parameter_{parameterId}() as (select array_agg(f) as array from {funcname}({token.Id}) f)", null, null);
        }


        private static bool IsEnumerableParameter(object? v)
        {
            if (v == null) return false;
            var type = v.GetType();
            return TypeSniffedEnumerable.TryGetEnumerableElementType(type) != null;
        }

        internal static IEnumerable Execute(nint conn, string sql, object[] parameters, List<EnumerableParameterSlot>? enumerableParameterSlots) => Execute((_duckdb_connection*)conn, sql, parameters, enumerableParameterSlots);
        internal static IEnumerable<T> Execute<T>(nint conn, string sql, object[] parameters, List<EnumerableParameterSlot>? enumerableParameterSlots) => Execute<T>((_duckdb_connection*)conn, sql, parameters, enumerableParameterSlots);

        public static IEnumerable<T> Execute<T>(_duckdb_connection* conn, string sql, object[] parameters, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            PrepareExecute<T>(conn, sql, parameters, out var result, out var structuralType, enumerableParameterSlots);
            var enumerable = EnumerateResultsCore<T>(result, structuralType, enumerableParameterSlots);
            result.Move();
            return enumerable;
        }

        internal static IEnumerable<T[]> ExecuteBatched<T>(nint conn, string sql, object[] parameters, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            PrepareExecute<T>((_duckdb_connection*)conn, sql, parameters, out var result, out var structuralType, enumerableParameterSlots);
            var enumerable = EnumerateResultsBatchedCore<T>(result, structuralType, new StrongBox<bool>(), enumerableParameterSlots);
            result.Move();
            return enumerable;
        }

        internal static void PrepareExecute<T>(_duckdb_connection* conn, string sql, object[] parameters, out OwnedDuckDbResult result, out DuckDbStructuralType structuralType, List<EnumerableParameterSlot> enumerableParameterSlots)
        {
            result = ExecuteCore(conn, sql, parameters, enumerableParameterSlots);
            structuralType = DuckDbStructuralType.CreateStructuralTypeForResult(result, typeof(T));
        }

        internal static DuckDbStructuralType GetSingleWrappedColumnType(DuckDbStructuralType structuralType)
        {
            if (structuralType.StructureFields.Count > 1) throw new ArgumentException($"{structuralType.StructureFields.Count} columns were selected, but only one output type was specified.");
            structuralType = structuralType.StructureFields.Single().FieldType;
            return structuralType;
        }

        public static void ExecuteNonQuery(_duckdb_connection* conn, string sql, object[] parameters, List<EnumerableParameterSlot>? enumerableParameterSlots)
        {
            using var _ = ExecuteCore(conn, sql, parameters, enumerableParameterSlots);
        }

        public static IEnumerable Execute(_duckdb_connection* conn, string sql, object[] parameters, List<EnumerableParameterSlot>? enumerableParameterSlots)
        {
            return Execute(conn, sql, parameters, expectSingleColumn: false, enumerableParameterSlots);
        }

        internal static IEnumerable Execute(_duckdb_connection* conn, string sql, object[] parameters, bool expectSingleColumn, List<EnumerableParameterSlot> enumerableParameterSlots)
        {
            using var result = ExecuteCore(conn, sql, parameters, enumerableParameterSlots);

            var resultStructuralType = DuckDbStructuralType.CreateStructuralTypeForResult(result, expectSingleColumn);
            if (!SerializerCreationContext.StructuralHashToRegisteredClrType.TryGetValue(resultStructuralType.Hash, out var resultType))
            {
                lock (TypeGenerationContext.Global)
                {
                    resultType = TypeGenerationContext.Global.CreateClrType(resultStructuralType, null, null);
                }
            }
            var enumerable = (IEnumerable)EnumerateResultsCoreMethod.MakeGenericMethod(resultType).Invoke(null, new object[] { result, resultStructuralType, enumerableParameterSlots })!;
            result.Move();
            return enumerable;
        }

        private static IEnumerable<T> EnumerateResultsCore<T>(OwnedDuckDbResult result, DuckDbStructuralType duckType, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            return EnumerateResultsCore2<T>(result, duckType, new StrongBox<bool>(), enumerableParameterSlots);
        }

        private static IEnumerable<T> EnumerateResultsCore2<T>(OwnedDuckDbResult result, DuckDbStructuralType duckType, StrongBox<bool> used, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            if (used.Value) throw new Exception("Results cannot be enumerated more than once.");
            used.Value = true;
            try
            {
                var deserializer = CreateRootDeserializer<T>(duckType);
                ulong chunkIdx = 0;
                while (true)
                {
                    var array = FetchAndDeserializeChunk<T>(deserializer, result.PointerAsIntPtr, chunkIdx++);
                    if (array == null) break;
                    foreach (var item in array)
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                result.Dispose();
            }
        }


        private static IEnumerable<T[]> EnumerateResultsBatchedCore<T>(OwnedDuckDbResult result, DuckDbStructuralType duckType, StrongBox<bool> used, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            if (used.Value) throw new Exception("Results cannot be enumerated more than once.");
            used.Value = true;
            try
            {
                var deserializer = CreateRootDeserializer<T>(duckType);
                ulong chunkIdx = 0;
                while (true)
                {
                    var array = FetchAndDeserializeChunk<T>(deserializer, result.PointerAsIntPtr, chunkIdx++);
                    if (array == null) break;
                    yield return array;
                }
            }
            finally
            {
                result.Dispose();
            }
        }

        private static RootDeserializer CreateRootDeserializer<T>(DuckDbStructuralType duckType)
        {
            RootDeserializer deserializer;
            lock (SerializerCreationContext.Global)
            {
                deserializer = SerializerCreationContext.Global.CreateRootDeserializer(typeof(T), duckType);
            }

            return deserializer;
        }



        private unsafe static T[]? FetchAndDeserializeChunk<T>(RootDeserializer deserializer, nint result, ulong chunkIndex)
        {
            using var chunk = (OwnedDuckDbDataChunk)Methods.duckdb_result_get_chunk(*(duckdb_result*)result, chunkIndex);
            if (chunk.Pointer == null) return null;
            var array = (T[])deserializer((nint)chunk.Pointer);
            return array;
        }

        record struct NamespaceAndName(string Namespace, string Name);
        private static NamespaceAndName GetNamespaceAndName(string fullName, string defaultNs)
        {
            var dot = fullName.LastIndexOf('.');
            if (dot != -1)
            {
                return new(fullName.Substring(0, dot), fullName.Substring(dot + 1));
            }
            else
            {
                return new(defaultNs, fullName);
            }
        }

        private static void AddTypesToPossiblyReuse(Type type, HashSet<Type> knownTypes, List<Type> knownTypesList)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            DuckDbTypeCreator.GetDuckDbType(type, null, out var primitiveType, out var sublistElementType, out _, out var structureFields);
            if (sublistElementType != null)
            {
                AddTypesToPossiblyReuse(sublistElementType, knownTypes, knownTypesList);
                return;
            }

            if (structureFields != null)
            {
                if (!knownTypes.Add(type)) return;
                knownTypesList.Add(type);
                foreach (var field in structureFields)
                {
                    AddTypesToPossiblyReuse(field.FieldType, knownTypes, knownTypesList);
                }
                return;
            }

            if (primitiveType.IsEnum)
            {
                knownTypesList.Add(type);
            }
        }

        public static void GenerateCSharpTypes(CodeGenerationOptions options)
        {
            var conn = options.Connection;
            using var tempConnection = conn == null ? ThreadSafeTypedDuckDbConnection.CreateInMemory() : null;
            if (conn == null) conn = tempConnection;
            options.Namespace ??= "Untitled";
            options.FullTypeNameForAotSerializers ??= options.Namespace + ".AotSerializers";
            options.FullTypeNameForQueries ??= options.Namespace + ".Queries";
            options.DestinationPath ??= "DuckDbGeneratedCode.cs";
            options.QueryTypeCachePath ??= Path.ChangeExtension(options.DestinationPath, ".json");
            if (File.Exists(options.QueryTypeCachePath))
            {
                options.QueryTypeCache = JsonSerializer.Deserialize<QueryTypeCache>(File.ReadAllText(options.QueryTypeCachePath));
            }
            else
                options.QueryTypeCache = new();
            options.QueryTypeCache.TypesById = options.QueryTypeCache.Types.ToDictionary(x => x.Id);

            Type? ResolveType(PossiblyUnresolvedType? type) => options.ResolveType(type);

            if (!options.DestinationPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException($"{nameof(options.DestinationPath)} must end in '.cs'.");

            var typesToPossiblyReuseHashSet = new HashSet<Type>();
            var typesToPossiblyReuseList = new List<Type>();
            if (options.TryReuseTypes != null)
            {
                foreach (var type in options.TryReuseTypes)
                {
                    AddTypesToPossiblyReuse(type, typesToPossiblyReuseHashSet, typesToPossiblyReuseList);
                }
            }
            options.Specifications = options.Specifications.SelectMany(x => x.queriesDirectory != null ? x.queriesDirectory.EnumerateFiles("*.sql").Select(x => new SerializerSpecification(x)) : new[] { x }).ToList();
            foreach (var spec in options.Specifications)
            {
                if (spec.Type != null)
                {
                    AddTypesToPossiblyReuse(ResolveType(spec.Type), typesToPossiblyReuseHashSet, typesToPossiblyReuseList);
                }
            }

            if (options.CreateNamedEnumTypes != null)
            {
                foreach (var type in options.CreateNamedEnumTypes)
                {
                    AddTypesToPossiblyReuse(type, typesToPossiblyReuseHashSet, typesToPossiblyReuseList);
                    CreateEnumType(conn.Handle, type);
                }
            }

            var destinationPathForSerializers = options.GenerateAotSerializers ? options.DestinationPath[..^3] + ".AotSerializers.cs" : null;

            var sqlNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var spec in options.Specifications)
            {
                if (spec.SqlName != null && !sqlNames.Add(spec.SqlName))
                    throw new ArgumentException($"Duplicate query name: {spec.SqlName}");
            }

            var typeCheckingCtx = new SerializerCreationContext();

            var specToGeneratedType = new Dictionary<SerializerSpecification, (Type ClrType, DuckDbStructuralType StructuralType)>();
            Action<TypeGenerationContext> action = ctx =>
            {
                if (ctx.ClrTypeCache != null)
                {
                    foreach (var type in typesToPossiblyReuseList)
                    {
                        ctx.AddKnownClrType(type);
                    }
                }
                foreach (var spec in options.Specifications)
                {
                    if (spec.Type == null)
                    {
                        var sql = spec.GetSql();
                        var generatedType = ctx.GenerateCSharpTypeForQuery(conn, spec.SqlName, sql, spec.Parameters, options, spec);
                        if (generatedType == default) continue;
                        specToGeneratedType.Add(spec, (generatedType.Type, generatedType.StructuralType));
                    }
                    else
                    {
                        if (spec.SqlName != null)
                        {
                            var sql = spec.GetSql();
                            if (options.LogToStderr)
                                Console.Error.WriteLine($"Checking types for {ToSingleLineSql(sql)}");

                            var args = DuckDbUtils.GetExampleParameterValues(spec.Parameters, options, spec.GetSql());
                            //using var result = DuckDbUtils.ExecuteCore(conn.Handle, $"select * from ({sql}) limit 0", args, conn.EnumerableParameterSlots);
                            var r = TypeGenerationContext.TryGetPossiblyCachedResultStructuralType(conn, sql, spec.Parameters, options);
                            if (r is null) continue;
                            var structuralType = DuckDbStructuralType.CreateStructuralTypeForResult(r, ResolveType(spec.Type));
                            typeCheckingCtx.CreateRootDeserializerCore(ResolveType(spec.Type), structuralType);
                        }
                    }
                }

            };


            Dictionary<TypeKey, List<string>>? alwaysNonNullFields = null;
            if (options.DetectNullability)
            {
                var ctx = new TypeGenerationContext(forNullnessCheck: true) { Namespace = options.Namespace };
                action(ctx);
                var cacheFile = options.DestinationPath[..^3] + ".nullnesscache";
                var neverNullCache = new Dictionary<NullnessCacheKey, bool>();
                if (File.Exists(cacheFile))
                {
                    foreach (var line in File.ReadAllLines(cacheFile))
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        var parts = line.Split('|');
                        neverNullCache.Add(new NullnessCacheKey(parts[0], parts[1]), parts[2] == "1");
                    }
                }
                alwaysNonNullFields = NullabilityDetection.FindAlwaysNonNullFields(conn, ctx, neverNullCache, options);
                File.WriteAllLines(cacheFile, neverNullCache.Select(x => $"{x.Key.Path}|{x.Key.FieldName}|{(x.Value ? 1 : 0)}").Order(StringComparer.OrdinalIgnoreCase));
                specToGeneratedType.Clear();
            }


            var ctx2 = new TypeGenerationContext(forNullnessCheck: false) { NeverNullFields = alwaysNonNullFields, Namespace = options.Namespace };
            action(ctx2);

            if (destinationPathForSerializers != null)
            {
                File.WriteAllText(destinationPathForSerializers, GenerateCSharpSerializationMethods(conn, GetNamespaceAndName(options.FullTypeNameForAotSerializers ?? "AotSerializers", options.Namespace), options.Specifications, specToGeneratedType, options));
            }
            var typeForQueries = GetNamespaceAndName(options.FullTypeNameForQueries ?? "Queries", options.Namespace);
            var sw = new StringWriter();
            var writer = new CSharpCodeWriter(sw);
            writer.WriteLine("// <autogenerated />");
            writer.WriteLine();
            writer.WriteLine("#nullable enable");
            writer.WriteLine("#pragma warning disable CS8618");
            writer.WriteLine();
            writer.WriteLine("using DuckDbIncludeAttribute = DuckDbSharp.DuckDbIncludeAttribute;");
            writer.WriteLine("using DuckDbGeneratedTypeAttribute = DuckDbSharp.DuckDbGeneratedTypeAttribute;");
            writer.WriteLine("using TypedDuckDbConnectionBase = DuckDbSharp.TypedDuckDbConnectionBase;");
            writer.WriteLine();
            writer.WriteNamespace(typeForQueries.Namespace);
            writer.Write("    public static class ");
            writer.WriteLine(typeForQueries.Name);
            writer.WriteLine("    {");
            foreach (var spec in options.Specifications)
            {
                if (spec.SqlName != null)
                {
                    var returnType = ResolveType(spec.Type) ?? specToGeneratedType[spec].ClrType;
                    writer.WriteLine("        /// <summary>");
                    writer.WriteLine("        /// <code>");
                    foreach (var line in (spec.Comment ?? spec.GetSql()).Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        writer.Write("        /// ");
                        writer.WriteLine(line.TrimEnd());
                    }
                    writer.WriteLine("        /// </code>");
                    writer.WriteLine("        /// </summary>");
                    writer.Write("        public static System.Collections.Generic.IEnumerable<");
                    writer.WriteTypeReference(returnType, false);
                    writer.Write("> ExecuteQuery_" + spec.SqlName + "(this TypedDuckDbConnectionBase db");
                    if (spec.Parameters != null)
                    {
                        foreach (var p in spec.Parameters)
                        {
                            writer.Write(", ");
                            writer.WriteTypeReference(ResolveType(p.Type), false);
                            writer.Write(" ");
                            writer.Write(p.Name);
                        }
                    }
                    writer.WriteLine(")");
                    writer.WriteLine("        {");
                    writer.Write("            return db.Execute<");
                    writer.WriteTypeReference(returnType, false);
                    writer.Write(">(");
                    writer.WriteLine();
                    writer.WriteString(spec.GetSql());
                    if (spec.Parameters != null && spec.Parameters.Length != 0)
                    {
                        writer.Write(", new object[] { "); // Otherwise string[] is interpreted as params object[] parameters
                        for (int i = 0; i < spec.Parameters.Length; i++)
                        {
                            if (i != 0) writer.Write(", ");
                            writer.Write(spec.Parameters[i].Name);
                        }
                        writer.Write(" }");
                    }
                    writer.WriteLine(");");
                    writer.WriteLine("        }");
                    writer.WriteLine();
                }
            }
            writer.WriteLine("    }");
            writer.Write(ctx2.ModuleBuilder.GetTypes());
            writer.Complete();
            File.WriteAllText(options.DestinationPath, sw.ToString());
            File.WriteAllText(options.QueryTypeCachePath, JsonSerializer.Serialize(options.QueryTypeCache, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, WriteIndented = true }));
        }


        internal static bool IsEnum(Type type)
        {
            if (!type.IsEnum) return false;
            if (type == typeof(HttpStatusCode)) return false;
            if (type.GetCustomAttribute<DuckDbSerializeAsAttribute>() != null) return false;
            return true;
        }

        internal static void CreateEnumType(_duckdb_connection* conn, Type type)
        {
            if (type.GetCustomAttribute<FlagsAttribute>() != null) throw new Exception("Enums marked with FlagsAttribute cannot be used in CREATE TYPE AS ENUM statements.");
            using var info = DuckDbPrimitiveTypeConverter.CreateEnumInfo(type);
            try
            {

                using var unused = ExecuteCore(conn, $"DROP TYPE {type.Name}", null, null);
            }
            catch (Exception)
            {
            }
            using var _ = ExecuteCore(conn, $"CREATE TYPE {type.Name} AS ENUM ({string.Join(", ", info.Members.Select(x => $"'{x.ToString()}'"))})", null, null);

        }

        public static string ToSingleLineSql(string sql)
        {
            return Regex.Replace(sql, @"\s+", " ").Trim();
        }

        private static string GenerateCSharpSerializationMethods(TypedDuckDbConnectionBase conn, NamespaceAndName typeName, IReadOnlyList<SerializerSpecification> specifications, Dictionary<SerializerSpecification, (Type ClrType, DuckDbStructuralType StructuralType)> specToGeneratedType, CodeGenerationOptions options)
        {
            var serializers = new SerializerCreationContext() { GeneratedMethods = new() };
            var tw = new StringWriter();
            var writer = new CSharpCodeWriter(tw);
            writer.WriteLine("// <autogenerated />");
            writer.WriteLine();
            writer.WriteLine("using SerializationHelpers = DuckDbSharp.SerializationHelpers;");
            writer.WriteLine();
            writer.WriteNamespace(typeName.Namespace);
            writer.WriteLine();
            writer.Write("    public static class ");
            writer.WriteLine(typeName.Name);
            writer.WriteLine("    {");

            foreach (var item in specifications.Where(x => x.IsForSerialization).Select(x => x.Type).Distinct())
            {
                serializers.CreateRootSerializerCore(options.ResolveType(item));
            }

            var createdRootDeserializers = new HashSet<(Type, DuckDbStructuralType)>();
            foreach (var spec in specifications.Where(x => !x.IsForSerialization))
            {
                Type clrType;
                DuckDbStructuralType structuralType;
                if (spec.Type != null)
                {
                    var r = TypeGenerationContext.TryGetPossiblyCachedResultStructuralType(conn, spec.GetSql(), spec.Parameters, options);
                    if (r is null) continue;
                    //using var r = ExecuteCore(conn.Handle, $"select * from ({spec.GetSql()}) limit 0", GetExampleParameterValues(spec.Parameters, options, spec.GetSql()), conn.EnumerableParameterSlots);
                    clrType = options.ResolveType(spec.Type);
                    structuralType = DuckDbStructuralType.CreateStructuralTypeForResult(r, clrType);
                }
                else
                {
                    var z = specToGeneratedType[spec];
                    clrType = z.ClrType;
                    structuralType = z.StructuralType;
                }

                if (createdRootDeserializers.Add((clrType, structuralType)))
                    serializers.CreateRootDeserializerCore(clrType, structuralType);
            }


            writer.WriteLine("        public static void RegisterAll()");
            writer.WriteLine("        {");

            foreach (var rootType in serializers.GeneratedMethods.Where(x => x.Delegate is RootSerializer && x.IsRootForType != null).GroupBy(x => x.IsRootForType))
            {
                var serializer = rootType.Single();
                writer.Write("            SerializationHelpers.RegisterSerializer(typeof(");
                writer.WriteTypeReference(rootType.Key, false);
                writer.Write("), ");
                writer.Write(serializer.Name);
                writer.WriteLine(");");
            }
            foreach (var rootType in serializers.GeneratedMethods.Where(x => x.Delegate is RootDeserializer && x.IsRootForType != null).GroupBy(x => x.IsRootForType))
            {
                foreach (var deserializer in rootType)
                {
                    writer.Write("            SerializationHelpers.RegisterDeserializer(typeof(");
                    writer.WriteTypeReference(rootType.Key, false);
                    writer.Write("), ");
                    writer.Write(deserializer.Name);
                    writer.Write(", ");
                    writer.WriteString(deserializer.IsRootForStructuralType.Hash.ToString());
                    writer.WriteLine(");");
                }

            }
            writer.WriteLine("        }");

            var delegateToMethod = serializers.GeneratedMethods.ToDictionary(x => x.Delegate);
            foreach (var gen in serializers.GeneratedMethods)
            {
                writer.Write("        private static ");
                writer.WriteTypeReference(gen.Delegate.Method.ReturnType, false);
                writer.Write(" ");
                writer.Write(gen.Name);
                writer.Write("(");
                if (gen.CSharpParameterNames != null)
                {
                    var clrParameters = gen.Delegate.Method.GetParameters();
                    for (int i = 0; i < gen.CSharpParameterNames.Length; i++)
                    {
                        if (i != 0) writer.Write(", ");
                        writer.WriteTypeReference(clrParameters[i].ParameterType, false);
                        writer.Write(" ");
                        writer.Write(gen.CSharpParameterNames[i]);
                    }
                }
                else
                {
                    writer.Write(gen.Parameters);
                }
                writer.WriteLine(")");
                writer.WriteLine("        {");
                if (gen.CSharpBody != null)
                {
                    writer.Write("            ");
                    writer.WriteLine(gen.CSharpBody);
                }
                else
                {
                    if (gen.Body is not BlockExpression { Expressions: { Count: 0 } })
                    {
                        writer.Write(gen.Body, x => delegateToMethod[x].Name, "            ");
                    }
                }

                writer.WriteLine("        }");
                writer.WriteLine();
            }
            writer.WriteLine("    }");
            writer.Complete();
            return tw.ToString();
        }

        private readonly static MethodInfo InsertRangeBoxedMethod = typeof(DuckDbUtils).GetMethod(nameof(InsertRangeBoxed), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);


        private static void InsertRangeBoxed<T>(nint conn, string? destinationSchema, string destinationTableOrView, IEnumerable<T> items)
        {
            InsertRange((_duckdb_connection*)conn, destinationSchema, destinationTableOrView, items.Select(x => new Box<T>() { Value = x }));
        }
        public static long InsertRange<T>(_duckdb_connection* conn, string? destinationSchema, string destinationTableOrView, IEnumerable<T> items)
        {
            if (SerializerCreationContext.IsWrappedSingleColumn(typeof(T)))
            {
                return (long)InsertRangeBoxedMethod.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { (nint)conn, destinationSchema, destinationTableOrView, items })!;
            }
            using var appender = OwnedDuckDbPreparedAppender.Allocate();

            using var destinationSchemaBytes = (ScopedString)destinationSchema;
            using var destinationTableOrViewBytes = (ScopedString)destinationTableOrView;
            BindingUtils.CheckAppenderError(Methods.duckdb_appender_create(conn, destinationSchemaBytes, destinationTableOrViewBytes, &appender.Pointer), appender);
            RootSerializer rootSerializer;
            lock (SerializerCreationContext.Global)
            {
                rootSerializer = SerializerCreationContext.Global.CreateRootSerializer(typeof(T));
            }
            using var enumerator = items.GetEnumerator();
            var cols = DuckDbTypeCreator.GetFields(typeof(T));
            using var arena = new NativeArenaSlim();
            var colTypes = BindingUtils.ToPointerArray<FieldInfo2, _duckdb_logical_type>(cols.ToArray(), x => DuckDbTypeCreator.CreateLogicalType(x.FieldType, null));
            long insertedItems = 0;
            fixed (_duckdb_logical_type** types = colTypes)
            {
                using OwnedDuckDbDataChunk chunk = (OwnedDuckDbDataChunk)Methods.duckdb_create_data_chunk(types, (ulong)colTypes.Length);

                while (true)
                {
                    var writtenItems = rootSerializer(enumerator, (nint)chunk.Pointer, arena);
                    insertedItems += writtenItems;
                    Methods.duckdb_data_chunk_set_size(chunk.Pointer, (ulong)writtenItems);
                    BindingUtils.CheckAppenderError(Methods.duckdb_append_data_chunk(appender, chunk), appender);
                    if (writtenItems != DuckDbUtils.STANDARD_VECTOR_SIZE) break;
                    Methods.duckdb_data_chunk_reset(chunk);
                }

            }

            BindingUtils.CheckAppenderError(Methods.duckdb_appender_close(appender), appender);
            

            return insertedItems;
        }

        public static object ExecuteScalar(_duckdb_connection* conn, string sql, object[] parameters, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            var box = DuckDbUtils.Execute(conn, sql, parameters, expectSingleColumn: true, enumerableParameterSlots).Cast<object>().Single();
            var fields = box.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            if (fields.Length != 1) throw new Exception($"A scalar query returned {fields.Length} columns.");
            return fields[0].GetValue(box);
        }
        internal static T ExecuteScalar<T>(_duckdb_connection* conn, string sql, object[] parameters, List<EnumerableParameterSlot?> enumerableParameterSlots)
        {
            return Execute<T>(conn, sql, parameters, enumerableParameterSlots).Single();
        }

        public static void LoadDuckDbDll()
        {
            var lib = Environment.GetEnvironmentVariable("DUCKDBSHARP_DUCKDB_DLL");
            if (!string.IsNullOrEmpty(lib))
                NativeLibrary.Load(lib);
        }

        internal static object[]? GetExampleParameterValues(QueryParameterInfo[]? parameters, CodeGenerationOptions options, string? sql)
        {
            var lines = sql?.Split('\n').Select(x => x.Trim()).Where(x => x.StartsWith("-- ExampleParam ", StringComparison.OrdinalIgnoreCase)).Select(x => Regex.Match(x, @"ExampleParam\s*(.*)$").Groups[1].Value).ToArray();
            return parameters != null ? parameters.Select((x, i) =>
            {
                var v = lines?.ElementAtOrDefault(i);
                if (v != null) return Convert.ChangeType(v, options.ResolveType(x.Type));
                return x.GetExampleValue(options);
            }).ToArray() : null;
        }

        public static IEnumerable<T> QueryParquet<T>(string parquetPath, string? query = null, params object[] parameters)
        {
            using var db = ThreadSafeTypedDuckDbConnection.CreateInMemory();
            db.ExecuteNonQuery($"CREATE TEMP VIEW data AS SELECT * FROM read_parquet('{parquetPath}') ");
            foreach (var item in db.Execute<T>(query ?? "select * from data", parameters))
            {
                yield return item;
            }
        }

        private readonly static MethodInfo EnumerateResultsCoreMethod = typeof(DuckDbUtils).GetMethod(nameof(EnumerateResultsCore), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);


        internal static string? ToStringUtf8(byte* ptr) => Marshal.PtrToStringUTF8((nint)ptr);
        internal static string? ToStringUtf8(this OwnedDuckPtr<byte> ptr) => Marshal.PtrToStringUTF8((nint)ptr.Pointer);
        internal static int StringLength(this OwnedDuckPtr<byte> ptr) => StringLength(ptr.Pointer);
        internal static int StringLength(byte* ptr)
        {
            for (int i = 0; ; i++)
            {
                if (ptr[i] == 0) return i;
            }
        }
        internal static void DisposeAll<T>(this List<OwnedDuckPtr<T>> items) where T : unmanaged
        {
            foreach (var item in items)
            {
                item.Dispose();
            }
            items.Clear();
        }


        internal static string ToPascalCase(string text)
        {
            if (char.IsUpper(text[0])) return text;
            var result = new StringBuilder();
            var firstLetter = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsLetterOrDigit(text[i]))
                {
                    if (firstLetter)
                    {
                        result.Append(char.ToUpperInvariant(text[i]));
                        firstLetter = false;
                    }
                    else
                    {
                        result.Append(text[i]);
                    }
                }
                else
                {
                    firstLetter = true;
                }
            }

            return result.ToString();
        }
        internal static void SaveToFile(this AssemblyBuilder asm, string path)
        {
            var generator = new Lokad.ILPack.AssemblyGenerator();
            generator.GenerateAssembly(asm.Modules.Single().Assembly, path);
        }
        internal readonly static string UseSnakeCaseFor = Environment.GetEnvironmentVariable("DUCKDBSHARP_USE_SNAKE_CASE");
        internal readonly static bool UseSnakeCaseForFunctions = UseSnakeCaseFor is "all" or "1" or "functions";
        internal readonly static bool UseSnakeCaseForFields = UseSnakeCaseFor is "all" or "1" or "fields";

        internal static string ToDuckCaseField(string str)
        {
            if (UseSnakeCaseForFields) return PascalCaseToSnakeCase(str);
            return str;
        }
        internal static string ToDuckCaseFunction(string str)
        {
            if (UseSnakeCaseForFunctions) return PascalCaseToSnakeCase(str);
            return str;
        }


        internal static string PascalCaseToSnakeCase(string str)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                if (char.IsUpper(ch))
                {
                    if (sb.Length != 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else if (!char.IsLetterOrDigit(ch)) sb.Append('_');
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        internal static string GetDuckName(FieldInfo field)
        {
            return field.GetCustomAttribute<DuckDbIncludeAttribute>()?.DuckName ?? field.Name;
        }

        [SkipLocalsInit]
        internal static void Write(this Stream s, StructuralTypeHash hash)
        {
            Span<UInt128> span = stackalloc UInt128[2];
            span[0] = hash.High;
            span[1] = hash.Low;
            s.Write(MemoryMarshal.AsBytes(span));
        }

        public static void WriteParquet<T>(string destinationFile, IEnumerable<T> rows, string? orderBy = null)
        {
            if (destinationFile == null) throw new ArgumentNullException();
            if (rows == null) throw new ArgumentNullException();

            using var conn = ThreadSafeTypedDuckDbConnection.CreateInMemory();
            conn.ExecuteNonQuery($"COPY (select * from table_parameter_1() {(orderBy != null ? "ORDER BY " + orderBy : null)}) TO '{destinationFile}' (FORMAT PARQUET, COMPRESSION ZSTD, USE_TMP_FILE 1)", rows);
            
        }
    }
}

