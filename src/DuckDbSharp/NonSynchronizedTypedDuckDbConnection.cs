using DuckDbSharp.Bindings;
using System;
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
        public static NonSynchronizedTypedDuckDbConnection Create(DuckDbDatabase db)
        {
            return new NonSynchronizedTypedDuckDbConnection(DuckDbDatabase.Connect(db), db);
        }
        public static NonSynchronizedTypedDuckDbConnection Create(string? path)
        {
            return new NonSynchronizedTypedDuckDbConnection(DuckDbUtils.Connect(path, out var db), db);
        }
        public static NonSynchronizedTypedDuckDbConnection CreateInMemory() => Create((string?)null);

        public override void ExecuteNonQuery(string sql, params object[]? parameters)
        {
            OnBeforeExecute(sql);
            DuckDbUtils.ExecuteNonQuery(conn, sql, parameters, EnumerableParameterSlots);
        }

        public override long InsertRange<T>(string? destinationSchema, string destinationTableOrView, IEnumerable<T> items)
        {
            OnBeforeExecute(null);
            return DuckDbUtils.InsertRange(conn, destinationSchema, destinationTableOrView, items);
        }

        public override IEnumerable<T> ExecuteWithOptions<T>(CommandOptions options, string sql, params object[]? parameters)
        {
            InitOptions(ref options);
            OnBeforeExecute(sql);
            return DuckDbUtils.Execute<T>(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext, options, this);
        }

        public override IEnumerable ExecuteWithOptions(CommandOptions options, string sql, params object[]? parameters)
        {
            InitOptions(ref options);
            OnBeforeExecute(sql);
            return DuckDbUtils.Execute(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext, options, this);
        }

        public override T ExecuteScalar<T>(string sql, params object[]? parameters)
        {
            OnBeforeExecute(sql);
            return DuckDbUtils.ExecuteScalar<T>(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
        }

        public override object ExecuteScalar(string sql, params object[]? parameters)
        {
            OnBeforeExecute(sql);
            return DuckDbUtils.ExecuteScalar(conn, sql, parameters, EnumerableParameterSlots, TypeGenerationContext);
		}
		public override OwnedDuckDbResult ExecuteUnsafeWithOptions(CommandOptions options, string sql, params object?[]? parameters)
		{
            InitOptions(ref options);
            OnBeforeExecute(sql);
			return DuckDbUtils.ExecuteCore(Handle, sql, parameters, EnumerableParameterSlots, options);
		}

        public virtual NonSynchronizedTypedDuckDbConnection Clone => Create(Database);
    }
}

