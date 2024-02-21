using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DuckDbSharp.Bindings
{
    /// <summary>
    ///  Almost the same as DUCKDB_TYPE, except for the numeric values,
    ///  which here are guaranteeded to be stable between versions (unlike in the duckdb codebase)
    /// </summary>
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DUCKDB_TYPE_STABLE
    {
        DUCKDB_TYPE_INVALID = 0,
        DUCKDB_TYPE_BOOLEAN,
        DUCKDB_TYPE_TINYINT,
        DUCKDB_TYPE_SMALLINT,
        DUCKDB_TYPE_INTEGER,
        DUCKDB_TYPE_BIGINT,
        DUCKDB_TYPE_UTINYINT,
        DUCKDB_TYPE_USMALLINT,
        DUCKDB_TYPE_UINTEGER,
        DUCKDB_TYPE_UBIGINT,
        DUCKDB_TYPE_FLOAT,
        DUCKDB_TYPE_DOUBLE,
        DUCKDB_TYPE_TIMESTAMP,
        DUCKDB_TYPE_DATE,
        DUCKDB_TYPE_TIME,
        DUCKDB_TYPE_INTERVAL,
        DUCKDB_TYPE_HUGEINT,
        DUCKDB_TYPE_VARCHAR,
        DUCKDB_TYPE_BLOB,
        DUCKDB_TYPE_DECIMAL,
        DUCKDB_TYPE_TIMESTAMP_S,
        DUCKDB_TYPE_TIMESTAMP_MS,
        DUCKDB_TYPE_TIMESTAMP_NS,
        DUCKDB_TYPE_ENUM,
        DUCKDB_TYPE_LIST,
        DUCKDB_TYPE_STRUCT,
        DUCKDB_TYPE_MAP,
        DUCKDB_TYPE_UUID,
        DUCKDB_TYPE_UNION,
        DUCKDB_TYPE_BIT,
        DUCKDB_TYPE_UHUGEINT,
        DUCKDB_TYPE_TIME_TZ,
        DUCKDB_TYPE_TIMESTAMP_TZ,
    }
}

