using DuckDbSharp;
using DuckDbSharp.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DuckDbSharp.Tests
{
    public class MiscTests
    {
        [Fact]
        public static void ReadParquet()
        {
            // Generated using select a.* from (select unnest([{a:1, b: 'b'}, {a:2, b:'bb'}]) as a)
            Assert.Equal(new[] { new SomeParquetRow(1, "b"), new SomeParquetRow(2, "bb") }, DuckDbUtils.QueryParquet<SomeParquetRow>("../../../data/1.parquet", "select * from data order by a"));
        }

        [Fact]
        public static void WriteParquet()
        {
            var rows = new[] { new SomeParquetRow(1, "b"), new SomeParquetRow(2, "bb") };
            DuckDbUtils.WriteParquet("output.parquet", rows);
            Assert.Equal(rows, DuckDbUtils.QueryParquet<SomeParquetRow>("output.parquet", "select * from data order by a"));
        }



    }
    record SomeParquetRow(int a, string b);
}

