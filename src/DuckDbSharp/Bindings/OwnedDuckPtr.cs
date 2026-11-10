using System;

namespace DuckDbSharp.Bindings
{
    public struct OwnedDuckPtr<T> : IDisposable where T : unmanaged
    {
        public static unsafe explicit operator OwnedDuckPtr<T>(T* ptr) => unsafe(new OwnedDuckPtr<T>(ptr));
        public static implicit operator T*(OwnedDuckPtr<T> ptr) => ptr.Pointer;
        public T* Pointer;
        public unsafe OwnedDuckPtr(T* ptr)
        {
            Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            unsafe { Methods.duckdb_free(Pointer); }
            Pointer = null;
        }

    }
}

