using DuckDbSharp.Bindings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace DuckDbSharp
{
    public class DuckDbDatabase
    {
        private readonly static Dictionary<string, DuckDbDatabase> dbs = new();
        internal List<EnumerableParameterSlot> EnumerableParameterSlots = new();
        internal HashSet<Type> didRegisterFunctionsInTypes = new();
        internal List<IDisposable>? ToDispose;
        private OwnedDuckDbDatabase database;
        private int usageCount;
        public List<string>? _attachedFiles;
        private DuckDbDatabase(string? path, OwnedDuckDbDatabase database)
        {
            this.Path = path;
            this.database = database;
        }

        internal Dictionary<string, object> RegisteredTableFunctions = new();
        internal Dictionary<string, object> RegisteredScalarFunctions = new();
        internal string? Path;

        internal unsafe static OwnedDuckDbConnection AcquireConnection(string? path, out DuckDbDatabase ownerDb, int timeoutMs = 30000)
        {
            lock (dbs)
            {
                if (path == null || !dbs.TryGetValue(path, out var db))
                {
                    var sw = Stopwatch.StartNew();
                    while (true)
                    {
                        try
                        {
                            db = new DuckDbDatabase(path, DuckDbUtils.OpenDatabase(path));
                            break;
                        }
                        catch (DuckDbException) when (sw.ElapsedMilliseconds < timeoutMs)
                        {
                            Console.Error.WriteLine($"[DataIO] Database {path} is locked, waiting...");
                            Thread.Sleep(2000);
                        }
                    }
                    if (path != null)
                        dbs.Add(path, db);
                }
                ownerDb = db;
                return Connect(path, db);
            }

        }

        internal static unsafe OwnedDuckDbConnection Connect(string path, DuckDbDatabase db)
        {
            lock (dbs)
            {
                var connection = DuckDbUtils.ConnectCore(db.database);
                db.usageCount++;
                return new OwnedDuckDbConnection(connection.Pointer, () =>
                {
                    lock (dbs)
                    {
                        db.usageCount--;
                        if (db.usageCount == 0)
                        {
                            if (db.ToDispose != null)
                            {
                                foreach (var item in db.ToDispose)
                                {
                                    item.Dispose();
                                }
                                db.ToDispose = null;
                            }

                            lock (db.EnumerableParameterSlots)
                            {
                                foreach (var item in db.EnumerableParameterSlots)
                                {
                                    item?.Function.Dispose();
                                }
                            }


                            db.database.Dispose();
                            if (path != null)
                                dbs.Remove(path);
                        }
                    }
                });
            }
        }
    }
}

