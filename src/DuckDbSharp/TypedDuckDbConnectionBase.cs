using DuckDbSharp.Bindings;
using DuckDbSharp.Functions;
using DuckDbSharp.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DuckDbSharp
{
    public abstract class TypedDuckDbConnectionBase : IDisposable
    {
        protected OwnedDuckDbConnection conn;

        internal protected List<EnumerableParameterSlot> EnumerableParameterSlots => database.EnumerableParameterSlots;

        protected DuckDbDatabase database;
        public OwnedDuckDbConnection Handle => conn;

        protected TypedDuckDbConnectionBase(OwnedDuckDbConnection conn, DuckDbDatabase database)
        {
            this.conn = conn;
            this.database = database;
        }
		public TypeGenerationContext TypeGenerationContext { get; set; } = TypeGenerationContext.Global;
        
		public DuckDbDatabase Database => database;
        public virtual void Dispose()
        {
            conn.Dispose();
        }

        public void Insert<T>(string destinationTableOrView, T item)
        {
            Insert(null, destinationTableOrView, item);
        }

        public void Insert<T>(string? destinationSchema, string destinationTableOrView, T item)
        {
            InsertRange(destinationSchema, destinationTableOrView, new[] { item });
        }
        public long InsertRange<T>(string destinationTableOrView, IEnumerable<T> items)
        {
            return InsertRange(null, destinationTableOrView, items);
        }

        public long DeleteRange<TKey>(string destinationTable, string keyFieldName, IEnumerable<TKey> keys)
        {
            return ExecuteScalar<long>($"delete from {destinationTable} where {keyFieldName} in (select k.Value from table_parameter_1() k)", new object[] { keys });
        }

        public IEnumerable<T> BatchLookup<T, TKey>(string selectAndFromOrTableName, string keyFieldName, IEnumerable<TKey> keys, string? additionalFilter = null, params object[]? parameters)
        {
            if (!selectAndFromOrTableName.Contains(' ')) selectAndFromOrTableName = "from " + selectAndFromOrTableName;
            return Execute<T>($"{selectAndFromOrTableName} where {(additionalFilter != null ? ("(" + additionalFilter + ") and ") : null)} {keyFieldName} in (select k.Value from table_parameter_1() k)", [keys, ..parameters]);
        }

        public abstract long InsertRange<T>(string? destinationSchema, string destinationTableOrView, IEnumerable<T> items);
        public abstract IEnumerable<T> Execute<T>(string sql, params object[]? parameters);
        public abstract IEnumerable Execute(string sql, params object[]? parameters);

        public abstract T ExecuteScalar<T>(string sql, params object[]? parameters);
        public abstract object ExecuteScalar(string sql, params object[]? parameters);
        public abstract void ExecuteNonQuery(string sql, params object[]? parameters);

        protected unsafe void CheckDisposed()
        {
            if (conn.Pointer == null)
                throw new ObjectDisposedException(nameof(NonSynchronizedTypedDuckDbConnection));
        }

        public unsafe IReadOnlyList<FunctionInfo> RegisterFunctions(Assembly methodsInAssembly)
        {
            lock (database)
            {
                var fns = FunctionUtils.RegisterFunctions(conn, methodsInAssembly);
                foreach (var item in fns)
                {
                    EnqueueFunctionDisposal(item);
                }
                return fns;
            }
        }
        public unsafe void RegisterFunctions(Type methodsInType)
        {
            lock (database)
            {
                var fns = FunctionUtils.RegisterFunctions(conn, methodsInType);
                foreach (var item in fns)
                {
                    EnqueueFunctionDisposal(item);
                }
            }
        }
        public unsafe void RegisterFunction(string name, Delegate @delegate)
        {
            lock (database)
            {
                if (AddFunction(name, @delegate))
                {
                    var fn = FunctionUtils.RegisterFunction(conn, name, @delegate);
                    EnqueueFunctionDisposal(fn);
                }
            }

        }

        public unsafe void RegisterFunction(MethodInfo method)
        {
            lock (database)
            {
                if (AddFunction(FunctionUtils.GetRegistrationNameForMethod(method), method))
                {
                    var fn = FunctionUtils.RegisterFunction(conn, method);
                    EnqueueFunctionDisposal(fn);
                }

            }

        }

        private bool AddFunction(string name, object method)
        {
            if (database.RegisteredFunctions.TryGetValue(name, out var existing))
            {
                if (object.ReferenceEquals(existing, method)) return false;
                throw new ArgumentException($"A function with the same name (but different lambda or method) was already added to a different connection to the same database file. This is not supported. Ensure that the lambda or method passed to {nameof(RegisterFunction)} is reference-equal for all connections to this database file. This can also be caused by Visual Studio \"Edit and Continue\".");
            }
            database.RegisteredFunctions.Add(name, method);
            return true;
        }

        private void EnqueueFunctionDisposal(FunctionInfo obj)
        {
            database.ToDispose ??= new();
            database.ToDispose.Add(obj);
        }


        public bool LogQueries { get; set; }

        protected virtual void MaybeLog(string sql)
        {
            if (LogQueries)
                Console.Error.WriteLine("[DuckDB] " + DuckDbUtils.ToSingleLineSql(sql));
        }

        public virtual unsafe void CreateTable<T>(string name, bool replaceIfExisting = false, string[]? primaryKey = null)
        {
            var sb = new StringBuilder(replaceIfExisting ? "CREATE OR REPLACE TABLE " : "CREATE TABLE ");
            sb.Append(name);
            DuckDbTypeCreator.GetDuckDbType(typeof(T), null, out _, out _, out _, out var structureFields);
            if (structureFields == null) throw new ArgumentException("A non-primitive, non-list type is required.");
            sb.Append('(');
            var any = false;
            foreach (var col in structureFields)
            {
                if (any) sb.Append(", ");
                any = true;
                sb.Append(col!.DuckDbFieldName);
                sb.Append(' ');
                var structural = DuckDbStructuralType.CreateStructuralType(col.FieldType);
                sb.Append(structural.ToSql());
            }
            if (primaryKey == null)
            {
                primaryKey = structureFields.Where(x => x!.ClrField.GetCustomAttribute<KeyAttribute>() != null).Select(x => x!.DuckDbFieldName).ToArray();
                if (primaryKey.Length == 0) primaryKey = null;
            }
            if (primaryKey != null)
            {
                sb.Append($", PRIMARY KEY ({string.Join(", ", primaryKey)})");
            }
            sb.Append(')');
            ExecuteNonQuery(sb.ToString());
        }

        public virtual unsafe void CreateEnum(Type t)
        {
            CheckDisposed();
            DuckDbUtils.CreateEnumType(Handle, t);
        }
        public abstract OwnedDuckDbResult ExecuteUnsafe(string sql, params object?[]? parameters);


	}
}
