using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbResult : IDisposable
    {
        public static implicit operator duckdb_result*(OwnedDuckDbResult ptr) => ptr.Pointer;
        //public static explicit operator OwnedDuckDbResult(duckdb_result* ptr) => new OwnedDuckDbResult(ptr);
        public duckdb_result* Pointer;
        public nint PointerAsIntPtr => (nint)Pointer;
        internal Action? OnDispose;
        public OwnedDuckDbResult(duckdb_result* ptr, Action? onDispose)
        {
            this.Pointer = ptr;
            this.OnDispose = onDispose;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            Methods.duckdb_destroy_result(Pointer);
            BindingUtils.Free(Pointer);
            OnDispose?.Invoke();
            Pointer = null;
            Disposed?.Invoke();
        }

        internal static OwnedDuckDbResult Allocate(Action? onDispose)
        {
            return new(BindingUtils.Alloc<duckdb_result>(), onDispose);
        }

        public OwnedDuckDbResult Move()
        {
            var copy = this;
            this = default;
            return copy;
        }

        public event Action? Disposed;
    }
}

