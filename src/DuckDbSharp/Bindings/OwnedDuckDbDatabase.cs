using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbDatabase : IDisposable
    {
        public static explicit operator OwnedDuckDbDatabase(_duckdb_database* ptr) => new OwnedDuckDbDatabase(ptr);
        public static implicit operator _duckdb_database*(OwnedDuckDbDatabase ptr) => ptr.Pointer;
        public _duckdb_database* Pointer;

        public OwnedDuckDbDatabase(_duckdb_database* ptr)
        {
            this.Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_database** ptr = &Pointer)
            {
                Methods.duckdb_close(ptr);
            }
            Pointer = null;
        }
        public OwnedDuckDbDatabase Move() => BindingUtils.Move(ref this);
    }
}

