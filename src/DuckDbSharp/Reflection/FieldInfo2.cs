using System;
using System.Reflection;

namespace DuckDbSharp.Reflection
{
    internal record class FieldInfo2(string Name, Type FieldType, MemberInfo Member)
    {
        internal DuckDbStructuralType FieldStructuralType;
    }

}

