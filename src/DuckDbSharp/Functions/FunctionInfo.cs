using DuckDbSharp.Bindings;
using DuckDbSharp.Reflection;
using System;
using System.Collections;
using System.Reflection;

namespace DuckDbSharp.Functions
{
    public class FunctionInfo : IDisposable
    {
        public required string Name;
        public required MethodInfo Method;
        public required Type[] Parameters;
        public Type? FinalElementType { get; internal set; }
        internal Func<object, object>? Transformer;
        internal duckdb_table_function_ptr PointerTableFn;
        internal duckdb_scalar_function_ptr PointerScalarFn;


        internal DuckDbStructuralType? ScalarArgumentChunkType;
        public RootDeserializer? ScalarArgumentDeserializer;
        public object? DelegateTarget { get; internal set; }
        internal bool IsBatched;
        internal Type? ScalarReturnType;
        public unsafe void Dispose()
        {
            if (PointerTableFn.ptr != null)
            {
                fixed (duckdb_table_function_ptr* ptr = &PointerTableFn)
                {
                    Methods.duckdb_destroy_table_function(ptr);
                }
                PointerTableFn = default;
            }
            if (PointerScalarFn.ptr != null)
            {
                fixed (duckdb_scalar_function_ptr* ptr = &PointerScalarFn)
                {
                    Methods.duckdb_destroy_scalar_function(ptr);
                }
                PointerScalarFn = default;
            }

            this.Method = null!;
            this.DelegateTarget = null;
            this.Transformer = null;
        }
    }


}
