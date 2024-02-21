using DuckDbSharp.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DuckDbSharp.Reflection
{
    internal unsafe static class NullabilityDetection
    {
        private static bool CanTryTurningIntoNonNullable(Type fieldType)
        {
            if (!fieldType.IsValueType) return true;
            if (Nullable.GetUnderlyingType(fieldType) != null) return true;
            return false;
        }


        private static bool IsAlwaysNonNull(TypedDuckDbConnectionBase conn, FieldInfo field, TypePath path, Dictionary<NullnessCacheKey, bool> neverNullCache, CodeGenerationOptions options)
        {

            if (!CanTryTurningIntoNonNullable(field.FieldType)) return true;
            var key = new NullnessCacheKey(path.ToString(true), field.Name);
            if (neverNullCache.TryGetValue(key, out var result))
            {
                if (options.LogToStderr)
                    WriteAlwaysNonNull(result, true);
                return result;
            }
            if (options.LogToStderr)
                Console.Error.Write("Determining whether field is ever null: " + path.ToString() + " -> " + field.Name);
            result = IsAlwaysNonNullCore(conn, field, path, options);
            neverNullCache.Add(key, result);
            return result;
        }

        private static bool IsAlwaysNonNullCore(TypedDuckDbConnectionBase conn, FieldInfo field, TypePath path, CodeGenerationOptions options)
        {
            var duckFieldName = DuckDbUtils.GetDuckName(field);
            var testsql = $"select 1 from ({path.FullSql}) p where p{path.Prefix}.{duckFieldName} is null limit 1;";
            using var isEverNullQuery = (ScopedString)testsql;
            var root = path;
            while (root.Parent != null)
            {
                root = root.Parent;
            }
            using var r = DuckDbUtils.ExecuteCore(conn.Handle, testsql, DuckDbUtils.GetExampleParameterValues(root.RootParameters, options, root.RootSpec?.GetSql()), conn.EnumerableParameterSlots);
            using var chunk = (OwnedDuckDbDataChunk)Methods.duckdb_result_get_chunk(*r.Pointer, 0);
            bool alwaysNonNull = true;
            if (chunk.Pointer != null)
            {
                var rowCount = checked((int)Methods.duckdb_data_chunk_get_size(chunk));
                if (rowCount != 1) throw new Exception();
                alwaysNonNull = false;
            }

            if (options.LogToStderr)
                WriteAlwaysNonNull(alwaysNonNull, false);
            return alwaysNonNull;

        }

        private static void WriteAlwaysNonNull(bool alwaysNonNull, bool fromCache)
        {

            Console.Error.WriteLine(" = " + (alwaysNonNull ? "(mandatory)" : "(optional)") + (fromCache ? " [from cache]" : null));
        }

        public static Dictionary<TypeKey, List<string>> FindAlwaysNonNullFields(TypedDuckDbConnectionBase conn, TypeGenerationContext ctx, Dictionary<NullnessCacheKey, bool> neverNullCache, CodeGenerationOptions options)
        {
            var types = ctx.Paths
                .GroupBy(x => x.TypeKey)
                .Select(x =>
                {

                    var typeKey = x.Key;
                    var clrType = x.First().Type;
                    var paths = x.Select(x => x.Path).ToList();
                    var fields = clrType.GetFields().ToList();
                    var neverNullFields = fields.Where(field =>
                    {
                        return paths.All(x => IsAlwaysNonNull(conn, field, x, neverNullCache, options));

                    }).Select(x => DuckDbUtils.GetDuckName(x)).ToList();
                    return (typeKey, neverNullFields);
                });

            return types.ToDictionary(x => x.typeKey, x => x.neverNullFields);
        }





    }


    record struct NullnessCacheKey(string Path, string FieldName);

}

