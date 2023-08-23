using System;

namespace DuckDbSharp
{

    [AttributeUsage(AttributeTargets.Method)]
    public class DuckDbFunctionAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DuckDbIgnoreAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum)]
    public class DuckDbGeneratedTypeAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DuckDbIncludeAttribute : Attribute
    {
        public DuckDbIncludeAttribute()
        {

        }
        public DuckDbIncludeAttribute(string? duckName)
        {
            this.DuckName = duckName;
        }
        public string? DuckName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class DuckDbDefaultValueIsNullishAttribute : Attribute
    {
    }


    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum)]
    public class DuckDbSerializeAsAttribute : Attribute
    {
        public Type Type { get; private set; }
        public DuckDbSerializeAsAttribute(Type type)
        {
            Type = type;
        }
    }
}

