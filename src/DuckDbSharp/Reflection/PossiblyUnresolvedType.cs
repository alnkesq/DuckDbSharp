using System;

namespace DuckDbSharp.Reflection
{
    public record struct PossiblyUnresolvedType(Type? Type, string? UnresolvedType);
}

