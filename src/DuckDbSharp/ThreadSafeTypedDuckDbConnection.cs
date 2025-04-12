using DuckDbSharp.Bindings;
using DuckDbSharp.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DuckDbSharp
{
    public unsafe class ThreadSafeTypedDuckDbConnection : TypedDuckDbConnectionBase
    {
        protected ThreadSafeTypedDuckDbConnection(OwnedDuckDbConnection conn, DuckDbDatabase database)
            : base(conn, database)
        {
        }
        public static ThreadSafeTypedDuckDbConnection Create(DuckDbDatabase db)
        {
            return new ThreadSafeTypedDuckDbConnection(DuckDbDatabase.Connect(db.Path, db), db);
        }
        public static ThreadSafeTypedDuckDbConnection Create(string? path)
        {
            return new ThreadSafeTypedDuckDbConnection(DuckDbUtils.Connect(path, out var db), db);
        }
        public static ThreadSafeTypedDuckDbConnection CreateInMemory() => Create((string?)null);

        public override void Dispose()
        {
            lock (this)
            {
                base.Dispose();
            }
        }

        public override long InsertRange<T>(string? destinationSchema, string destinationTableOrView, IEnumerable<T> items)
        {
            lock (this)
            {
                CheckDisposed();
                return DuckDbUtils.InsertRange(conn, destinationSchema, destinationTableOrView, items);
            }
        }


        public override IEnumerable<T> ExecuteWithOptions<T>(CommandOptions options, string sql, params object?[]? parameters)
        {
            InitOptions(ref options);
            IEnumerator<T[]> enumerator;
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                enumerator = DuckDbUtils.ExecuteBatched<T>(Pointer, sql, parameters, EnumerableParameterSlots, TypeGenerationContext, options).GetEnumerator();
            }
            try
            {
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
            finally
            {
                lock (this)
                {
                    enumerator.Dispose();
                }
            }
        }
        public override OwnedDuckDbResult ExecuteUnsafeWithOptions(CommandOptions options, string sql, params object?[]? parameters)
        {
            InitOptions(ref options);
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                return DuckDbUtils.ExecuteCore(Handle, sql, parameters, EnumerableParameterSlots, options);
            }
        }


		public override IEnumerable ExecuteWithOptions(CommandOptions options, string sql, params object?[]? parameters)
        {
            InitOptions(ref options);
            IEnumerator enumerator;
            Type elementType;
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                var enumerable = DuckDbUtils.Execute(Pointer, sql, parameters, EnumerableParameterSlots, TypeGenerationContext, options);
                elementType = TypeSniffedEnumerable.TryGetEnumerableElementType(enumerable.GetType())!;
                enumerator = enumerable.GetEnumerator();
            }
            var threadSafeEnumerable = ProduceEnumerable(enumerator);
            EnumerableCast ??= typeof(Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public);
            return (IEnumerable)EnumerableCast.MakeGenericMethod([elementType]).Invoke(null, [threadSafeEnumerable])!;
        }

        private static MethodInfo? EnumerableCast;
        
        private IEnumerable ProduceEnumerable(IEnumerator enumerator)
        {

            try
            {

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
            finally
            {
                lock (this)
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }
        }

        public override T ExecuteScalar<T>(string sql, params object[]? parameters)
        {
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                return DuckDbUtils.ExecuteScalar<T>(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
            }
        }

        public override object ExecuteScalar(string sql, params object[]? parameters)
        {
            lock (this)
            {
                CheckDisposed();
                MaybeLog(sql);
                return DuckDbUtils.ExecuteScalar(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
            }
        }
        public override void ExecuteNonQuery(string sql, params object[]? parameters)
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

        public override void CreateTable<T>(string name, bool replaceIfExisting = false, string[]? primaryKey = null)
        {
            lock (this)
            {
                base.CreateTable<T>(name, replaceIfExisting, primaryKey);
            }
        }

        private nint Pointer => (nint)conn.Pointer;
    }
}

