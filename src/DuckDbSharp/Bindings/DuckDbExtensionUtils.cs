using DuckDbSharp.Functions;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Bindings
{
    internal unsafe static class DuckDbExtensionUtils
    {
        private static bool _resolverInitialized;

        private static void InitializeDllResolver()
        {
            if (_resolverInitialized) return;
            _resolverInitialized = true;
            NativeLibrary.SetDllImportResolver(typeof(DuckDbExtensionUtils).Assembly, (library, asm, searchPath) =>
            {
                if (library == "duckdb")
                {
                    var lib = NativeLibrary.Load(GetMandatoryEnvironmentVariable("DUCKDBSHARP_DUCKDB_DLL"));
                    return lib;
                }
                return 0;
            });
        }

        public static string GetMandatoryEnvironmentVariable(string name)
        {
            var val = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(val))
            {
                throw new Exception("Environment variable must be set: " + name);
            }
            return val;
        }

        public static byte* BridgeVersion()
        {
            try
            {
                InitializeDllResolver();
                return Methods.duckdb_library_version();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return null;
            }
        }


        [Obsolete("Empty params argument. If this is intentional, consider passing Array.Empty<Assembly>().")]
        public static void BridgeInit(_duckdb_database db)
        {
            BridgeInit(db, Array.Empty<Assembly>());
        }
        public static void BridgeInit(_duckdb_database db, params Assembly[] registerFunctionsInAssemblies)
        {
            try
            {
                InitializeDllResolver();
                var conn = DuckDbUtils.ConnectCore(&db);
                foreach (var item in registerFunctionsInAssemblies)
                {
                    FunctionUtils.RegisterFunctions(conn, item);
                }

                var dlls = GetMandatoryEnvironmentVariable("DUCKDBSHARP_USER_ASSEMBLY_PATH").Split(new[] { ';', ',' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                /*
                var folders = dlls.Select(x => Path.GetDirectoryName(Path.GetFullPath(x))).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    var name = new AssemblyName(e.Name).Name + ".dll";
                    //Console.WriteLine("Want to load: " + name);
                    foreach (var folder in folders)
                    {
                        var p = Path.Combine(folder, name);
                        //Console.WriteLine("Try: " + p);
                        if (File.Exists(p))
                        {
                            //Console.WriteLine("Found!");
                            return Assembly.LoadFrom(p);
                        }
                    }
                    //Console.WriteLine("NOT FOUND: " + name);
                    return null;
                };
                */
                foreach (var asmpath in dlls)
                {
                    var asm = Assembly.LoadFrom(asmpath);
                    FunctionUtils.RegisterFunctions(conn, asm);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

    }
}

