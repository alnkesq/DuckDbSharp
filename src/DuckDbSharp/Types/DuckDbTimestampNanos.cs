using System;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct DuckDbTimestampNanos(long Nanos)
    {
        const long NanosecondsPerTick = 100;
        public static DuckDbTimestampNanos FromDateTime(DateTime d)
        {
            // Only works until approx year 2200, then it overflows...
            return new(checked((d - DateTime.UnixEpoch).Ticks * NanosecondsPerTick));
        }
        public readonly DateTime AsDateTime => checked(DateTime.UnixEpoch.AddTicks(Nanos / NanosecondsPerTick));
    }

}

