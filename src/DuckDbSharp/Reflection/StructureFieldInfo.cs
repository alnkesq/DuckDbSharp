using System;
using System.Linq.Expressions;

namespace DuckDbSharp.Reflection
{
    internal class StructureFieldInfo
    {
        public string DuckDbFieldName { get; private init; }
        public Type FieldType { get; private init; }
        public DuckDbStructuralType? FieldStructuralType { get; private init; }
        public CreateGetExpressionDelegate CreateGetExpression { get; private init; }
        public CreateSetExpressionDelegate CreateSetExpression { get; private init; }
        public GetterKey CacheKey { get; private init; }
        public StructureFieldInfo(string duckDbFieldName, Type fieldType, DuckDbStructuralType fieldStructuralType, CreateGetExpressionDelegate createGetExpression, CreateSetExpressionDelegate createSetExpression, GetterKey cacheKey)
        {
            this.DuckDbFieldName = duckDbFieldName;
            this.FieldStructuralType = fieldStructuralType;
            this.FieldType = fieldType;
            this.CreateGetExpression = createGetExpression;
            this.CreateSetExpression = createSetExpression;
            this.CacheKey = cacheKey;
        }

        public override string ToString()
        {
            return this.DuckDbFieldName;
        }

        internal int FlagEnumShift = -1;
    }
    internal delegate Expression CreateGetExpressionDelegate(Expression obj, SerializerCreationContext context);
    internal delegate Expression CreateSetExpressionDelegate(Expression destinationArray, Expression destinationIndex, Expression value, SerializerCreationContext context);

}

