using System;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct DuckDbTimestampMillis(long Millis)
    {
        public static DuckDbTimestampMillis FromDateTime(DateTime d)
        {
            return new((d - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerMillisecond);
        }
        public readonly DateTime AsDateTime => checked(DateTime.UnixEpoch.AddTicks(Millis * TimeSpan.TicksPerMillisecond));
    }

}

