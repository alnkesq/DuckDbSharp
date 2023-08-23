using DuckDbSharp.Reflection;
using System;
using System.Linq.Expressions;

namespace DuckDbSharp
{


    internal record struct DeserializerParameters(Type OutputArrayElementType, DuckDbStructuralType OutputArrayStructuralType, ParameterExpression VectorPtr, ParameterExpression Objects, ParameterExpression ObjectsLength)
    {
    }


}

