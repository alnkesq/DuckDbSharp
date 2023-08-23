using System;
using System.Linq.Expressions;

namespace DuckDbSharp
{


    internal record struct SerializerParameters(Type InputArrayElementType, ParameterExpression VectorPtr, ParameterExpression Objects, ParameterExpression ObjectsLength, Expression ParentValidity, ParameterExpression Arena)
    {
    }


}

