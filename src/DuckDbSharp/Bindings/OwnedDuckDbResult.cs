using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbResult : IDisposable
    {
        public static implicit operator duckdb_result*(OwnedDuckDbResult ptr) => ptr.Pointer;
        //public static explicit operator OwnedDuckDbResult(duckdb_result* ptr) => new OwnedDuckDbResult(ptr);
        public duckdb_result* Pointer;
        public nint PointerAsIntPtr => (nint)Pointer;
        public event Action? Disposed;
        public OwnedDuckDbResult(duckdb_result* ptr)
        {
            this.Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            Methods.duckdb_destroy_result(Pointer);
            BindingUtils.Free(Pointer);
            Pointer = null;
            Disposed?.Invoke();
        }

        internal static OwnedDuckDbResult Allocate(Action? onDispose)
        {
            var result = new OwnedDuckDbResult(BindingUtils.Alloc<duckdb_result>());
            result.Disposed += onDispose;
            return result;
        }

        public OwnedDuckDbResult Move()
        {
            var copy = this;
            this = default;
            return copy;
        }

    }
}

