using System;

namespace DuckDbSharp.Reflection
{
    public record struct QueryParameterInfo(string? Name, PossiblyUnresolvedType Type)
    {
        public object GetExampleValue(CodeGenerationOptions options)
        {
            var t = options?.ResolveType(Type) ?? Type.Type;
            if (t.IsValueType) return Activator.CreateInstance(t);
            if (t == typeof(string)) return "a";
            if (t.IsArray)
            {
                return Array.CreateInstance(t.GetElementType(), 0);
            }
            throw new NotImplementedException();
        }
    }
}

