$DuckDbRepo = "C:\Repositories\duckdb"

$opaquePointers = "duckdb_table_function duckdb_scalar_function duckdb_bind_info duckdb_init_info duckdb_function_info duckdb_replacement_scan_info duckdb_task_state"
$opaquePointersArr = $opaquePointers.Split(' ')
("namespace DuckDbSharp.Bindings; ", ($opaquePointersArr | %{ "public unsafe struct $($_)_ptr { public void* ptr; }" })) | out-file DuckDbSharp/Bindings/DuckDb.OpaquePointers.g.cs


ClangSharpPInvokeGenerator -I $DuckDbRepo/src/include  -f $DuckDbRepo/src/include/duckdb.h -n DuckDbSharp.Bindings -o DuckDbSharp/Bindings/DuckDb.g.cs -l duckdb --config preview-codegen  --remap "const char *=byte*" "char *=byte*" ($opaquePointers.Split(' ') | %{ "$_=$($_)_ptr" })

(gc DuckDbSharp\Bindings\DuckDb.g.cs -raw).Replace('public enum DUCKDB_TYPE', "[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]`n    public enum DUCKDB_TYPE") | out-file DuckDbSharp\Bindings\DuckDb.g.cs
