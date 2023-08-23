using System;

namespace DuckDbSharp.Functions
{
    class BindContext
    {
        public FunctionInfo Function;
        public object[] Args;
        public object? PrecomputedReturnValue;
        public bool HasPrecomputedReturnValue;
        public Func<object, object> LateTransformer;
        public Type? FinalElementType;
    }


}
