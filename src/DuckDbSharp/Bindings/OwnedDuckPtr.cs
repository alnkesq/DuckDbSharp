using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckPtr<T> : IDisposable where T : unmanaged
    {
        public static explicit operator OwnedDuckPtr<T>(T* ptr) => new OwnedDuckPtr<T>(ptr);
        public static implicit operator T*(OwnedDuckPtr<T> ptr) => ptr.Pointer;
        public T* Pointer;
        public OwnedDuckPtr(T* ptr)
        {
            Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            Methods.duckdb_free(Pointer);
            Pointer = null;
        }

    }
}

