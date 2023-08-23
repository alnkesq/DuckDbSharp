using DuckDbSharp.Functions;
using System.Collections.Generic;

namespace DuckDbSharp
{
    public class EnumerableParameterSlot
    {
        internal int ParameterId;
        internal FunctionInfo Function;
        internal Dictionary<EnumerableParametersInvocationToken, object> ValueByQueryToken = new();
    }
}

