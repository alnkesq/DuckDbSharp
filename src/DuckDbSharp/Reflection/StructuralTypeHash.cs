using DuckDbSharp.Bindings;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DuckDbSharp.Reflection
{
    public record struct StructuralTypeHash(UInt128 High, UInt128 Low)
    {
        internal static StructuralTypeHash Hash(StructuralTypeHash inner, DUCKDB_TYPE type)
        {
            return Hash(MemoryMarshal.AsBytes(new ReadOnlySpan<StructuralTypeHash>(in inner)), type);
        }

        internal static StructuralTypeHash Hash(DUCKDB_TYPE type)
        {
            return new StructuralTypeHash(0, (uint)DuckDbTypeToDuckDbStableType(type));
        }

        public static DUCKDB_TYPE_STABLE DuckDbTypeToDuckDbStableType(DUCKDB_TYPE type)
        {
            return DuckDbTypeToDuckDbTypeStable[(int)type];
        }

        static StructuralTypeHash()
        {
            var nonStableValues = Enum.GetValues<DUCKDB_TYPE>();
            var nonStableToStable = new DUCKDB_TYPE_STABLE[nonStableValues.Max(x => (int)x) + 1];
            foreach (var nonStable in nonStableValues)
            {
                var stable = Enum.Parse<DUCKDB_TYPE_STABLE>(nonStable.ToString());
                nonStableToStable[(int)nonStable] = stable;
            }
            DuckDbTypeToDuckDbTypeStable = nonStableToStable;
        }

        private readonly static DUCKDB_TYPE_STABLE[] DuckDbTypeToDuckDbTypeStable;

        internal static StructuralTypeHash Hash(ReadOnlySpan<byte> span, DUCKDB_TYPE type)
        {
            var h = MemoryMarshal.Cast<byte, UInt128>(SHA256.HashData(span));
            return new StructuralTypeHash(h[0], h[1] + (uint)DuckDbTypeToDuckDbStableType(type));
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

