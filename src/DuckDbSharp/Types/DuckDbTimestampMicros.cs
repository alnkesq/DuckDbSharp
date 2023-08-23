using System;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct DuckDbTimestampMicros(long Micros)
    {
        public static DuckDbTimestampMicros FromDateTime(DateTime d)
        {
            return new((d - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerMicrosecond);
        }
        public readonly DateTime AsDateTime => checked(DateTime.UnixEpoch.AddTicks(Micros * TimeSpan.TicksPerMicrosecond));
    }

}

