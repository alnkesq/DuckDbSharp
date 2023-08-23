using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbConnection : IDisposable
    {
        public static implicit operator _duckdb_connection*(OwnedDuckDbConnection ptr) => ptr.Pointer;
        public _duckdb_connection* Pointer;
        private Action _onDispose;
        public OwnedDuckDbConnection(_duckdb_connection* ptr, Action? onDispose = null)
        {
            this.Pointer = ptr;
            this._onDispose = onDispose;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_connection** ptr = &Pointer)
            {
                Methods.duckdb_disconnect(ptr);
            }
            Pointer = null;
            _onDispose?.Invoke();
        }

        public OwnedDuckDbConnection Move() => BindingUtils.Move(ref this);
    }
}

