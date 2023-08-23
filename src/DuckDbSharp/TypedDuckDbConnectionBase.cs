using DuckDbSharp.Bindings;
using DuckDbSharp.Functions;
using DuckDbSharp.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DuckDbSharp
{
    public abstract class TypedDuckDbConnectionBase : IDisposable
    {
        protected OwnedDuckDbConnection conn;

        internal protected List<EnumerableParameterSlot> EnumerableParameterSlots => database.EnumerableParameterSlots;

        private DuckDbDatabase database;
        public OwnedDuckDbConnection Handle => conn;

        protected TypedDuckDbConnectionBase(OwnedDuckDbConnection conn, DuckDbDatabase database)
        {
            this.conn = conn;
            this.database = database;
        }

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
        public void InsertRange<T>(string destinationTableOrView, IEnumerable<T> items)
        {
            InsertRange(null, destinationTableOrView, items);
        }

        public abstract void InsertRange<T>(string? destinationSchema, string destinationTableOrView, IEnumerable<T> items);
        public abstract IEnumerable<T> Execute<T>(string sql, params object[] parameters);
        public abstract IEnumerable Execute(string sql, params object[] parameters);

        public abstract T ExecuteScalar<T>(string sql, params object[] parameters);
        public abstract object ExecuteScalar(string sql, params object[] parameters);
        public abstract void ExecuteNonQuery(string sql, params object[] parameters);

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
                throw new ArgumentException($"A function with the same name (but different lambda or method) was already added to a different connection to the same database file. This is not supported. Ensure that the lambda or method passed to {nameof(RegisterFunction)} is reference-equal for all connections to this database file.");
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

        protected void MaybeLog(string sql)
        {
            if (LogQueries)
                Console.Error.WriteLine("[DuckDB] " + DuckDbUtils.ToSingleLineSql(sql));
        }

        public virtual unsafe void CreateTable<T>(string name, bool replaceIfExisting = false)
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
                sb.Append(col.DuckDbFieldName);
                sb.Append(' ');
                var structural = DuckDbStructuralType.CreateStructuralType(col.FieldType);
                sb.Append(structural.ToSql());
            }
            sb.Append(')');
            ExecuteNonQuery(sb.ToString());
        }

        public virtual unsafe void CreateEnum(Type t)
        {
            CheckDisposed();
            DuckDbUtils.CreateEnumType(Handle, t);
        }

    }
}
