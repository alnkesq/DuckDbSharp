using System.Runtime.InteropServices;

namespace DuckDbSharp.Bindings
{
    public enum DUCKDB_TYPE
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
        DUCKDB_TYPE_UHUGEINT,
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
        DUCKDB_TYPE_TIME_TZ,
        DUCKDB_TYPE_TIMESTAMP_TZ,
    }

    public enum duckdb_state
    {
        DuckDBSuccess = 0,
        DuckDBError = 1,
    }

    public enum duckdb_pending_state
    {
        DUCKDB_PENDING_RESULT_READY = 0,
        DUCKDB_PENDING_RESULT_NOT_READY = 1,
        DUCKDB_PENDING_ERROR = 2,
        DUCKDB_PENDING_NO_TASKS_AVAILABLE = 3,
    }

    public enum duckdb_result_type
    {
        DUCKDB_RESULT_TYPE_INVALID,
        DUCKDB_RESULT_TYPE_CHANGED_ROWS,
        DUCKDB_RESULT_TYPE_NOTHING,
        DUCKDB_RESULT_TYPE_QUERY_RESULT,
    }

    public enum duckdb_statement_type
    {
        DUCKDB_STATEMENT_TYPE_INVALID,
        DUCKDB_STATEMENT_TYPE_SELECT,
        DUCKDB_STATEMENT_TYPE_INSERT,
        DUCKDB_STATEMENT_TYPE_UPDATE,
        DUCKDB_STATEMENT_TYPE_EXPLAIN,
        DUCKDB_STATEMENT_TYPE_DELETE,
        DUCKDB_STATEMENT_TYPE_PREPARE,
        DUCKDB_STATEMENT_TYPE_CREATE,
        DUCKDB_STATEMENT_TYPE_EXECUTE,
        DUCKDB_STATEMENT_TYPE_ALTER,
        DUCKDB_STATEMENT_TYPE_TRANSACTION,
        DUCKDB_STATEMENT_TYPE_COPY,
        DUCKDB_STATEMENT_TYPE_ANALYZE,
        DUCKDB_STATEMENT_TYPE_VARIABLE_SET,
        DUCKDB_STATEMENT_TYPE_CREATE_FUNC,
        DUCKDB_STATEMENT_TYPE_DROP,
        DUCKDB_STATEMENT_TYPE_EXPORT,
        DUCKDB_STATEMENT_TYPE_PRAGMA,
        DUCKDB_STATEMENT_TYPE_VACUUM,
        DUCKDB_STATEMENT_TYPE_CALL,
        DUCKDB_STATEMENT_TYPE_SET,
        DUCKDB_STATEMENT_TYPE_LOAD,
        DUCKDB_STATEMENT_TYPE_RELATION,
        DUCKDB_STATEMENT_TYPE_EXTENSION,
        DUCKDB_STATEMENT_TYPE_LOGICAL_PLAN,
        DUCKDB_STATEMENT_TYPE_ATTACH,
        DUCKDB_STATEMENT_TYPE_DETACH,
        DUCKDB_STATEMENT_TYPE_MULTI,
    }

    public partial struct duckdb_date
    {
        [NativeTypeName("int32_t")]
        public int days;
    }

    public partial struct duckdb_date_struct
    {
        [NativeTypeName("int32_t")]
        public int year;

        [NativeTypeName("int8_t")]
        public sbyte month;

        [NativeTypeName("int8_t")]
        public sbyte day;
    }

    public partial struct duckdb_time
    {
        [NativeTypeName("int64_t")]
        public long micros;
    }

    public partial struct duckdb_time_struct
    {
        [NativeTypeName("int8_t")]
        public sbyte hour;

        [NativeTypeName("int8_t")]
        public sbyte min;

        [NativeTypeName("int8_t")]
        public sbyte sec;

        [NativeTypeName("int32_t")]
        public int micros;
    }

    public partial struct duckdb_time_tz
    {
        [NativeTypeName("uint64_t")]
        public ulong bits;
    }

    public partial struct duckdb_time_tz_struct
    {
        public duckdb_time time;

        [NativeTypeName("int32_t")]
        public int offset;
    }

    public partial struct duckdb_timestamp
    {
        [NativeTypeName("int64_t")]
        public long micros;
    }

    public partial struct duckdb_timestamp_struct
    {
        public duckdb_date_struct date;

        public duckdb_time_struct time;
    }

    public partial struct duckdb_interval
    {
        [NativeTypeName("int32_t")]
        public int months;

        [NativeTypeName("int32_t")]
        public int days;

        [NativeTypeName("int64_t")]
        public long micros;
    }

    public partial struct duckdb_hugeint
    {
        [NativeTypeName("uint64_t")]
        public ulong lower;

        [NativeTypeName("int64_t")]
        public long upper;
    }

    public partial struct duckdb_uhugeint
    {
        [NativeTypeName("uint64_t")]
        public ulong lower;

        [NativeTypeName("uint64_t")]
        public ulong upper;
    }

    public partial struct duckdb_decimal
    {
        [NativeTypeName("uint8_t")]
        public byte width;

        [NativeTypeName("uint8_t")]
        public byte scale;

        public duckdb_hugeint value;
    }

    public partial struct duckdb_query_progress_type
    {
        public double percentage;

        [NativeTypeName("uint64_t")]
        public ulong rows_processed;

        [NativeTypeName("uint64_t")]
        public ulong total_rows_to_process;
    }

    public partial struct duckdb_string_t
    {
        [NativeTypeName("union (anonymous union at C:/Repositories/duckdb/src/include/duckdb.h:280:2)")]
        public _value_e__Union value;

        [StructLayout(LayoutKind.Explicit)]
        public partial struct _value_e__Union
        {
            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct at C:/Repositories/duckdb/src/include/duckdb.h:281:3)")]
            public _pointer_e__Struct pointer;

            [FieldOffset(0)]
            [NativeTypeName("struct (anonymous struct at C:/Repositories/duckdb/src/include/duckdb.h:286:3)")]
            public _inlined_e__Struct inlined;

            public unsafe partial struct _pointer_e__Struct
            {
                [NativeTypeName("uint32_t")]
                public uint length;

                [NativeTypeName("char[4]")]
                public fixed sbyte prefix[4];

                [NativeTypeName("char *")]
                public byte* ptr;
            }

            public unsafe partial struct _inlined_e__Struct
            {
                [NativeTypeName("uint32_t")]
                public uint length;

                [NativeTypeName("char[12]")]
                public fixed sbyte inlined[12];
            }
        }
    }

    public partial struct duckdb_list_entry
    {
        [NativeTypeName("uint64_t")]
        public ulong offset;

        [NativeTypeName("uint64_t")]
        public ulong length;
    }

    public unsafe partial struct duckdb_column
    {
        public void* __deprecated_data;

        public bool* __deprecated_nullmask;

        public DUCKDB_TYPE __deprecated_type;

        [NativeTypeName("char *")]
        public byte* __deprecated_name;

        public void* internal_data;
    }

    public unsafe partial struct _duckdb_vector
    {
        public void* __vctr;
    }

    public unsafe partial struct duckdb_string
    {
        [NativeTypeName("char *")]
        public byte* data;

        [NativeTypeName("idx_t")]
        public ulong size;
    }

    public unsafe partial struct duckdb_blob
    {
        public void* data;

        [NativeTypeName("idx_t")]
        public ulong size;
    }

    public unsafe partial struct duckdb_result
    {
        [NativeTypeName("idx_t")]
        public ulong __deprecated_column_count;

        [NativeTypeName("idx_t")]
        public ulong __deprecated_row_count;

        [NativeTypeName("idx_t")]
        public ulong __deprecated_rows_changed;

        public duckdb_column* __deprecated_columns;

        [NativeTypeName("char *")]
        public byte* __deprecated_error_message;

        public void* internal_data;
    }

    public unsafe partial struct _duckdb_database
    {
        public void* __db;
    }

    public unsafe partial struct _duckdb_connection
    {
        public void* __conn;
    }

    public unsafe partial struct _duckdb_prepared_statement
    {
        public void* __prep;
    }

    public unsafe partial struct _duckdb_extracted_statements
    {
        public void* __extrac;
    }

    public unsafe partial struct _duckdb_pending_result
    {
        public void* __pend;
    }

    public unsafe partial struct _duckdb_appender
    {
        public void* __appn;
    }

    public unsafe partial struct _duckdb_config
    {
        public void* __cnfg;
    }

    public unsafe partial struct _duckdb_logical_type
    {
        public void* __lglt;
    }

    public unsafe partial struct _duckdb_data_chunk
    {
        public void* __dtck;
    }

    public unsafe partial struct _duckdb_value
    {
        public void* __val;
    }

    public unsafe partial struct _duckdb_arrow
    {
        public void* __arrw;
    }

    public unsafe partial struct _duckdb_arrow_stream
    {
        public void* __arrwstr;
    }

    public unsafe partial struct _duckdb_arrow_schema
    {
        public void* __arrs;
    }

    public unsafe partial struct _duckdb_arrow_array
    {
        public void* __arra;
    }

    public static unsafe partial class Methods
    {
        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_open([NativeTypeName("const char *")] byte* path, [NativeTypeName("duckdb_database *")] _duckdb_database** out_database);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_open_ext([NativeTypeName("const char *")] byte* path, [NativeTypeName("duckdb_database *")] _duckdb_database** out_database, [NativeTypeName("duckdb_config")] _duckdb_config* config, [NativeTypeName("char **")] byte** out_error);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_close([NativeTypeName("duckdb_database *")] _duckdb_database** database);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_connect([NativeTypeName("duckdb_database")] _duckdb_database* database, [NativeTypeName("duckdb_connection *")] _duckdb_connection** out_connection);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_interrupt([NativeTypeName("duckdb_connection")] _duckdb_connection* connection);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_query_progress_type duckdb_query_progress([NativeTypeName("duckdb_connection")] _duckdb_connection* connection);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_disconnect([NativeTypeName("duckdb_connection *")] _duckdb_connection** connection);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_library_version();

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_create_config([NativeTypeName("duckdb_config *")] _duckdb_config** out_config);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("size_t")]
        public static extern nuint duckdb_config_count();

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_get_config_flag([NativeTypeName("size_t")] nuint index, [NativeTypeName("const char **")] byte** out_name, [NativeTypeName("const char **")] byte** out_description);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_set_config([NativeTypeName("duckdb_config")] _duckdb_config* config, [NativeTypeName("const char *")] byte* name, [NativeTypeName("const char *")] byte* option);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_config([NativeTypeName("duckdb_config *")] _duckdb_config** config);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_query([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* query, duckdb_result* out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_result(duckdb_result* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_column_name(duckdb_result* result, [NativeTypeName("idx_t")] ulong col);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern DUCKDB_TYPE duckdb_column_type(duckdb_result* result, [NativeTypeName("idx_t")] ulong col);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_statement_type duckdb_result_statement_type(duckdb_result result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_column_logical_type(duckdb_result* result, [NativeTypeName("idx_t")] ulong col);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_column_count(duckdb_result* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_row_count(duckdb_result* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_rows_changed(duckdb_result* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_column_data(duckdb_result* result, [NativeTypeName("idx_t")] ulong col);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern bool* duckdb_nullmask_data(duckdb_result* result, [NativeTypeName("idx_t")] ulong col);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_result_error(duckdb_result* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_data_chunk")]
        public static extern _duckdb_data_chunk* duckdb_result_get_chunk(duckdb_result result, [NativeTypeName("idx_t")] ulong chunk_index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_result_is_streaming(duckdb_result result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_result_chunk_count(duckdb_result result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_result_type duckdb_result_return_type(duckdb_result result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_value_boolean(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int8_t")]
        public static extern sbyte duckdb_value_int8(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int16_t")]
        public static extern short duckdb_value_int16(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int32_t")]
        public static extern int duckdb_value_int32(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long duckdb_value_int64(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_hugeint duckdb_value_hugeint(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_uhugeint duckdb_value_uhugeint(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_decimal duckdb_value_decimal(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint8_t")]
        public static extern byte duckdb_value_uint8(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint16_t")]
        public static extern ushort duckdb_value_uint16(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint duckdb_value_uint32(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint64_t")]
        public static extern ulong duckdb_value_uint64(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern float duckdb_value_float(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double duckdb_value_double(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_date duckdb_value_date(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_time duckdb_value_time(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_timestamp duckdb_value_timestamp(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_interval duckdb_value_interval(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_value_varchar(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_string duckdb_value_string(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_value_varchar_internal(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_string duckdb_value_string_internal(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_blob duckdb_value_blob(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_value_is_null(duckdb_result* result, [NativeTypeName("idx_t")] ulong col, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_malloc([NativeTypeName("size_t")] nuint size);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_free(void* ptr);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_vector_size();

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_string_is_inlined(duckdb_string_t @string);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_date_struct duckdb_from_date(duckdb_date date);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_date duckdb_to_date(duckdb_date_struct date);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_is_finite_date(duckdb_date date);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_time_struct duckdb_from_time(duckdb_time time);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_time_tz duckdb_create_time_tz([NativeTypeName("int64_t")] long micros, [NativeTypeName("int32_t")] int offset);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_time_tz_struct duckdb_from_time_tz(duckdb_time_tz micros);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_time duckdb_to_time(duckdb_time_struct time);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_timestamp_struct duckdb_from_timestamp(duckdb_timestamp ts);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_timestamp duckdb_to_timestamp(duckdb_timestamp_struct ts);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_is_finite_timestamp(duckdb_timestamp ts);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double duckdb_hugeint_to_double(duckdb_hugeint val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_hugeint duckdb_double_to_hugeint(double val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double duckdb_uhugeint_to_double(duckdb_uhugeint val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_uhugeint duckdb_double_to_uhugeint(double val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_decimal duckdb_double_to_decimal(double val, [NativeTypeName("uint8_t")] byte width, [NativeTypeName("uint8_t")] byte scale);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern double duckdb_decimal_to_double(duckdb_decimal val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_prepare([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* query, [NativeTypeName("duckdb_prepared_statement *")] _duckdb_prepared_statement** out_prepared_statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_prepare([NativeTypeName("duckdb_prepared_statement *")] _duckdb_prepared_statement** prepared_statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_prepare_error([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_nparams([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_parameter_name([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern DUCKDB_TYPE duckdb_param_type([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_clear_bindings([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_statement_type duckdb_prepared_statement_type([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_value([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("duckdb_value")] _duckdb_value* val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_parameter_index([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t *")] ulong* param_idx_out, [NativeTypeName("const char *")] byte* name);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_boolean([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("bool")] byte val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_int8([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("int8_t")] sbyte val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_int16([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("int16_t")] short val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_int32([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("int32_t")] int val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_int64([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("int64_t")] long val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_hugeint([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_hugeint val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_uhugeint([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_uhugeint val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_decimal([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_decimal val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_uint8([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("uint8_t")] byte val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_uint16([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("uint16_t")] ushort val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_uint32([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("uint32_t")] uint val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_uint64([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("uint64_t")] ulong val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_float([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, float val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_double([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, double val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_date([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_date val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_time([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_time val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_timestamp([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_timestamp val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_interval([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, duckdb_interval val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_varchar([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("const char *")] byte* val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_varchar_length([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("const char *")] byte* val, [NativeTypeName("idx_t")] ulong length);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_blob([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx, [NativeTypeName("const void *")] void* data, [NativeTypeName("idx_t")] ulong length);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_bind_null([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("idx_t")] ulong param_idx);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_execute_prepared([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, duckdb_result* out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_execute_prepared_streaming([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, duckdb_result* out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_extract_statements([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* query, [NativeTypeName("duckdb_extracted_statements *")] _duckdb_extracted_statements** out_extracted_statements);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_prepare_extracted_statement([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("duckdb_extracted_statements")] _duckdb_extracted_statements* extracted_statements, [NativeTypeName("idx_t")] ulong index, [NativeTypeName("duckdb_prepared_statement *")] _duckdb_prepared_statement** out_prepared_statement);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_extract_statements_error([NativeTypeName("duckdb_extracted_statements")] _duckdb_extracted_statements* extracted_statements);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_extracted([NativeTypeName("duckdb_extracted_statements *")] _duckdb_extracted_statements** extracted_statements);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_pending_prepared([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("duckdb_pending_result *")] _duckdb_pending_result** out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_pending_prepared_streaming([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("duckdb_pending_result *")] _duckdb_pending_result** out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_pending([NativeTypeName("duckdb_pending_result *")] _duckdb_pending_result** pending_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_pending_error([NativeTypeName("duckdb_pending_result")] _duckdb_pending_result* pending_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_pending_state duckdb_pending_execute_task([NativeTypeName("duckdb_pending_result")] _duckdb_pending_result* pending_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_pending_state duckdb_pending_execute_check_state([NativeTypeName("duckdb_pending_result")] _duckdb_pending_result* pending_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_execute_pending([NativeTypeName("duckdb_pending_result")] _duckdb_pending_result* pending_result, duckdb_result* out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_pending_execution_is_finished(duckdb_pending_state pending_state);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_value([NativeTypeName("duckdb_value *")] _duckdb_value** value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_create_varchar([NativeTypeName("const char *")] byte* text);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_create_varchar_length([NativeTypeName("const char *")] byte* text, [NativeTypeName("idx_t")] ulong length);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_create_int64([NativeTypeName("int64_t")] long val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_create_struct_value([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("duckdb_value *")] _duckdb_value** values);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_create_list_value([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("duckdb_value *")] _duckdb_value** values, [NativeTypeName("idx_t")] ulong value_count);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_get_varchar([NativeTypeName("duckdb_value")] _duckdb_value* value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("int64_t")]
        public static extern long duckdb_get_int64([NativeTypeName("duckdb_value")] _duckdb_value* value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_logical_type(DUCKDB_TYPE type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_logical_type_get_alias([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_list_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_map_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* key_type, [NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* value_type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_union_type([NativeTypeName("duckdb_logical_type *")] _duckdb_logical_type** member_types, [NativeTypeName("const char **")] byte** member_names, [NativeTypeName("idx_t")] ulong member_count);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_struct_type([NativeTypeName("duckdb_logical_type *")] _duckdb_logical_type** member_types, [NativeTypeName("const char **")] byte** member_names, [NativeTypeName("idx_t")] ulong member_count);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_enum_type([NativeTypeName("const char **")] byte** member_names, [NativeTypeName("idx_t")] ulong member_count);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_create_decimal_type([NativeTypeName("uint8_t")] byte width, [NativeTypeName("uint8_t")] byte scale);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern DUCKDB_TYPE duckdb_get_type_id([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint8_t")]
        public static extern byte duckdb_decimal_width([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint8_t")]
        public static extern byte duckdb_decimal_scale([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern DUCKDB_TYPE duckdb_decimal_internal_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern DUCKDB_TYPE duckdb_enum_internal_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint32_t")]
        public static extern uint duckdb_enum_dictionary_size([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_enum_dictionary_value([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_list_type_child_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_map_type_key_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_map_type_value_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_struct_type_child_count([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_struct_type_child_name([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_struct_type_child_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_union_type_member_count([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("char *")]
        public static extern byte* duckdb_union_type_member_name([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_union_type_member_type([NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_logical_type([NativeTypeName("duckdb_logical_type *")] _duckdb_logical_type** type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_data_chunk")]
        public static extern _duckdb_data_chunk* duckdb_create_data_chunk([NativeTypeName("duckdb_logical_type *")] _duckdb_logical_type** types, [NativeTypeName("idx_t")] ulong column_count);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_data_chunk([NativeTypeName("duckdb_data_chunk *")] _duckdb_data_chunk** chunk);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_data_chunk_reset([NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_data_chunk_get_column_count([NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_vector")]
        public static extern _duckdb_vector* duckdb_data_chunk_get_vector([NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk, [NativeTypeName("idx_t")] ulong col_idx);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_data_chunk_get_size([NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_data_chunk_set_size([NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk, [NativeTypeName("idx_t")] ulong size);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_vector_get_column_type([NativeTypeName("duckdb_vector")] _duckdb_vector* vector);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_vector_get_data([NativeTypeName("duckdb_vector")] _duckdb_vector* vector);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("uint64_t *")]
        public static extern ulong* duckdb_vector_get_validity([NativeTypeName("duckdb_vector")] _duckdb_vector* vector);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_vector_ensure_validity_writable([NativeTypeName("duckdb_vector")] _duckdb_vector* vector);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_vector_assign_string_element([NativeTypeName("duckdb_vector")] _duckdb_vector* vector, [NativeTypeName("idx_t")] ulong index, [NativeTypeName("const char *")] byte* str);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_vector_assign_string_element_len([NativeTypeName("duckdb_vector")] _duckdb_vector* vector, [NativeTypeName("idx_t")] ulong index, [NativeTypeName("const char *")] byte* str, [NativeTypeName("idx_t")] ulong str_len);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_vector")]
        public static extern _duckdb_vector* duckdb_list_vector_get_child([NativeTypeName("duckdb_vector")] _duckdb_vector* vector);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_list_vector_get_size([NativeTypeName("duckdb_vector")] _duckdb_vector* vector);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_list_vector_set_size([NativeTypeName("duckdb_vector")] _duckdb_vector* vector, [NativeTypeName("idx_t")] ulong size);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_list_vector_reserve([NativeTypeName("duckdb_vector")] _duckdb_vector* vector, [NativeTypeName("idx_t")] ulong required_capacity);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_vector")]
        public static extern _duckdb_vector* duckdb_struct_vector_get_child([NativeTypeName("duckdb_vector")] _duckdb_vector* vector, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_validity_row_is_valid([NativeTypeName("uint64_t *")] ulong* validity, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_validity_set_row_validity([NativeTypeName("uint64_t *")] ulong* validity, [NativeTypeName("idx_t")] ulong row, [NativeTypeName("bool")] byte valid);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_validity_set_row_invalid([NativeTypeName("uint64_t *")] ulong* validity, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_validity_set_row_valid([NativeTypeName("uint64_t *")] ulong* validity, [NativeTypeName("idx_t")] ulong row);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_table_function")]
        public static extern duckdb_table_function_ptr duckdb_create_table_function();

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_table_function([NativeTypeName("duckdb_table_function *")] duckdb_table_function_ptr* table_function);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_set_name([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("const char *")] byte* name);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_add_parameter([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_add_named_parameter([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("const char *")] byte* name, [NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_set_extra_info([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, void* extra_info, [NativeTypeName("duckdb_delete_callback_t")] delegate* unmanaged[Cdecl]<void*, void> destroy);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_set_bind([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("duckdb_table_function_bind_t")] delegate* unmanaged[Cdecl]<duckdb_bind_info_ptr, void> bind);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_set_init([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("duckdb_table_function_init_t")] delegate* unmanaged[Cdecl]<duckdb_init_info_ptr, void> init);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_set_local_init([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("duckdb_table_function_init_t")] delegate* unmanaged[Cdecl]<duckdb_init_info_ptr, void> init);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_set_function([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("duckdb_table_function_t")] delegate* unmanaged[Cdecl]<duckdb_function_info_ptr, _duckdb_data_chunk*, void> function);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_table_function_supports_projection_pushdown([NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr table_function, [NativeTypeName("bool")] byte pushdown);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_register_table_function([NativeTypeName("duckdb_connection")] _duckdb_connection* con, [NativeTypeName("duckdb_table_function")] duckdb_table_function_ptr function);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_bind_get_extra_info([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_bind_add_result_column([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info, [NativeTypeName("const char *")] byte* name, [NativeTypeName("duckdb_logical_type")] _duckdb_logical_type* type);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_bind_get_parameter_count([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_bind_get_parameter([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info, [NativeTypeName("idx_t")] ulong index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_value")]
        public static extern _duckdb_value* duckdb_bind_get_named_parameter([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info, [NativeTypeName("const char *")] byte* name);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_bind_set_bind_data([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info, void* bind_data, [NativeTypeName("duckdb_delete_callback_t")] delegate* unmanaged[Cdecl]<void*, void> destroy);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_bind_set_cardinality([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info, [NativeTypeName("idx_t")] ulong cardinality, [NativeTypeName("bool")] byte is_exact);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_bind_set_error([NativeTypeName("duckdb_bind_info")] duckdb_bind_info_ptr info, [NativeTypeName("const char *")] byte* error);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_init_get_extra_info([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_init_get_bind_data([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_init_set_init_data([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info, void* init_data, [NativeTypeName("duckdb_delete_callback_t")] delegate* unmanaged[Cdecl]<void*, void> destroy);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_init_get_column_count([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_init_get_column_index([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info, [NativeTypeName("idx_t")] ulong column_index);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_init_set_max_threads([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info, [NativeTypeName("idx_t")] ulong max_threads);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_init_set_error([NativeTypeName("duckdb_init_info")] duckdb_init_info_ptr info, [NativeTypeName("const char *")] byte* error);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_function_get_extra_info([NativeTypeName("duckdb_function_info")] duckdb_function_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_function_get_bind_data([NativeTypeName("duckdb_function_info")] duckdb_function_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_function_get_init_data([NativeTypeName("duckdb_function_info")] duckdb_function_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void* duckdb_function_get_local_init_data([NativeTypeName("duckdb_function_info")] duckdb_function_info_ptr info);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_function_set_error([NativeTypeName("duckdb_function_info")] duckdb_function_info_ptr info, [NativeTypeName("const char *")] byte* error);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_add_replacement_scan([NativeTypeName("duckdb_database")] _duckdb_database* db, [NativeTypeName("duckdb_replacement_callback_t")] delegate* unmanaged[Cdecl]<duckdb_replacement_scan_info_ptr, byte*, void*, void> replacement, void* extra_data, [NativeTypeName("duckdb_delete_callback_t")] delegate* unmanaged[Cdecl]<void*, void> delete_callback);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_replacement_scan_set_function_name([NativeTypeName("duckdb_replacement_scan_info")] duckdb_replacement_scan_info_ptr info, [NativeTypeName("const char *")] byte* function_name);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_replacement_scan_add_parameter([NativeTypeName("duckdb_replacement_scan_info")] duckdb_replacement_scan_info_ptr info, [NativeTypeName("duckdb_value")] _duckdb_value* parameter);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_replacement_scan_set_error([NativeTypeName("duckdb_replacement_scan_info")] duckdb_replacement_scan_info_ptr info, [NativeTypeName("const char *")] byte* error);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_appender_create([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* schema, [NativeTypeName("const char *")] byte* table, [NativeTypeName("duckdb_appender *")] _duckdb_appender** out_appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_appender_column_count([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_logical_type")]
        public static extern _duckdb_logical_type* duckdb_appender_column_type([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("idx_t")] ulong col_idx);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_appender_error([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_appender_flush([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_appender_close([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_appender_destroy([NativeTypeName("duckdb_appender *")] _duckdb_appender** appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_appender_begin_row([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_appender_end_row([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_bool([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("bool")] byte value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_int8([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("int8_t")] sbyte value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_int16([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("int16_t")] short value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_int32([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("int32_t")] int value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_int64([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("int64_t")] long value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_hugeint([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, duckdb_hugeint value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_uint8([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("uint8_t")] byte value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_uint16([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("uint16_t")] ushort value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_uint32([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("uint32_t")] uint value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_uint64([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("uint64_t")] ulong value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_uhugeint([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, duckdb_uhugeint value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_float([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, float value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_double([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, double value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_date([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, duckdb_date value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_time([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, duckdb_time value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_timestamp([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, duckdb_timestamp value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_interval([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, duckdb_interval value);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_varchar([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("const char *")] byte* val);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_varchar_length([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("const char *")] byte* val, [NativeTypeName("idx_t")] ulong length);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_blob([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("const void *")] void* data, [NativeTypeName("idx_t")] ulong length);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_null([NativeTypeName("duckdb_appender")] _duckdb_appender* appender);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_append_data_chunk([NativeTypeName("duckdb_appender")] _duckdb_appender* appender, [NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_query_arrow([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* query, [NativeTypeName("duckdb_arrow *")] _duckdb_arrow** out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_query_arrow_schema([NativeTypeName("duckdb_arrow")] _duckdb_arrow* result, [NativeTypeName("duckdb_arrow_schema *")] _duckdb_arrow_schema** out_schema);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_prepared_arrow_schema([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared, [NativeTypeName("duckdb_arrow_schema *")] _duckdb_arrow_schema** out_schema);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_result_arrow_array(duckdb_result result, [NativeTypeName("duckdb_data_chunk")] _duckdb_data_chunk* chunk, [NativeTypeName("duckdb_arrow_array *")] _duckdb_arrow_array** out_array);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_query_arrow_array([NativeTypeName("duckdb_arrow")] _duckdb_arrow* result, [NativeTypeName("duckdb_arrow_array *")] _duckdb_arrow_array** out_array);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_arrow_column_count([NativeTypeName("duckdb_arrow")] _duckdb_arrow* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_arrow_row_count([NativeTypeName("duckdb_arrow")] _duckdb_arrow* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_arrow_rows_changed([NativeTypeName("duckdb_arrow")] _duckdb_arrow* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("const char *")]
        public static extern byte* duckdb_query_arrow_error([NativeTypeName("duckdb_arrow")] _duckdb_arrow* result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_arrow([NativeTypeName("duckdb_arrow *")] _duckdb_arrow** result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_arrow_stream([NativeTypeName("duckdb_arrow_stream *")] _duckdb_arrow_stream** stream_p);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_execute_prepared_arrow([NativeTypeName("duckdb_prepared_statement")] _duckdb_prepared_statement* prepared_statement, [NativeTypeName("duckdb_arrow *")] _duckdb_arrow** out_result);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_arrow_scan([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* table_name, [NativeTypeName("duckdb_arrow_stream")] _duckdb_arrow_stream* arrow);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern duckdb_state duckdb_arrow_array_scan([NativeTypeName("duckdb_connection")] _duckdb_connection* connection, [NativeTypeName("const char *")] byte* table_name, [NativeTypeName("duckdb_arrow_schema")] _duckdb_arrow_schema* arrow_schema, [NativeTypeName("duckdb_arrow_array")] _duckdb_arrow_array* arrow_array, [NativeTypeName("duckdb_arrow_stream *")] _duckdb_arrow_stream** out_stream);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_execute_tasks([NativeTypeName("duckdb_database")] _duckdb_database* database, [NativeTypeName("idx_t")] ulong max_tasks);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_task_state")]
        public static extern duckdb_task_state_ptr duckdb_create_task_state([NativeTypeName("duckdb_database")] _duckdb_database* database);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_execute_tasks_state([NativeTypeName("duckdb_task_state")] duckdb_task_state_ptr state);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("idx_t")]
        public static extern ulong duckdb_execute_n_tasks_state([NativeTypeName("duckdb_task_state")] duckdb_task_state_ptr state, [NativeTypeName("idx_t")] ulong max_tasks);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_finish_execution([NativeTypeName("duckdb_task_state")] duckdb_task_state_ptr state);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_task_state_is_finished([NativeTypeName("duckdb_task_state")] duckdb_task_state_ptr state);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void duckdb_destroy_task_state([NativeTypeName("duckdb_task_state")] duckdb_task_state_ptr state);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("bool")]
        public static extern byte duckdb_execution_is_finished([NativeTypeName("duckdb_connection")] _duckdb_connection* con);

        [DllImport("duckdb", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: NativeTypeName("duckdb_data_chunk")]
        public static extern _duckdb_data_chunk* duckdb_stream_fetch_chunk(duckdb_result result);
    }
}
