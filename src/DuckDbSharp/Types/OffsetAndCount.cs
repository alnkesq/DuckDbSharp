using System.Runtime.InteropServices;

namespace DuckDbSharp.Types
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OffsetAndCount
    {
        public readonly ulong Offset;
        public readonly ulong Count;
        public readonly ulong End => Offset + Count;
        public OffsetAndCount(int offset, int count)
        {
            Offset = (uint)offset;
            Count = (uint)count;
        }



        public override string ToString()
        {
            return $"{{Offset={Offset}, Count={Count}}}";
        }
    }


}
