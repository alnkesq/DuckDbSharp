using DuckDbSharp.Bindings;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct DuckString
    {
        [FieldOffset(0)] public int Length;
        [FieldOffset(4)] public uint Inlined0;
        [FieldOffset(8)] public uint Inlined1;
        [FieldOffset(12)] public uint Inlined2;
        [FieldOffset(8)] public byte* LongStringPointer;

        public const int TRUNCATED_STRING_INLINE_LENGTH = 4;
        public const int STRING_INLINE_LENGTH = 12;
        public readonly override string ToString() => Encoding.UTF8.GetString(Span);

        public static DuckString Create(ReadOnlySpan<byte> source, NativeArenaSlim arena)
        {
            if (source.Length > STRING_INLINE_LENGTH)
            {
                var remainingSpace = arena.GetRemaingSpaceInCurrentChunk();
                if (remainingSpace.Length < source.Length)
                {
                    arena.Grow(source.Length);
                    remainingSpace = arena.GetRemaingSpaceInCurrentChunk();
                }
                source.CopyTo(remainingSpace);
            }
            return CreateWithPreallocatedAndPrepopulatedSpaceInArena(source, arena);
        }
        public static DuckString CreateWithPreallocatedAndPrepopulatedSpaceInArena(ReadOnlySpan<byte> source, NativeArenaSlim arena)
        {
            DuckString d = default;
            d.Length = source.Length;
            var inlinePtr = &d.Inlined0;

            var inline = new Span<byte>((byte*)inlinePtr, STRING_INLINE_LENGTH);

            if (source.Length > STRING_INLINE_LENGTH)
            {
                d.LongStringPointer = arena.NextAllocation;
                arena.AdvanceBy(source.Length);
                source.Slice(0, TRUNCATED_STRING_INLINE_LENGTH).CopyTo(inline);
            }
            else
            {
                source.CopyTo(inline);
            }
            return d;
        }

        [UnscopedRef]
        public readonly ReadOnlySpan<byte> Span
        {
            get
            {
                fixed (uint* ptr = &Inlined0)
                {
                    if (Length <= STRING_INLINE_LENGTH)
                    {
                        return new ReadOnlySpan<byte>((byte*)ptr, Length);

                    }
                    else
                    {
                        return new ReadOnlySpan<byte>(LongStringPointer, Length);
                    }
                }
            }
        }

    }


}
