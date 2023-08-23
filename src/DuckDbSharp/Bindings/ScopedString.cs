using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace DuckDbSharp.Bindings
{
    public unsafe struct ScopedString : IDisposable
    {
        private byte* ptr;
        private int length;

        private ScopedString(byte* ptr, int length) : this()
        {
            this.ptr = ptr;
            this.length = length;
        }

        public void Dispose()
        {
            if (ptr != null)
            {
                Span.Clear();
                NativeMemory.Free(ptr);
                this = default;
            }
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Span);
        }
        public readonly Span<byte> Span => new Span<byte>(ptr, length);
        public readonly int Length => length;

        public static explicit operator ScopedString(string? str)
        {
            if (str == null) return default;
            var maxBytes = Encoding.UTF8.GetMaxByteCount(str.Length) + 1;
            var ptr = NativeMemory.Alloc((nuint)maxBytes);
            var buffer = new Span<byte>(ptr, maxBytes);
            if (Utf8.FromUtf16(str, buffer, out _, out var written) != System.Buffers.OperationStatus.Done)
                throw new Exception();
            buffer[written] = 0;
            return new ScopedString((byte*)ptr, written);
        }

        public static implicit operator byte*(ScopedString str)
        {
            return str.ptr;
        }
        public readonly byte* Pointer => ptr;
    }


}
