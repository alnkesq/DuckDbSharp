using System;

namespace DuckDbSharp.Functions
{
    class BindContext
    {
        public required FunctionInfo Function;
        public required object?[] Args;
        public object? PrecomputedReturnValue;
        public bool HasPrecomputedReturnValue;
        public Func<object, object>? LateTransformer;
        public Type? FinalElementType;
    }


}
