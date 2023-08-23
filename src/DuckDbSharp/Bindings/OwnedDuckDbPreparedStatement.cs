using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbPreparedStatement : IDisposable
    {
        public static implicit operator _duckdb_prepared_statement*(OwnedDuckDbPreparedStatement ptr) => ptr.Pointer;
        public static explicit operator OwnedDuckDbPreparedStatement(_duckdb_prepared_statement* ptr) => new OwnedDuckDbPreparedStatement(ptr);
        public _duckdb_prepared_statement* Pointer;

        private OwnedDuckDbPreparedStatement(_duckdb_prepared_statement* ptr)
        {
            this.Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_prepared_statement** ptr = &Pointer)
            {
                Methods.duckdb_destroy_prepare(ptr);
            }
            BindingUtils.Free(Pointer);
            Pointer = null;
        }
        internal static OwnedDuckDbPreparedStatement Allocate()
        {
            return new(BindingUtils.Alloc<_duckdb_prepared_statement>());
        }

        public OwnedDuckDbPreparedStatement Move() => BindingUtils.Move(ref this);
    }
}

