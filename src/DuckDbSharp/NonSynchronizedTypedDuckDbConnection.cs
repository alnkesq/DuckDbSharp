using DuckDbSharp.Bindings;
using System.Collections;
using System.Collections.Generic;

namespace DuckDbSharp
{
    public unsafe class NonSynchronizedTypedDuckDbConnection : TypedDuckDbConnectionBase
    {
        protected NonSynchronizedTypedDuckDbConnection(OwnedDuckDbConnection conn, DuckDbDatabase db)
            : base(conn, db)
        {
        }
        public static NonSynchronizedTypedDuckDbConnection Create(string? path)
        {
            return new NonSynchronizedTypedDuckDbConnection(DuckDbUtils.Connect(path, out var db), db);
        }
        public static NonSynchronizedTypedDuckDbConnection CreateInMemory() => Create(null);


        public override void ExecuteNonQuery(string sql, params object[]? parameters)
        {
            CheckDisposed();
            MaybeLog(sql);
            DuckDbUtils.ExecuteNonQuery(conn, sql, parameters, EnumerableParameterSlots);
        }

        public override long InsertRange<T>(string? destinationSchema, string destinationTableOrView, IEnumerable<T> items)
        {
            CheckDisposed();
            return DuckDbUtils.InsertRange(conn, destinationSchema, destinationTableOrView, items);
        }

        public override IEnumerable<T> Execute<T>(string sql, params object[]? parameters)
        {
            CheckDisposed();
            MaybeLog(sql);
            return DuckDbUtils.Execute<T>(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
        }

        public override IEnumerable Execute(string sql, params object[]? parameters)
        {
            CheckDisposed();
            MaybeLog(sql);
            return DuckDbUtils.Execute(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
        }

        public override T ExecuteScalar<T>(string sql, params object[]? parameters)
        {
            CheckDisposed();
            MaybeLog(sql);
            return DuckDbUtils.ExecuteScalar<T>(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
        }

        public override object ExecuteScalar(string sql, params object[]? parameters)
        {
            CheckDisposed();
            MaybeLog(sql);
            return DuckDbUtils.ExecuteScalar(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
        }
    }
}

