using DuckDbSharp.Functions;
using DuckDbSharp.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DuckDbSharp.Reflection
{
    public unsafe class CodeGenerationOptions
    {
        public string? DestinationPath;
        public string? Namespace;
        public string? FullTypeNameForQueries;
        public string? FullTypeNameForAotSerializers;
        public IReadOnlyList<SerializerSpecification> Specifications;
        public bool DetectNullability = true;
        public bool GenerateAotSerializers;
        public Type[]? TryReuseTypes;
        public Type[]? CreateNamedEnumTypes;
        public TypedDuckDbConnectionBase? Connection;
        public Assembly[]? AssembliesForQueryFileTypeResolution;
        public string? QueryTypeCachePath;
        internal QueryTypeCache QueryTypeCache;
        public QueryInfoCacheBehavior CacheBehavior = QueryInfoCacheBehavior.PreferCache;

        public Type GetTypeByName(string name)
        {
            if (name.EndsWith("[]", StringComparison.Ordinal))
            {
                return GetTypeByName(name.Substring(0, name.Length - 2)).MakeArrayType();
            }
            var wellknown = ParsePrimitiveType(name, throwOnError: false);
            if (wellknown != null) return wellknown;
            var assembliesForTypeNameLookup = AssembliesForQueryFileTypeResolution;
            if (assembliesForTypeNameLookup == null || assembliesForTypeNameLookup.Length == 0) throw new ArgumentException($"Unable to find type {name}, because no assemblies were passed to {nameof(CodeGenerationOptions)}.{nameof(CodeGenerationOptions.AssembliesForQueryFileTypeResolution)}.");
            var isFullName = name.Contains('.');
            var candidates = isFullName
                ? assembliesForTypeNameLookup.Select(x => x.GetType(name, throwOnError: false)).Where(x => x != null).ToArray()
                : assembliesForTypeNameLookup.SelectMany(x => FunctionUtils.GetTypesBestEffort(x).Where(x => x.Name == name)).ToArray();
            candidates = candidates.Where(x => x.GetCustomAttribute<DuckDbGeneratedTypeAttribute>() == null).ToArray();
            if (candidates.Length == 0) throw new ArgumentException($"Could not find a type named '{name}' in any of the provided assemblies.");
            if (candidates.Length == 1) return candidates[0];
            throw new InvalidOperationException($"Multiple types named '{name}' were found in the provided assemblies." + (!isFullName ? " Try specifying a namespace-qualified name instead." : null));

        }

        internal Type ResolveType(PossiblyUnresolvedType? type)
        {
            if (type == null) return null;
            if (type.Value.Type is { } t) return t;
            return GetTypeByName(type.Value.UnresolvedType);
        }

        private static Type? ParsePrimitiveType(string name, bool throwOnError)
        {
            name = name.Trim();
            if (name == "blob") return typeof(byte[]);
            if (name == "uuid") return typeof(DuckDbUuid);
            var type = CSharpCodeWriter.PrimitiveTypes.FirstOrDefault(x => x.CSharp == name).Type;
            if (type != null) return type;
            type = typeof(object).Assembly.GetType("System." + name, throwOnError: throwOnError);
            return type;
        }
    }
}

