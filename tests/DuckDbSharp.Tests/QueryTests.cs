using Newtonsoft.Json;
using DuckDbSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace DuckDbSharp.Tests
{
    public class QueryTests : IDisposable
    {
        private readonly ThreadSafeTypedDuckDbConnection db = ThreadSafeTypedDuckDbConnection.CreateInMemory();


        [Fact]
        public void BadSql()
        {
            var ex = Assert.Throws<DuckDbException>(() => db.ExecuteScalar<int>("invalid sql", 2));
            Assert.Contains("Parser Error: syntax error", ex.Message);
            Assert.Contains("invalid sql", ex.Message);
        }
        [Fact]
        public void UnspeakableDynamicColumn()
        {
            var ex = Assert.Throws<NotSupportedException>(() => db.Execute("select 1 + 2").Cast<object>().ToList());
            Assert.Contains("Consider adding column aliases", ex.Message);
        }

        [Fact]
        public void ExecuteScalarTyped()
        {
            Assert.Equal(7, db.ExecuteScalar<int>("select 5 + ?", 2));
        }
        [Fact]
        public void ExecuteScalarUntyped()
        {
            Assert.Equal(7L, db.ExecuteScalar("select cast(5 + ? as long)", 2));
        }

        [Fact]
        public void ExecuteQuerySingleColumn()
        {
            Assert.Equal(new int[] { 1, 2, 3, 4 }, db.Execute<int>("select unnest([1, 2, 3, ?])", 4));
        }

        [Fact]
        public void ExecuteQueryUntyped()
        {
            Assert.Equal("[{'col':{'a':1,'b':[2,3]}}]", Serialize(db.Execute("select {a: 1, b: [2, 3]} as col")));
        }
        [Fact]
        public void CreateEnum() 
        {
            db.CreateEnum(typeof(SomeEnum));
            Assert.Equal(SomeEnum.A, db.ExecuteScalar<SomeEnum>("select 'A'::SomeEnum"));
        }

        [Fact]
        public void CreateTable()
        {
            db.CreateTable<SomeRow>("t");
            db.Insert("t", new SomeRow { Col1 = 1, Col2 = 2 });
            db.InsertRange("t", new[] { new SomeRow { Col1 = 3, Col2 = 4 } });
            Assert.Equal("[{'Col1':1,'Col2':2},{'Col1':3,'Col2':4}]", Serialize(db.Execute<SomeRow>("select * from t order by Col1")));
        }

        [Fact]
        public void RegisterFunction()
        {
            db.RegisterFunction("myfunc", (int a, string b) => new[] { new SomeRow { Col1 = a, Col2 = int.Parse(b) } });
            Assert.Equal("[{'Col1':7,'Col2':8}]", Serialize(db.Execute<SomeRow>("select * from myfunc(?, ?)", 7, "8")));
        }


        [Fact]
        public void PropagateFunctionError()
        {
            db.RegisterFunction("myfunc", (int a, string b) => new[] { new SomeRow { Col1 = a, Col2 = int.Parse(b) } });
            var ex = Assert.Throws<DuckDbException>(() => db.Execute<SomeRow>("select * from myfunc(?, ?)", 7, "aaaaa").ToList());
            Assert.Contains("The input string 'aaaaa' was not in a correct format", ex.Message);
        }

        [Fact]
        public void RegisterFunctionUntyped()
        {
            db.RegisterFunction("myfunc", (int a, string b) => (object)new[] { new SomeRow { Col1 = a, Col2 = int.Parse(b) } });
            Assert.Equal("[{'Col1':7,'Col2':8}]", Serialize(db.Execute<SomeRow>("select * from myfunc(?, ?)", 7, "8")));
        }

        [Fact]
        public void RegisterFunctionListOfScalars()
        {
            db.RegisterFunction("myfunc", (int a, string b) => new[] { a, int.Parse(b) });
            Assert.Equal(new[] { 7, 8 }, db.Execute<int>("select v.Value from myfunc(?, ?) v order by v", 7, "8"));
        }

        [Fact]
        public void RegisterFunctionReturningStructs()
        {
            db.RegisterFunction("myfunc", (int a, string b) => new[] { new { a, b } });
            Assert.Equal("[{'a':7,'b':'8'}]", Serialize(db.Execute("select * from myfunc(?, ?)", 7, "8")));
        }

        [Fact]
        public void RegisterFunctionTwiceButIdentical()
        {
            var deleg = () => new[] { 5 };
            db.RegisterFunction("myfunc", deleg);
            db.RegisterFunction("myfunc", deleg);
        }

        [Fact]
        public void RegisterFunctionTwiceAndDifferent()
        {
            db.RegisterFunction("myfunc", () => new[] { 6 });
            Assert.Throws<ArgumentException>(() => db.RegisterFunction("myfunc", () => new[] { 7 }));
        }

        [Fact]
        public void QueryVariousTypes() 
        {
            Assert.Equal("[{'a':1,'b':[2,3],'c':'c','d':[{'f1':'v1','f2':2,'f3':null},{'f1':'v11','f2':22,'f3':'v3'}],'e':'2009-01-01T00:00:00Z'}]", Serialize(db.Execute("select 1 as a, [2, 3] as b, 'c' as c, [{f1: 'v1', f2: 2, f3: null}, {f1: 'v11', f2: 22, f3: 'v3'}] as d, '2009-01-01'::timestamp as e")));
        }

        [Fact]
        public void ListParameterAsTable()
        {
            Assert.Equal(new[] { 1, 2, 3 }, db.Execute<int>("select t.Value from table_parameter_1() t", new object[] { new int[] { 1, 2, 3 } }));
        }

        [Fact]
        public void ListParameterAsArray()
        {
            Assert.Equal(new[] { 1, 2, 3 }, db.ExecuteScalar<int[]>("select array_transform(array_parameter_1(), x -> x.Value)", new object[] { new int[] { 1, 2, 3 } }));
        }


        [Fact]
        public void FunctionsFromAttributes()
        {
            db.RegisterFunctions(typeof(QueryTests));

            Assert.Equal(5, db.ExecuteScalar<int>("select Value from ReturnsInt()"));
            var json2 = Serialize(db.Execute("select * from ReturnsSingleMyResult()"), true);
            //File.WriteAllText("../../../data/2.json", json2);
            Assert.Equal(File.ReadAllText("../../../data/2.json"), json2);
            Assert.Equal(new[] { 0, 1, 2, 3, 4 }, db.Execute<int>("select Value from ReturnsInts()"));
            Assert.Equal("[{'SomeVal':5}]", Serialize(db.Execute("select * from ReturnsAnonymousObjects()")));
            var json3 = Serialize(db.Execute<MyResult>("select * from ReturnsComplexObjects(3, 'a')"), true);
            //File.WriteAllText("../../../data/3.json", json3);
            Assert.Equal(File.ReadAllText("../../../data/3.json"), json3);

            db.ExecuteNonQuery("call ReturnsVoid()");
            Assert.True(VoidFunctionWasCalled);
        }


        [DuckDbFunction] static int ReturnsInt() => 5;
        [DuckDbFunction] static MyResult ReturnsSingleMyResult() => new MyResult();
        [DuckDbFunction] static IEnumerable<int> ReturnsInts() => Enumerable.Range(0, 5);
        [DuckDbFunction] public static IEnumerable<object> ReturnsAnonymousObjects() => new[] { new { SomeVal = 5 } };

        [DuckDbFunction]
        public static IEnumerable<MyResult> ReturnsComplexObjects(int a, string q)
        {
            for (int i = 0; i < a; i++)
            {
                yield return new MyResult
                {
                    Kind = (DateTimeKind)1,
                    B = q,
                    Number = a,
                    SomeDate = new DateTime(2023, 10, 5),
                    FListInt = null,
                    FSub = null
                };
                foreach (var item in Enumerable.Range(0, 5).Select(x => new MyResult
                {
                    A = "val" + x,
                    B = "f2-" + x,
                    Number = x,
                    IsMultipleOfThree = x % 3 == 0,
                    SplitFlags = (StringSplitOptions)(x & 0b11),
                    Kind = (DateTimeKind)(x % 3),
                    FListInt = Enumerable.Range(0, x).Select(x => x).ToList()
                }))
                {
                    yield return item;
                }
            }
        }

        [DuckDbFunction] static void ReturnsVoid() => VoidFunctionWasCalled = true;
        static bool VoidFunctionWasCalled;



        private static string Serialize(object obj, bool expanded = false)
        {
            if (obj is IEnumerable ienu && !TypeSniffedEnumerable.IsFalseEnumerable(ienu.GetType()) && !(obj is Array))
            {
                obj = ienu.Cast<object>().ToArray();
            }
            if (expanded) return JsonConvert.SerializeObject(obj, Formatting.Indented).Replace("\r", null);
            return JsonConvert.SerializeObject(obj, Formatting.None).Replace('"', '\'');
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }

    enum SomeEnum
    { 
        A,
        B,
    }

    class SomeRow
    {
        public int Col1;
        public int Col2;
    }
    public class MyResult
    {
        public string A;
        public string B;
        public string NeverSetStr;
        public int Number;
        public bool IsMultipleOfThree;
        public DateTimeKind Kind;
        public StringSplitOptions SplitFlags;
        public DateTime SomeDate;
        public DateTime? SomeOptionalDate;
        public List<int>? FListInt = new List<int>() { 4, 5, 6 };
        public List<List<int>>? FListListInt = new List<List<int>>()
            {
                new List<int>() { 33, 44 },
                new List<int>() { 56 }
            };
        public List<Substruct> FListSub = new List<Substruct>()
            {
                new Substruct { Sub1 = 67, Sub2 = 68 },
                new Substruct { Sub1 = 6, Sub2 = 99 },
            };
        public Substruct? FSub = new Substruct { Sub1 = 6, Sub2 = 44 };
    }

    public class Substruct
    {
        public int Sub1;
        public int Sub2;
        public int Sub3;
    }

}

