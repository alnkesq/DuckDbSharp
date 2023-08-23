using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ProtoMemberAttribute = ProtoBuf.ProtoMemberAttribute;
using DuckDbSharp;
using System.Data;
using System.IO;
using System.Data.Common;
using Dapper;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DuckDbSharp.Benchmarks
{
    internal class Program
    {
        internal const int SmallNorthwindRows = 91;
        internal const int DatasetCopies = (100000 / SmallNorthwindRows) + 1;


        public static List<Customer> ReadDataset()
        {
            using var db = ThreadSafeTypedDuckDbConnection.CreateInMemory();
            var small = db.Execute<Customer>($"select * from read_csv_auto(\"{GetNorthwindCustomersPath()}\", header=true)").ToList();
            var dataset = new List<Customer>();
            for (int i = 0; i < Program.DatasetCopies; i++)
            {
                dataset.AddRange(small);
            }
            return dataset;
        }

        public static object GetNorthwindCustomersPath()
        {
            var directory = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            while (!Directory.Exists(Path.Combine(directory, "../../tests")))
            {
                directory = Path.GetDirectoryName(directory);
                if (directory == null) throw new Exception();
            }

            return Path.Combine(directory, "../DuckDbSharp.Tests/data/NorthwindsCustomers.csv");
        }



        private static void Main()
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }


 
    }

    public class DuckDbSharpQueryingBenchmark : IDisposable
    {
        private ThreadSafeTypedDuckDbConnection db;

        [GlobalSetup]
        public void Setup()
        {
            db = ThreadSafeTypedDuckDbConnection.CreateInMemory();
            db.ExecuteNonQuery($"create table customer_small as select * from read_csv_auto(\"{Program.GetNorthwindCustomersPath()}\", header=true) ");
            db.CreateTable<Customer>("customer");
            Console.WriteLine("Preparing table...");
            for (int i = 0; i < Program.DatasetCopies; i++)
            {
                db.ExecuteNonQuery("insert into customer select * from customer_small");
            }
        }

        [Benchmark]
        public void DuckDbSharpQuerying()
        {
            _ = db.Execute<Customer>("select * from customer").ToList();
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }

    public class DuckDbDotNetQueryingBenchmark : IDisposable
    {
        private DuckDB.NET.Data.DuckDBConnection db;

        [GlobalSetup]
        public void Setup()
        {
            db = new DuckDB.NET.Data.DuckDBConnection("DataSource=:memory:");
            db.Open();
            ExecuteNonQuery($"create table customer_small as select * from read_csv_auto(\"{Program.GetNorthwindCustomersPath()}\", header=true) ");
            ExecuteNonQuery("create table customer as select * from customer_small limit 0");
            for (int i = 0; i < Program.DatasetCopies; i++)
            {
                ExecuteNonQuery("insert into customer select * from customer_small");
            }

        }
        [Benchmark]
        public void DuckDbDotNetQuerying()
        {
            var _ = db.Query<Customer>("select * from customer").ToList();
        }

        public void Dispose()
        {
            db.Dispose();
        }
        void ExecuteNonQuery(string sql)
        {
            using var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

    }

    public class NewtonsoftJsonDeserializationBenchmark
    {
        private MemoryStream ms;
        private readonly Newtonsoft.Json.JsonSerializer JsonSerializer = Newtonsoft.Json.JsonSerializer.CreateDefault();

        [GlobalSetup]
        public void Setup()
        {
            ms = new MemoryStream();
            using (var tw = new StreamWriter(ms, leaveOpen: true))
            {
                JsonSerializer.Serialize(tw, Program.ReadDataset());
            }
        }

        [Benchmark]
        public void NewtonsoftJsonDeserialization()
        {
            ms.Seek(0, SeekOrigin.Begin);
            using var tr = new StreamReader(ms, leaveOpen: true);
            using var jr = new Newtonsoft.Json.JsonTextReader(tr);
            _ = JsonSerializer.Deserialize<List<Customer>>(jr);

        }
    }


    public class ProtobufNetDeserializationBenchmark
    {
        private MemoryStream ms;

        [GlobalSetup]
        public void Setup()
        {
            ms = new MemoryStream();
            ProtoBuf.Serializer.Serialize(ms, Program.ReadDataset());
        }

        [Benchmark]
        public void ProtobufNetDeserialization()
        {
            ms.Seek(0, SeekOrigin.Begin);
            _ = ProtoBuf.Serializer.Deserialize<List<Customer>>(ms);
        }
    }


    [ProtoBuf.ProtoContract]
    public class Customer
    {
        [ProtoMember(1)] public string CustomerId;
        [ProtoMember(2)] public string CompanyName;
        [ProtoMember(3)] public string ContactName;
        [ProtoMember(4)] public string ContactTitle;
        [ProtoMember(5)] public string Address;
        [ProtoMember(6)] public string City;
        [ProtoMember(7)] public string Region;
        [ProtoMember(8)] public string PostalCode;
        [ProtoMember(9)] public string Country;
        [ProtoMember(10)] public string Phone;
        [ProtoMember(11)] public string Fax;
    }
}
