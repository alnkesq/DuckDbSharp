using DuckDbSharp.Bindings;
using System;
using System.Reflection;

namespace DuckDbSharp.Reflection
{
    internal record EnumSerializationInfo(ScopedString EnumName, ScopedString[] Members, MethodInfo SerializationMethod, MethodInfo DeserializationMethod, object MaximumAllowedValue) : IDisposable
    {
        public void Dispose()
        {
            EnumName.Dispose();
            foreach (ref var item in Members.AsSpan())
            {
                item.Dispose();
            }
        }
    }

}

