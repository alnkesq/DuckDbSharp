using DuckDbSharp.Bindings;
using DuckDbSharp.Tests;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DuckDbSharp
{

    public static unsafe class Program
    {

        [UnmanagedCallersOnly(EntryPoint = "duckdbsharp_init", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void duckdbsharp_init(_duckdb_database db) => DuckDbExtensionUtils.BridgeInit(db, typeof(QueryTests).Assembly);


        [UnmanagedCallersOnly(EntryPoint = "duckdbsharp_version", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static byte* duckdbsharp_version() => DuckDbExtensionUtils.BridgeVersion();




    }



}
