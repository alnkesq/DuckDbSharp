using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace DuckDbSharp.Reflection
{
    public class SerializerSpecification
    {
        public readonly PossiblyUnresolvedType? Type;
        public readonly string? TableOrView;
        public readonly string? Sql;
        public readonly string? SqlName;
        public readonly QueryParameterInfo[] Parameters;
        internal bool IsForSerialization => Sql == null && TableOrView == null;
        public string? Comment { get; init; }
        internal readonly DirectoryInfo? queriesDirectory;
        public SerializerSpecification(DirectoryInfo queriesDirectory)
        {
            this.queriesDirectory = queriesDirectory;
        }

        public SerializerSpecification(FileInfo queryFile)
        {
            var path = queryFile.FullName;
            var name = Path.GetFileNameWithoutExtension(path);
            var openParens = name.IndexOf('(');
            var closeParens = name.IndexOf(')');
            var sql = File.ReadAllText(path);
            if (openParens != -1 != (closeParens != -1)) throw new ArgumentException();
            QueryParameterInfo[] parameters = Array.Empty<QueryParameterInfo>();
            if (openParens != -1)
            {
                var paramString = name.Substring(openParens + 1, closeParens - openParens - 1).Trim();
                if (!string.IsNullOrEmpty(paramString))
                {
                    var paramStrings = paramString.Split(',', StringSplitOptions.TrimEntries);

                    parameters = paramStrings.Select((x, i) =>
                    {
                        var space = x.IndexOf(' ');
                        if (space != -1) return new QueryParameterInfo(
                            x.Substring(space).Trim(),
                            new(null, x.Substring(0, space).Trim()));
                        return new QueryParameterInfo("p" + (i + 1), new(null, x));
                    }).ToArray();
                }
                name = name.Substring(0, openParens).Trim();
            }
            PossiblyUnresolvedType? returnType = null;
            var space = name.IndexOf(' ');
            if (space != -1)
            {
                var returnTypeStr = name.Substring(0, space).Trim();
                name = name.Substring(space).Trim();
                returnType = new PossiblyUnresolvedType(null, returnTypeStr);
            }

            Sql = sql;
            SqlName = name;
            Parameters = parameters;
            Type = returnType;
        }
        public SerializerSpecification(Type typeForSerialization)
        {
            Type = new(typeForSerialization, null);
        }

        public SerializerSpecification(string tableOrView)
        {
            TableOrView = tableOrView;
            SqlName = tableOrView;
        }
        public SerializerSpecification(string sql, string sqlName, QueryParameterInfo[]? parameters = null)
        {
            Sql = sql;
            SqlName = sqlName;
            Parameters = parameters;
        }
        public SerializerSpecification(string tableOrView, Type typeForDeserialization, QueryParameterInfo[]? parameters = null)
        {
            SqlName = tableOrView;
            TableOrView = tableOrView;
            Type = new(typeForDeserialization, null);
            Parameters = parameters;
        }
        public SerializerSpecification(string sql, string sqlName, Type typeForDeserialization, QueryParameterInfo[]? parameters = null)
        {
            Sql = sql;
            SqlName = sqlName;
            Type = new(typeForDeserialization, null);
            Parameters = parameters;
        }

        internal string GetSql() => Sql ?? $"select * from {TableOrView}";

        public override string ToString()
        {
            return SqlName ?? Type.ToString();
        }


    }
}

