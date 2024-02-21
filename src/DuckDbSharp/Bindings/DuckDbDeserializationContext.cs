using DuckDbSharp.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

