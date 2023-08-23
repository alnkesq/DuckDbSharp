using DuckDbSharp.Bindings;
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DuckDbSharp.Reflection
{
    internal record struct StructuralTypeHash(UInt128 High, UInt128 Low)
    {
        internal static StructuralTypeHash Hash(StructuralTypeHash inner, DUCKDB_TYPE type)
        {
            return Hash(MemoryMarshal.AsBytes(new ReadOnlySpan<StructuralTypeHash>(in inner)), type);
        }

        internal static StructuralTypeHash Hash(DUCKDB_TYPE type)
        {
            return new StructuralTypeHash(0, (uint)type);
        }

        internal static StructuralTypeHash Hash(ReadOnlySpan<byte> span, DUCKDB_TYPE type)
        {
            var h = MemoryMarshal.Cast<byte, UInt128>(SHA256.HashData(span));
            return new StructuralTypeHash(h[0], h[1] + (uint)type);
        }

        internal static StructuralTypeHash Parse(string hash)
        {
            var s = MemoryMarshal.Cast<byte, UInt128>(Convert.FromHexString(hash));
            return new StructuralTypeHash(s[0], s[1]);
        }

        public override string ToString()
        {
            return Convert.ToHexString(MemoryMarshal.Cast<UInt128, byte>(new[] { High, Low })).ToLowerInvariant();
        }
    }
}

