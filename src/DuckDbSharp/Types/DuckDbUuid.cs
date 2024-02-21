using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Explicit)]
    [SuppressMessage("Design", "CA1036:Override methods on comparable types")]
    public readonly struct DuckDbUuid : IEquatable<DuckDbUuid>, IComparable<DuckDbUuid>
    {
        private DuckDbUuid(long upper, ulong lower)
        {
            this.upper = upper;
            this.lower = lower;
        }

        public static DuckDbUuid FromUpperLower(long upper, ulong lower) => new DuckDbUuid(upper, lower);

        public DuckDbUuid(ReadOnlySpan<byte> uuidBigEndian)
        {
            if (uuidBigEndian.Length != 16) throw new ArgumentException();

            lower = BinaryPrimitives.ReadUInt64BigEndian(uuidBigEndian.Slice(8));
            upper = BinaryPrimitives.ReadInt64BigEndian(uuidBigEndian);

            // As per duckdb/src/common/types/uuid.cpp
            upper ^= (1L << 63);

        }



        [FieldOffset(0)]
        private readonly ulong lower;
        [FieldOffset(8)]
        private readonly long upper;

        public override int GetHashCode()
        {
            return lower.GetHashCode();
        }

        public bool Equals(DuckDbUuid other)
        {
            return this.upper == other.upper && this.lower == other.lower;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is DuckDbUuid other && this == other;
        }

        public static bool operator ==(DuckDbUuid a, DuckDbUuid b) => a.Equals(b);
        public static bool operator !=(DuckDbUuid a, DuckDbUuid b) => !a.Equals(b);

        public int CompareTo(DuckDbUuid other)
        {
            var cmp = this.upper.CompareTo(other.upper);
            if (cmp != 0) return cmp;
            return this.lower.CompareTo(other.lower);
        }

        public void WriteBytes(Span<byte> destinationBigEndian)
        {
            BinaryPrimitives.WriteUInt64BigEndian(destinationBigEndian.Slice(8), this.lower);
            var upperFlipBack = this.upper;

            // As per duckdb/src/common/types/uuid.cpp
            upperFlipBack ^= (1L << 63);
            BinaryPrimitives.WriteInt64BigEndian(destinationBigEndian, upperFlipBack);
        }

        public byte[] ToByteArray()
        {
            var result = new byte[16];
            WriteBytes(result);
            return result;
        }

#if NET8_0_OR_GREATER


        public static DuckDbUuid Parse(string str) => Guid.Parse(str);

        [SkipLocalsInit]
        public static implicit operator Guid(DuckDbUuid uuid)
        {
            Span<byte> buffer = stackalloc byte[16];
            uuid.WriteBytes(buffer);
            return new Guid(buffer, bigEndian: true);
        }


        [SkipLocalsInit]
        public static implicit operator DuckDbUuid(Guid guid)
        {
            Span<byte> buffer = stackalloc byte[16];

            guid.TryWriteBytes(buffer, bigEndian: true, out _);
            return new DuckDbUuid(buffer);
        }

        public override string ToString()
        {
            return ((Guid)this).ToString();
        }
        public string ToString([StringSyntax("GuidFormat")] string? format)
        {
            return ((Guid)this).ToString(format);
        }

#endif

        public ulong GetLower()
        {
            return lower;
        }
        public long GetUpper()
        {
            return upper;
        }
#if NET8_0_OR_GREATER
        public string ToGuidString()
        {
            return ((Guid)this).ToString();
        }
#endif
    }
}