using DuckDbSharp.Bindings;
using System;
using System.Collections.Generic;

namespace DuckDbSharp
{
    public class DuckDbDatabase
    {
        private readonly static Dictionary<string, DuckDbDatabase> dbs = new();
        internal List<EnumerableParameterSlot> EnumerableParameterSlots = new();
        internal List<IDisposable>? ToDispose;
        private OwnedDuckDbDatabase database;
        private int usageCount;
        private DuckDbDatabase(OwnedDuckDbDatabase database)
        {
            this.database = database;
        }

        internal Dictionary<string, object> RegisteredFunctions = new();

        internal unsafe static OwnedDuckDbConnection AcquireConnection(string? path, out DuckDbDatabase ownerDb)
        {
            lock (dbs)
            {
                if (path == null || !dbs.TryGetValue(path, out var db))
                {
                    db = new DuckDbDatabase(DuckDbUtils.OpenDatabase(path));
                    if (path != null)
                        dbs.Add(path, db);
                }
                var connection = DuckDbUtils.ConnectCore(db.database);
                db.usageCount++;
                ownerDb = db;
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

