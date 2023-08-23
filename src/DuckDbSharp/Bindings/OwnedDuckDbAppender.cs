using System;

namespace DuckDbSharp.Bindings
{
    public unsafe struct OwnedDuckDbPreparedAppender : IDisposable
    {
        public static implicit operator _duckdb_appender*(OwnedDuckDbPreparedAppender ptr) => ptr.Pointer;
        public static explicit operator OwnedDuckDbPreparedAppender(_duckdb_appender* ptr) => new OwnedDuckDbPreparedAppender(ptr);
        public _duckdb_appender* Pointer;

        private OwnedDuckDbPreparedAppender(_duckdb_appender* ptr)
        {
            this.Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            fixed (_duckdb_appender** ptr = &Pointer)
            {
                Methods.duckdb_appender_destroy(ptr);
            }
            BindingUtils.Free(Pointer);
            Pointer = null;
        }
        internal static OwnedDuckDbPreparedAppender Allocate()
        {
            return new(BindingUtils.Alloc<_duckdb_appender>());
        }

        public OwnedDuckDbPreparedAppender Move() => BindingUtils.Move(ref this);
    }
}

