using System;

namespace DuckDbSharp.Reflection
{
    public class RecursiveTypeException : Exception
    {
        public Type[] Cycle;
        public RecursiveTypeException(string message, Type[] cycle) : base(message)
        {
            Cycle = cycle;
        }
    }


}
