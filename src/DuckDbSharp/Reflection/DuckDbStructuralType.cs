using DuckDbSharp.Bindings;
using DuckDbSharp.Functions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace DuckDbSharp.Reflection
{
    internal unsafe class DuckDbStructuralType : IEquatable<DuckDbStructuralType>
    {
        [JsonConstructor]
        public DuckDbStructuralType(DUCKDB_TYPE Kind, List<string>? EnumMembers, DuckDbStructuralType? ElementType, List<StructuralTypeStructureField>? StructureFields, int? FixedSizeArrayLength)
        {
            this.Kind = Kind;
            this.EnumMembers = EnumMembers;
            this.ElementType = ElementType;
            this.StructureFields = StructureFields;
            this.FixedSizeArrayLength = FixedSizeArrayLength;
        }
        internal DuckDbStructuralType(StructuralTypeHash hash, DUCKDB_TYPE kind)
        {
            Hash = hash;
            Kind = kind;
        }

        public DUCKDB_TYPE Kind { get; }
        internal readonly StructuralTypeHash Hash;
        private readonly static ConcurrentDictionary<StructuralTypeHash, DuckDbStructuralType> DuckDbStructuralTypes = new();
        public List<string>? EnumMembers { get; init; }
        public List<StructuralTypeStructureField>? StructureFields { get; init; }
        public DuckDbStructuralType? ElementType { get; init; }
        public int? FixedSizeArrayLength { get; init; }

        internal static DuckDbStructuralType BooleanStructuralType = CreateStructuralTypeForPrimitive(DUCKDB_TYPE.DUCKDB_TYPE_BOOLEAN);
        // internal bool IsRoot => Kind == default && StructureFields != null;

        public override string ToString()
        {
            return ToSql();
        }

        public override int GetHashCode()
        {
            return (int)Hash.Low;
        }
        public override bool Equals(object? obj)
        {
            return obj is DuckDbStructuralType other && this == other;
        }
        public bool Equals(DuckDbStructuralType? other)
        {
            return this == other;
        }

        public static DuckDbStructuralType CreateStructuralType(Type clrType) => CreateStructuralType(DuckDbTypeCreator.CreateLogicalType(clrType, null));

        public static DuckDbStructuralType CreateStructuralType(_duckdb_logical_type* lt)
        {
            var kind = Methods.duckdb_get_type_id(lt);
            if (kind == DUCKDB_TYPE.DUCKDB_TYPE_ENUM) return CreateStructuralTypeForEnum(lt);
            else if (kind == DUCKDB_TYPE.DUCKDB_TYPE_STRUCT) return CreateStructuralTypeForStructure(lt);
            else if (kind == DUCKDB_TYPE.DUCKDB_TYPE_LIST) return CreateStructuralTypeForList(lt);
            else if (kind == DUCKDB_TYPE.DUCKDB_TYPE_ARRAY) return CreateStructuralTypeForFixedLengthArray(lt, (int)Methods.duckdb_array_type_array_size(lt));
            else return CreateStructuralTypeForPrimitive(kind);
        }


        private static DuckDbStructuralType CreateStructuralTypeForPrimitive(DUCKDB_TYPE k)
        {
            var hash = StructuralTypeHash.Hash(k);
            return CreateOrReuseStructuralType(hash, () => new DuckDbStructuralType(hash, k));
        }

        internal static DuckDbStructuralType CreateStructuralTypeForResult(duckdb_result* result, Type type)
        {
            return CreateStructuralTypeForResult(CreateStructuralTypeForResult(result), type);
        }
        internal static DuckDbStructuralType CreateStructuralTypeForResult(DuckDbStructuralType structuralType, Type type)
        {
            if (SerializerCreationContext.IsWrappedSingleColumn(type)) structuralType = DuckDbUtils.GetSingleWrappedColumnType(structuralType);
            return structuralType;
        }

        internal static DuckDbStructuralType CreateStructuralTypeForResult(duckdb_result* result, bool expectSingleColumn = false)
        {

            var size = (int)Methods.duckdb_column_count(result);
            var columns = new List<(OwnedDuckPtr<byte> Name, DuckDbStructuralType Type)>();

            using var buffer = new MemoryStream();
            for (int i = 0; i < size; i++)
            {
                var ptrName = /*don't free*/ (OwnedDuckPtr<byte>)Methods.duckdb_column_name(result, (ulong)i);
                using var fieldType = (OwnedDuckDbLogicalType)Methods.duckdb_column_logical_type(result, (ulong)i);
                var fieldStructuralType = CreateStructuralType(fieldType);
                columns.Add((ptrName, fieldStructuralType));

                buffer.Write(new Span<byte>(ptrName, ptrName.StringLength() + 1));
                buffer.Write(fieldStructuralType.Hash);
            }
            if (expectSingleColumn)
            {
                buffer.WriteByte((byte)1);
                if (columns.Count != 1) throw new DuckDbException($"Expected a single column, but {columns.Count} were returned.");
            }
            if (!buffer.TryGetBuffer(out var bufferSegment)) throw new Exception();
            var hash = StructuralTypeHash.Hash(bufferSegment.AsSpan(), DUCKDB_TYPE.DUCKDB_TYPE_STRUCT);
            var t = CreateOrReuseStructuralType(hash, () =>
            {
                return new DuckDbStructuralType(hash, DUCKDB_TYPE.DUCKDB_TYPE_STRUCT)
                {
                    StructureFields = columns.Select(x =>
                    {
                        if (expectSingleColumn && size == 1) return new StructuralTypeStructureField("Value", x.Type);
                        return new StructuralTypeStructureField(x.Name.ToStringUtf8(), x.Type);
                    }).ToList()
                };
            });
            return t;
            // Column names must NOT be freed!
        }

        private static DuckDbStructuralType CreateStructuralTypeForList(_duckdb_logical_type* lt)
        {
            using var elementType = (OwnedDuckDbLogicalType)Methods.duckdb_list_type_child_type(lt);
            var elementTypeStructuralType = CreateStructuralType(elementType);
            var hash = StructuralTypeHash.Hash(elementTypeStructuralType.Hash, DUCKDB_TYPE.DUCKDB_TYPE_LIST);
            return CreateOrReuseStructuralType(hash, () => new DuckDbStructuralType(hash, DUCKDB_TYPE.DUCKDB_TYPE_LIST) { ElementType = elementTypeStructuralType });
        }
        private static DuckDbStructuralType CreateStructuralTypeForFixedLengthArray(_duckdb_logical_type* lt, int length)
        {
            using var elementType = (OwnedDuckDbLogicalType)Methods.duckdb_array_type_child_type(lt);
            var elementTypeStructuralType = CreateStructuralType(elementType);
            var hash = StructuralTypeHash.Hash(elementTypeStructuralType.Hash, length);
            return CreateOrReuseStructuralType(hash, () => new DuckDbStructuralType(hash, DUCKDB_TYPE.DUCKDB_TYPE_ARRAY) { ElementType = elementTypeStructuralType, FixedSizeArrayLength = length });
        }

        private static DuckDbStructuralType CreateStructuralTypeForStructure(_duckdb_logical_type* lt)
        {

            var size = (int)Methods.duckdb_struct_type_child_count(lt);
            var members = new List<(OwnedDuckPtr<byte> Name, DuckDbStructuralType Type)>();
            var memberTypes = new List<Type>();
            try
            {
                using var buffer = new MemoryStream();
                for (int i = 0; i < size; i++)
                {
                    var ptrName = (OwnedDuckPtr<byte>)Methods.duckdb_struct_type_child_name(lt, (ulong)i);
                    using var fieldType = (OwnedDuckDbLogicalType)Methods.duckdb_struct_type_child_type(lt, (ulong)i);
                    var structuralFieldType = CreateStructuralType(fieldType);
                    members.Add((ptrName, structuralFieldType));

                    buffer.Write(new Span<byte>(ptrName, ptrName.StringLength() + 1));
                    buffer.Write(structuralFieldType.Hash);
                }
                if (!buffer.TryGetBuffer(out var bufferSegment)) throw new Exception();
                var hash = StructuralTypeHash.Hash(bufferSegment.AsSpan(), DUCKDB_TYPE.DUCKDB_TYPE_STRUCT);
                var result = CreateOrReuseStructuralType(hash, () =>
                {
                    return new DuckDbStructuralType(hash, DUCKDB_TYPE.DUCKDB_TYPE_STRUCT)
                    {
                        StructureFields = members.Select(x => new StructuralTypeStructureField(x.Name.ToStringUtf8(), x.Type)).ToList()
                    };
                });
                return result;
            }
            finally
            {
                foreach (var item in members)
                {
                    item.Name.Dispose();
                }
            }
        }





        private static DuckDbStructuralType CreateStructuralTypeForEnum(_duckdb_logical_type* lt)
        {
            var size = Methods.duckdb_enum_dictionary_size(lt);
            var members = new List<OwnedDuckPtr<byte>>();
            try
            {
                using var buffer = new MemoryStream();
                for (int i = 0; i < size; i++)
                {
                    var ptr = (OwnedDuckPtr<byte>)Methods.duckdb_enum_dictionary_value(lt, (ulong)i);
                    members.Add(ptr);
                    var length = ptr.StringLength();
                    buffer.Write(new Span<byte>(ptr, length + 1));
                }
                if (!buffer.TryGetBuffer(out var bufferSegment)) throw new Exception();
                var hash = StructuralTypeHash.Hash(bufferSegment.AsSpan(), DUCKDB_TYPE.DUCKDB_TYPE_ENUM);
                return CreateOrReuseStructuralType(hash, () =>
                {
                    var enumMembers = new List<string>();
                    for (int i = 0; i < members.Count; i++)
                    {
                        enumMembers.Add(members[i].ToStringUtf8());
                    }
                    return new DuckDbStructuralType(hash, DUCKDB_TYPE.DUCKDB_TYPE_ENUM)
                    {
                        EnumMembers = enumMembers
                    };
                });
            }
            finally
            {
                members.DisposeAll();
            }
        }

        internal static DuckDbStructuralType CreateOrReuseStructuralType(StructuralTypeHash hash, Func<DuckDbStructuralType> factory)
        {
            if (!DuckDbStructuralTypes.TryGetValue(hash, out var r))
            {
                return DuckDbStructuralTypes.GetOrAdd(hash, _ => factory());
            }
            return r;
        }

        public string ToSql()
        {
            if (FixedSizeArrayLength != null) return $"{ElementType.ToSql()}[{FixedSizeArrayLength}]";
            if (ElementType is not null) return $"{ElementType.ToSql()}[]";
            if (StructureFields is not null) return $"STRUCT({string.Join(", ", StructureFields.Select(x => x.Name + " " + x.FieldType.ToSql()))})";
            if (EnumMembers is not null) return $"ENUM({string.Join(", ", EnumMembers.Select(x => $"'{x}'"))})";

            return this.Kind.ToString().Substring("DUCKDB_TYPE_".Length);
        }

        internal TypeKey GetTypeKey(string? nameHint = null)
        {
            return new TypeKey(this.Hash);
        }

        public static bool operator ==(DuckDbStructuralType a, DuckDbStructuralType b)
        {
            if (a is null || b is null) return a is null == b is null;
            return a.Hash == b.Hash;
        }
        public static bool operator !=(DuckDbStructuralType a, DuckDbStructuralType b)
        {
            return !(a == b);
        }
    }
    internal record struct StructuralTypeStructureField(string Name, DuckDbStructuralType FieldType);
}

