using System;

namespace DuckDbSharp.Bindings
{
    public struct OwnedDuckDbPreparedStatement : IDisposable
    {
        public static implicit operator _duckdb_prepared_statement*(OwnedDuckDbPreparedStatement ptr) => ptr.Pointer;
        public static unsafe explicit operator OwnedDuckDbPreparedStatement(_duckdb_prepared_statement* ptr) => unsafe(new OwnedDuckDbPreparedStatement(ptr));
        public _duckdb_prepared_statement* Pointer;

        private unsafe OwnedDuckDbPreparedStatement(_duckdb_prepared_statement* ptr)
        {
            this.Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            unsafe
            {
                fixed (_duckdb_prepared_statement** ptr = &Pointer)
                {
                    Methods.duckdb_destroy_prepare(ptr);
                }
                BindingUtils.Free(Pointer);
            }
            Pointer = null;
        }
        internal static OwnedDuckDbPreparedStatement Allocate()
        {
            return unsafe(new(BindingUtils.Alloc<_duckdb_prepared_statement>()));
        }

        public OwnedDuckDbPreparedStatement Move() => BindingUtils.Move(ref this);
    }
}

