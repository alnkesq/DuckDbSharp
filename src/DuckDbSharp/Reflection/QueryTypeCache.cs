using DuckDbSharp.Bindings;
using System.Collections.Generic;

namespace DuckDbSharp.Reflection
{
    internal class QueryTypeCache
    {
        public List<QueryInfoCache> Queries { get; set; } = new();
        public List<JsonStructuralType> Types { get; set; } = new();
        public Dictionary<string, JsonStructuralType> TypesById;
    }

    internal class JsonStructuralType
    {
        public string Id { get; set; }
        public DUCKDB_TYPE Kind { get; set; }
        public List<string> EnumMembers { get; set; }
        public string ElementTypeId { get; set; }
        public List<JsonStructuralTypeStructureField> StructureFields { get; set; }
    }

    internal record struct JsonStructuralTypeStructureField(string Name, string FieldTypeId);

    internal class QueryInfoCache
    {
        public string Sql { get; set; }
        public string[]? ParameterTypes { get; set; }
        public string JsonStructuralTypeReference { get; set; }
    }
}

