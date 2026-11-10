using System;

namespace DuckDbSharp.Bindings
{
    public struct OwnedDuckDbLogicalType : IDisposable
    {
        public static unsafe explicit operator OwnedDuckDbLogicalType(_duckdb_logical_type* ptr) => unsafe(new OwnedDuckDbLogicalType(ptr));
        public static implicit operator _duckdb_logical_type*(OwnedDuckDbLogicalType ptr) => ptr.Pointer;
        public _duckdb_logical_type* Pointer;
        public unsafe OwnedDuckDbLogicalType(_duckdb_logical_type* ptr)
        {
            Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_logical_type** ptr = &Pointer)
            {
                unsafe { Methods.duckdb_destroy_logical_type(ptr); }
            }
            Pointer = null;
        }
        public OwnedDuckDbLogicalType Move() => BindingUtils.Move(ref this);
    }
}

