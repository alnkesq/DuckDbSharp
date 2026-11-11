using System;

namespace DuckDbSharp.Bindings
{
    public struct OwnedDuckDbPreparedAppender : IDisposable
    {
        public static implicit operator _duckdb_appender*(OwnedDuckDbPreparedAppender ptr) => ptr.Pointer;
        public static unsafe explicit operator OwnedDuckDbPreparedAppender(_duckdb_appender* ptr) => unsafe(new OwnedDuckDbPreparedAppender(ptr));
        public _duckdb_appender* Pointer;

        private unsafe OwnedDuckDbPreparedAppender(_duckdb_appender* ptr)
        {
            this.Pointer = ptr;
        }

        public void Dispose()
        {
            if (Pointer == null) return;
            unsafe
            {
                fixed (_duckdb_appender** ptr = &Pointer)
                {
                    Methods.duckdb_appender_destroy(ptr);
                }
                BindingUtils.Free(Pointer);
            }
            Pointer = null;
        }
        internal static OwnedDuckDbPreparedAppender Allocate()
        {
            return unsafe(new(BindingUtils.Alloc<_duckdb_appender>()));
        }

        public OwnedDuckDbPreparedAppender Move() => BindingUtils.Move(ref this);
    }
}

