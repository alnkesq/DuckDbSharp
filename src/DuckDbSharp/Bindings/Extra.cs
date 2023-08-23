using System;

namespace DuckDbSharp.Bindings
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.ReturnValue)]
    internal class NativeTypeName : Attribute
    {
        public NativeTypeName(string name)
        {
        }
    }

}

