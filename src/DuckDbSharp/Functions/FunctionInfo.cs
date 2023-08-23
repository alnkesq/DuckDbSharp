using DuckDbSharp.Bindings;
using System;
using System.Reflection;

namespace DuckDbSharp.Functions
{
    public unsafe class FunctionInfo : IDisposable
    {
        public string Name { get; internal set; }
        public MethodInfo Method { get; internal set; }
        public ParameterInfo[] Parameters { get; internal set; }
        public Type? FinalElementType { get; internal set; }
        internal Func<object, object>? Transformer;
        internal duckdb_table_function_ptr Pointer;

        public object? DelegateTarget { get; internal set; }

        public unsafe void Dispose()
        {
            if (Pointer.ptr != null)
            {
                fixed (duckdb_table_function_ptr* ptr = &Pointer)
                {
                    Methods.duckdb_destroy_table_function(ptr);
                }
                Pointer = default;
            }

            this.Method = null!;
            this.DelegateTarget = null;
            this.Transformer = null;
        }
    }


}
