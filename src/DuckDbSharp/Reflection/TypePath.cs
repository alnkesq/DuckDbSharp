using System;
using System.Security.Cryptography;
using System.Text;

namespace DuckDbSharp.Reflection
{
    public class TypePath
    {
        public TypePath? Parent;
        public string? RootSql;
        public string? RootSqlName;
        public string? Column;
        public bool ListElement;
        public string? StructField;
        public string? FieldOrColumn => StructField ?? Column;
        public override string ToString() => ToString(false);
        public string ToString(bool forCacheKey)
        {
            return (Parent != null ? Parent.ToString() + " -> " : null) + ToStringSingle(forCacheKey);
        }
        public string ToStringSingle(bool forCacheKey)
        {
            if (RootSql != null)
            {
                if (forCacheKey) return $"RootSqlHash({RootSqlName}, {Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(RootSql)))})";
                return $"RootSql({DuckDbUtils.ToSingleLineSql(RootSql)})";
            }
            if (Column != null) return $"Column({Column})";
            if (ListElement) return "ListElement";
            if (StructField != null) return $"StructField({StructField})";
            return "(null)";
        }

        public int Id;

        public string Prefix => RootSql != null ? null : ".v";
        public QueryParameterInfo[]? RootParameters;
        public SerializerSpecification? RootSpec;
        public string FullSql
        {
            get
            {
                if (RootSql != null)
                {
                    return RootSql;
                }


                if (ListElement)
                {
                    return $"select unnest(p{Parent!.Prefix}) as v from ({Parent.FullSql}) p";
                }
                if (FieldOrColumn != null)
                {
                    var f = Parent!.Prefix + "." + FieldOrColumn;
                    return $"select p{f} as v from ({Parent.FullSql}) p where p{f} is not null";
                }
                throw new NotSupportedException();
            }
        }

        public string SqlIdentifier => Id < 26 ? ((char)('a' + (Id - 1))).ToString() : "z" + Id;
    }
}

