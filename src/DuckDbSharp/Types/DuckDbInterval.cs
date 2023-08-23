using System;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public record struct DuckDbInterval(int Months, int Days, long Micros)
    {
        const long MicrosPerDay = 24 * 3600 * 1_000_000L;
        const long MicrosPerMonthApprox = 30 * MicrosPerDay;
        public static DuckDbInterval FromTimeSpan(TimeSpan d)
        {
            var totalMicros = d.Ticks / TimeSpan.TicksPerMicrosecond;
            return new DuckDbInterval(0, (int)(totalMicros / MicrosPerDay), (int)(totalMicros % MicrosPerDay));
        }

        public TimeSpan ToTimeSpan()
        {
            if (Months != 0) throw new NotSupportedException($"DuckDB INTERVAL cannot be converted to System.TimeSpan due to non-zero number of months, which have a non-fixed duration: {this}. Consider using the native {nameof(DuckDbInterval)} type.");
            var totalMicros =
                Micros +
                Days * MicrosPerDay;
            return new TimeSpan(totalMicros);
        }

        public TimeSpan ToTimeSpanWithPossiblePrecisionLoss()
        {
            var totalMicros =
                Micros +
                Days * MicrosPerDay +
                Months * MicrosPerMonthApprox;
            return new TimeSpan(totalMicros * TimeSpan.TicksPerMicrosecond);
        }

    }

}

