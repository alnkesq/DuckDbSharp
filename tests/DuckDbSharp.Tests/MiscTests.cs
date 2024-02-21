using DuckDbSharp;
using DuckDbSharp.Reflection;
using DuckDbSharp.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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

#if NET8_0_OR_GREATER
        [Fact]
        public static void UuidMarshalling()
        {
            using var db = ThreadSafeTypedDuckDbConnection.CreateInMemory();
            var uuid = db.Execute<DuckDbUuid>("select '6dc1a4ed-bff3-47a7-83fe-3beba206808c'::uuid").Single();
            Assert.Equal("6dc1a4ed-bff3-47a7-83fe-3beba206808c", uuid.ToString());
        }

        [Fact]
        public static void UuidComparisons()
        {
            var longs = new ulong[] 
            { 
                ulong.MinValue, 
                ulong.MaxValue,
                unchecked((ulong)long.MinValue), 
                long.MaxValue, 
                1UL,
                0xFFUL,
                0x1UL << 8,
                0xFFUL << 8,
                0x1UL << (64 - 16),
                0xFFUL << (64 - 16),
                0x1UL << (64 - 8),
                0xFFUL << (64 - 8),
            };
            var guids = new List<Guid>();
            foreach (var a in longs)
            {
                foreach (var b in longs)
                {
                    var bytes = MemoryMarshal.AsBytes<ulong>(new[] { a, b });
                    var guid = new Guid(bytes, true);
                    guids.Add(guid);
                }
            }

            guids = guids.OrderBy(x => x.ToString()).ToList();

            for (int i = 0; i < guids.Count; i++)
            {
                var guid = guids[i];
                var roundtrip = (Guid)(DuckDbUuid)guid;
                Assert.Equal(guid, roundtrip);
                if (i != 0)
                {
                    var cmp = ((DuckDbUuid)guids[i - 1]).CompareTo((DuckDbUuid)guid);
                    Assert.True(cmp < 0);
                }
            }
        }
#endif

    }
    record SomeParquetRow(int a, string b);
}

