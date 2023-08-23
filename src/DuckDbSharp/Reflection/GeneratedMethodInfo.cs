using System;
using System.Linq.Expressions;

namespace DuckDbSharp.Reflection
{
    internal record GeneratedMethodInfo(string Name, Delegate Delegate, Type? IsRootForType, DuckDbStructuralType? IsRootForStructuralType, Expression? Body, ParameterExpression[]? Parameters, string? CSharpBody, string[]? CSharpParameterNames);
}

