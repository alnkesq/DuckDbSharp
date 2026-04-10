using DuckDbSharp.Types;

namespace DuckDbSharp.Bindings
{
    public class DuckDbDeserializationContext
    {
        internal ClrStringCache[]? CacheEntries;
    }
    internal struct ClrStringCache
    {
        public DuckString DuckString;
        public string? ClrString;
    }
}

