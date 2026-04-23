using DuckDbSharp.Functions;
using System.Collections.Generic;

namespace DuckDbSharp
{
    internal class EnumerableParameterSlot
    {
        internal int ParameterId;
        internal required FunctionInfo Function;
        internal Dictionary<EnumerableParametersInvocationToken, object> ValueByQueryToken = [];
    }
}

