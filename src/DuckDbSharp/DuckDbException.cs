using System;

namespace DuckDbSharp
{
    public class DuckDbException : Exception
    {
        public DuckDbException(string? message = null, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}

