using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbLogicalType : IDisposable
    {
        public static explicit operator OwnedDuckDbLogicalType(_duckdb_logical_type* ptr) => new OwnedDuckDbLogicalType(ptr);
        public static implicit operator _duckdb_logical_type*(OwnedDuckDbLogicalType ptr) => ptr.Pointer;
        public _duckdb_logical_type* Pointer;
        public OwnedDuckDbLogicalType(_duckdb_logical_type* ptr)
        {
            Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_logical_type** ptr = &Pointer)
            {
                Methods.duckdb_destroy_logical_type(ptr);
            }
            Pointer = null;
        }
        public OwnedDuckDbLogicalType Move() => BindingUtils.Move(ref this);
    }
}

