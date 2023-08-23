using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbDataChunk : IDisposable
    {
        public static explicit operator OwnedDuckDbDataChunk(_duckdb_data_chunk* ptr) => new OwnedDuckDbDataChunk(ptr);
        public static implicit operator _duckdb_data_chunk*(OwnedDuckDbDataChunk ptr) => ptr.Pointer;
        public _duckdb_data_chunk* Pointer;
        public OwnedDuckDbDataChunk(_duckdb_data_chunk* ptr)
        {
            Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_data_chunk** ptr = &Pointer)
            {
                Methods.duckdb_destroy_data_chunk(ptr);
            }
            Pointer = null;
        }
        public OwnedDuckDbDataChunk Move() => BindingUtils.Move(ref this);
    }
}

