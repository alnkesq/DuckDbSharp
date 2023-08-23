using System;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct DuckDbTimestampSeconds(long Seconds)
    {
        public static DuckDbTimestampSeconds FromDateTime(DateTime d)
        {
            return new((d - DateTime.UnixEpoch).Ticks / TimeSpan.TicksPerSecond);
        }
        public readonly DateTime AsDateTime => checked(DateTime.UnixEpoch.AddTicks(Seconds * TimeSpan.TicksPerSecond));
    }

}

