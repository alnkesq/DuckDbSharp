using Newtonsoft.Json;
using DuckDbSharp.Bindings;
using DuckDbSharp.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace DuckDbSharp
{

    public static unsafe class Program
    {

        [UnmanagedCallersOnly(EntryPoint = "duckdbsharp_init", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void duckdbsharp_init(_duckdb_database db) => DuckDbExtensionUtils.BridgeInit(db, typeof(Program).Assembly);


        [UnmanagedCallersOnly(EntryPoint = "duckdbsharp_version", CallConvs = new[] { typeof(CallConvCdecl) })]
        public static byte* duckdbsharp_version() => DuckDbExtensionUtils.BridgeVersion();




    }



}
