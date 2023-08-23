using DuckDbSharp.Bindings;
using System;
using System.Linq;
using System.Reflection;

namespace DuckDbSharp.Reflection
{
    internal unsafe class DuckDbPrimitiveTypeConverter
    {

        public DuckDbPrimitiveTypeConverter(Type clrType, DUCKDB_TYPE duckDbType)
        {
            Kind = duckDbType;
            ClrType = clrType;
            SerializationType = clrType;
        }

        public DuckDbPrimitiveTypeConverter(Type clrType, EnumSerializationInfo enumInfo)
        {
            ClrType = clrType;
            SerializationType = enumInfo.SerializationMethod.ReturnType;
            SerializeMethod = enumInfo.SerializationMethod;
            DeserializeMethod = enumInfo.DeserializationMethod;
            EnumInfo = enumInfo;
        }

        private struct EnsureUnmanaged<T> where T : unmanaged { }
        public DuckDbPrimitiveTypeConverter(Type clrType, DUCKDB_TYPE duckDbType, MethodInfo serializeMethod, MethodInfo deserializeMethod, MethodInfo? isNullishMethod = null)
        {
            this.ClrType = clrType;
            this.Kind = duckDbType;
            this.SerializeMethod = serializeMethod;
            this.DeserializeMethod = deserializeMethod;
            this.IsNullishMethod = isNullishMethod;
            this.SerializationType = serializeMethod.ReturnType;
            if (DeserializeMethod.ReturnType != clrType) throw new ArgumentException($"Incorrect return type for deserialization method (should be {clrType}).");
            var serializeParams = SerializeMethod.GetParameters();
            if (serializeParams[0].ParameterType != clrType) throw new ArgumentException($"Incorrect first parameter type for serialization method (should be {clrType})");
            if (DeserializeMethod.GetParameters()[0].ParameterType != SerializationType) throw new ArgumentException("Incorrect first parameter type for deserialization method, it should be the same as the return type of the serialization method.");

            try
            {
                typeof(EnsureUnmanaged<>).MakeGenericType(SerializationType);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Return type of serialization method should not be a managed reference or a type that contains managed references: {SerializationType}");
            }
            if (isNullishMethod != null)
            {
                if (IsNullishMethod.ReturnType != typeof(bool)) throw new ArgumentException("If specified, nullishness method should return bool.");
                if (IsNullishMethod.GetParameters()[0].ParameterType != ClrType) throw new ArgumentException($"If specified, nullishness method should take a single parameter of type {clrType}.");
            }
            NeedsArena = serializeParams.Length == 2;

        }

        public DuckDbPrimitiveTypeConverter(Type clrType, DUCKDB_TYPE duckDbType, string serializeMethodName, string deserializeMethodName, string? isNullishMethodName = null)
            : this(
                  clrType,
                  duckDbType,
                  typeof(SerializationHelpers).GetMethod(serializeMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                  typeof(SerializationHelpers).GetMethod(deserializeMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic),
                  isNullishMethodName != null ? typeof(SerializationHelpers).GetMethod(isNullishMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null
                  )
        {
        }
        public readonly DUCKDB_TYPE Kind;
        public readonly Type ClrType;
        public readonly Type SerializationType;
        public readonly MethodInfo? SerializeMethod;
        public readonly MethodInfo? DeserializeMethod;
        public readonly MethodInfo? IsNullishMethod;
        public readonly bool NeedsArena;
        public bool IsEnum => EnumInfo != null;
        public readonly EnumSerializationInfo? EnumInfo;

        internal static EnumSerializationInfo CreateEnumInfo(Type clrType)
        {
            /*
            var converter = DuckDbSerializer.GetDuckDbEnumUnderlyingType(t);
            var members = converter.DuckDbToClr.Select(x => Enum.GetName(t, x)).ToArray();
            */
            var clrValues = Enum.GetValues(clrType);
            var clrUnderlyingType = Enum.GetUnderlyingType(clrType);
            if (clrUnderlyingType == typeof(long) || clrUnderlyingType == typeof(ulong)) throw new NotSupportedException("Enums backed by Int64 or UInt64 are not supported.");
            long max = 0;
            object? maxClr = null;
            foreach (var clrValue in clrValues)
            {
                long value = Convert.ToInt64(clrValue);
                if (value < 0) throw new NotSupportedException($"Enum {clrType} has negative members, which are not supported in DuckDB. Consider serializing as string.");
                if (value >= max)
                {
                    max = value;
                    maxClr = clrValue;
                }
            }
            var clrBits = Type.GetTypeCode(clrUnderlyingType) switch
            {
                TypeCode.Byte or TypeCode.SByte => 8,
                TypeCode.UInt16 or TypeCode.Int16 => 16,
                TypeCode.UInt32 or TypeCode.Int32 => 32,
                _ => throw new NotSupportedException()
            };
            if (max > 1_000_000) throw new NotSupportedException($"Enum {clrType} has a very high maximum numeric value, which has performance implications in DuckDB (unlike in .NET). Consider serializing as string.");
            var duckBits =
                max <= byte.MaxValue ? 8 :
                max <= ushort.MaxValue ? 16 :
                max <= uint.MaxValue ? 32 :
                throw new NotSupportedException();
            var serializer = typeof(SerializationHelpers).GetMethod($"SerializeEnum_{clrBits}To{duckBits}", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(clrType);
            var deserializer = typeof(SerializationHelpers).GetMethod($"DeserializeEnum_{duckBits}To{clrBits}", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(clrType);

            var members = new string[max + 1];
            foreach (var clrValue in clrValues)
            {
                var value = Convert.ToUInt64(clrValue);
                members[(int)value] = clrValue.ToString();
            }
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i] == null) members[i] = AnonymousEnumMemberPrefix + i;
            }


            var membersUtf8 = members.Select(x => (ScopedString)x).ToArray();
            return new EnumSerializationInfo((ScopedString)DuckDbUtils.ToDuckCaseField(clrType.Name), membersUtf8, serializer, deserializer, maxClr);
        }
        internal const string AnonymousEnumMemberPrefix = "Anonymous_";

    }

}

