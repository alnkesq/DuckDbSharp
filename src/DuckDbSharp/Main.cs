using DuckDbSharp.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DuckDbSharp
{
    public unsafe class Program
    {
        public static void Main()
        { 
        }

        [UnmanagedCallersOnly(EntryPoint = "duckdb_csharp_bridge_init", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void duckdb_csharp_bridge_init(_duckdb_database db) => DuckDbExtensionUtils.BridgeInit(db, typeof(Program).Assembly);
       
        [UnmanagedCallersOnly(EntryPoint = "duckdb_csharp_bridge_version", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static byte* duckdb_csharp_bridge_version() => DuckDbExtensionUtils.BridgeVersion();


    }
}

