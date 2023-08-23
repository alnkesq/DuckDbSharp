using DuckDbSharp.Bindings;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DuckDbSharp
{
    public unsafe class ThreadSafeTypedDuckDbConnection : TypedDuckDbConnectionBase
    {
        protected ThreadSafeTypedDuckDbConnection(OwnedDuckDbConnection conn, DuckDbDatabase database)
            : base(conn, database)
        {
        }
        public static ThreadSafeTypedDuckDbConnection Create(string? path)
        {
            return new ThreadSafeTypedDuckDbConnection(DuckDbUtils.Connect(path, out var db), db);
        }
        public static ThreadSafeTypedDuckDbConnection CreateInMemory() => Create(null);

        public override void Dispose()
        {
            lock (this)
            {
                base.Dispose();
            }
        }

        public override void InsertRange<T>(string? destinationSchema, string destinationTableOrView, IEnumerable<T> items)
        {
            lock (this)
            {
                CheckDisposed();
                DuckDbUtils.InsertRange(conn, destinationSchema, destinationTableOrView, items);
            }
        }

        public override IEnumerable<T> Execute<T>(string sql, params object[] parameters)
        {
            IEnumerator<T[]> enumerator;
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                enumerator = DuckDbUtils.ExecuteBatched<T>(Pointer, sql, parameters, EnumerableParameterSlots).GetEnumerator();
            }
            while (true)
            {
                T[] current;
                lock (this)
                {
                    CheckDisposed();
                    if (!enumerator.MoveNext()) yield break;
                    current = enumerator.Current;
                }
                foreach (var item in current)
                {
                    yield return item;
                }
            }
        }

        public override IEnumerable Execute(string sql, params object[] parameters)
        {
            IEnumerator enumerator;
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                enumerator = DuckDbUtils.Execute(Pointer, sql, parameters, EnumerableParameterSlots).GetEnumerator();
            }
            while (true)
            {
                // Here it's wasteful to lock and unlock for every item in the chunk, but this is the untyped version so we don't care too much.
                object current;
                lock (this)
                {
                    CheckDisposed();
                    if (!enumerator.MoveNext()) yield break;
                    current = enumerator.Current;
                }
                yield return current;
            }
        }

        public override T ExecuteScalar<T>(string sql, params object[] parameters)
        {
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                return DuckDbUtils.ExecuteScalar<T>(conn, sql, parameters, EnumerableParameterSlots);
            }
        }

        public override object ExecuteScalar(string sql, params object[] parameters)
        {
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                return DuckDbUtils.ExecuteScalar(conn, sql, parameters, EnumerableParameterSlots);
            }
        }
        public override void ExecuteNonQuery(string sql, params object[] parameters)
        {
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                DuckDbUtils.ExecuteNonQuery(conn, sql, parameters, EnumerableParameterSlots);
            }
        }

        public override void CreateEnum(Type t)
        {
            lock (this)
            {
                base.CreateEnum(t);
            }
        }

        public override void CreateTable<T>(string name, bool replaceIfExisting = false)
        {
            lock (this)
            {
                base.CreateTable<T>(name, replaceIfExisting);
            }
        }

        private nint Pointer => (nint)conn.Pointer;
    }
}

