using System;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct DuckDbTimestampTz(long Micros)
    {
        // https://duckdb.org/docs/lts/sql/data_types/timestamp
        // "Despite the name, a TIMESTAMP WITH TIME ZONE does not store time zone information. Instead, it only stores the INT64 number of non-leap microseconds since the Unix epoch"

        public static DuckDbTimestampTz FromDateTime(DateTime d)
        {
            return new((d - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerMicrosecond);
        }
        public readonly DateTime AsDateTime => checked(DateTime.UnixEpoch.AddTicks(Micros * TimeSpan.TicksPerMicrosecond));
    }

}

