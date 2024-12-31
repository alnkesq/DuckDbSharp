using DuckDbSharp.Bindings;
using DuckDbSharp.Functions;
using DuckDbSharp.Reflection;
using DuckDbSharp.Types;
using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

namespace DuckDbSharp
{
    public static class SerializationHelpers
    {


        public static DuckDbInterval SerializeTimespan(TimeSpan d)
        {
            return DuckDbInterval.FromTimeSpan(d);
        }

        public static TimeSpan DeserializeTimespan(DuckDbInterval interval)
        {
            return interval.ToTimeSpan();
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDateTimeNullish(DateTime d) => d.Ticks == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDateOnlyNullish(DateOnly d) => d == default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DuckDbTimestampMicros SerializeTimestampMicros(DateTime d) => DuckDbTimestampMicros.FromDateTime(d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime DeserializeTimestampMicros(DuckDbTimestampMicros t) => t.AsDateTime;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SerializeDateOnly(DateOnly d) => d.DayNumber - UnixDateEpoch.DayNumber;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateOnly DeserializeDateOnly(int unixDay) => UnixDateEpoch.AddDays(unixDay);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DuckDbUuid SerializeGuid(Guid guid) => (DuckDbUuid)guid;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Guid DeserializeGuid(DuckDbUuid uuid) => (Guid)uuid;

        public readonly static DateOnly UnixDateEpoch = new DateOnly(1970, 1, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DuckString SerializeUri(Uri url, NativeArenaSlim arena)
        {
            return SerializeString(url.OriginalString, arena); // OriginalString instead of AbsoluteUri because it provides better roundtripping.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Uri? DeserializeUri(DuckString d, DuckDbDeserializationContext deserializationContext)
        {
            return DeserializeString(d, deserializationContext) is { } s ? new Uri(s) : null;
        }

        public static string? DeserializeString(DuckString d, DuckDbDeserializationContext deserializationContext)
        {
            if (d.Length == 0) return string.Empty;

            if (d.Length > 1024) return d.ToString(); // Avoid caching big strings

            const int CACHE_SIZE = 4096;
            ClrStringCache[] cache;
            cache = deserializationContext.CacheEntries ??= new ClrStringCache[CACHE_SIZE];
            var entryIndex = (uint)HashCode.Combine(d.Hi, d.Lo) % CACHE_SIZE;
            ref ClrStringCache cachedEntry = ref cache[(int)entryIndex];
            if (cachedEntry.DuckString.Hi == d.Hi && cachedEntry.DuckString.Lo == d.Lo)
            {
                return cachedEntry.ClrString;
            }
            cachedEntry.ClrString = d.ToString();
            cachedEntry.DuckString = d;
            return cachedEntry.ClrString;
        }
        public static byte[] DeserializeByteArray(DuckString d)
        {
            return d.Span.ToArray();
        }

        public static Memory<byte> DeserializeMemory(DuckString d) => DeserializeByteArray(d);
        public static ReadOnlyMemory<byte> DeserializeReadOnlyMemory(DuckString d) => DeserializeByteArray(d);
        public unsafe static DuckString SerializeByteArray(byte[] str, NativeArenaSlim arena) => DuckString.Create(str, arena);
        public unsafe static DuckString SerializeMemory(Memory<byte> str, NativeArenaSlim arena) => DuckString.Create(str.Span, arena);
        public unsafe static DuckString SerializeReadOnlyMemory(ReadOnlyMemory<byte> str, NativeArenaSlim arena) => DuckString.Create(str.Span, arena);


        public unsafe static DuckString SerializeString(string str, NativeArenaSlim arena)
        {
            var buffer = arena.GetRemaingSpaceInCurrentChunk();
            Span<byte> encoded;
            var status = Utf8.FromUtf16(str, buffer, out _, out var written);
            if (status == System.Buffers.OperationStatus.Done) { /* nothing to do */ }
            else if (status == System.Buffers.OperationStatus.DestinationTooSmall)
            {
                var byteCount = Encoding.UTF8.GetMaxByteCount(str.Length);
                arena.Grow(byteCount);
                buffer = arena.GetRemaingSpaceInCurrentChunk();
                status = Utf8.FromUtf16(str, buffer, out _, out written);
                if (status != System.Buffers.OperationStatus.Done)
                    throw new Exception();
            }
            else throw new Exception();

            encoded = buffer.Slice(0, written);

            return DuckString.CreateWithPreallocatedAndPrepopulatedSpaceInArena(encoded, arena);
        }


#if NET8_0_OR_GREATER
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public unsafe static TTo BitCast<TFrom, TTo>(TFrom value) where TFrom: unmanaged where TTo: unmanaged => Unsafe.BitCast<TFrom, TTo>(value);
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public unsafe static TTo BitCast<TFrom, TTo>(TFrom value) where TFrom : unmanaged where TTo : unmanaged => *(TTo*)&value;
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint SerializeEnum_32To32<T>(T e) where T : unmanaged => BitCast<T, uint>(e);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort SerializeEnum_32To16<T>(T e) where T : unmanaged => (ushort)BitCast<T, uint>(e);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte SerializeEnum_32To8<T>(T e) where T : unmanaged => (byte)BitCast<T, uint>(e);


        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort SerializeEnum_16To16<T>(T e) where T : unmanaged => BitCast<T, ushort>(e);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte SerializeEnum_16To8<T>(T e) where T : unmanaged => (byte)BitCast<T, ushort>(e);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte SerializeEnum_8To8<T>(T e) where T : unmanaged => BitCast<T, byte>(e);


        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T DeserializeEnum_32To32<T>(uint v) where T : unmanaged => BitCast<uint, T>(v);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T DeserializeEnum_16To32<T>(ushort v) where T : unmanaged => BitCast<uint, T>(v);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T DeserializeEnum_8To32<T>(byte v) where T : unmanaged => BitCast<uint, T>(v);


        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T DeserializeEnum_16To16<T>(ushort v) where T : unmanaged => BitCast<ushort, T>(v);
        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T DeserializeEnum_8To16<T>(byte v) where T : unmanaged => BitCast<ushort, T>(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)] public static T DeserializeEnum_8To8<T>(byte v) where T : unmanaged => BitCast<byte, T>(v);


        public static IEnumerable<T> EnumerateArrayGeneric<T>(T[] arr)
        {
            foreach (var item in arr)
            {
                yield return item;
            }
        }
        public static IEnumerable<T> EnumerateObjectArrayAsGeneric<T>(object[] arr)
        {
            foreach (var item in arr)
            {
                yield return (T)item;
            }
        }

        [DebuggerStepThrough]
        public unsafe static nint GetDataChunkVector(nint chunk, ulong colIdx) => (nint)Methods.duckdb_data_chunk_get_vector((_duckdb_data_chunk*)chunk, colIdx);
        [DebuggerStepThrough]
        public unsafe static nint GetVectorValidity(nint vector) => (nint)Methods.duckdb_vector_get_validity((_duckdb_vector*)vector);


        [DebuggerStepThrough]
        public unsafe static nint GetVectorValidityAndSetAll(nint vector)
        {
            Methods.duckdb_vector_ensure_validity_writable((_duckdb_vector*)vector); // this implicitly also sets everything to 1.
            return (nint)Methods.duckdb_vector_get_validity((_duckdb_vector*)vector);
        }


        public unsafe static bool IsPresent(nint ptr, int index)
        {
            if (ptr == 0) return true;
            var longIdx = index / 64;
            var bit = index % 64;
            var val = ((ulong*)ptr)[longIdx];
            return (val & (1ul << index)) != 0;
        }

        public unsafe static void SetNotPresent(nint ptr, int index)
        {
            var longIdx = index / 64;
            var bit = index % 64;
            ((ulong*)ptr)[longIdx] &= ~(1ul << index);
        }

        public static int GetSubObjectsCount(nint validityVector, int parentCount)
        {
            if (validityVector == 0) return parentCount;
            var num = 0;
            for (int i = 0; i < parentCount; i++)
            {
                if (IsPresent(validityVector, i)) num++;
            }
            return num;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public unsafe static nint GetStructureChildVector(nint parent, ulong index) => (nint)Methods.duckdb_struct_vector_get_child((_duckdb_vector*)parent, index);

        [DebuggerStepThrough]
        public unsafe static nint GetSublistChildVector(nint parent)
        {
            var vector = (_duckdb_vector*)parent;
            return (nint)Methods.duckdb_list_vector_get_child(vector);
        }
        [DebuggerStepThrough]
        public unsafe static nint GetSubarrayChildVector(nint parent)
        {
            var vector = (_duckdb_vector*)parent;
            return (nint)Methods.duckdb_array_vector_get_child(vector);
        }
        [DebuggerStepThrough]
        public unsafe static nint GetSublistChildVectorAndReserve(nint parent, int totalCount)
        {
            var vector = (_duckdb_vector*)parent;
            BindingUtils.CheckState(Methods.duckdb_list_vector_reserve(vector, (ulong)totalCount));
            BindingUtils.CheckState(Methods.duckdb_list_vector_set_size(vector, (ulong)totalCount));
            return (nint)Methods.duckdb_list_vector_get_child(vector);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static TDuckDb ThrowEnumOutOfRange<T, TDuckDb>(T item)
        {
            throw new NotSupportedException($"Encountered an enum value ({item}) which is above the expected maximum for {typeof(T)}. Consider applying a [DuckDbSerializeAs(typeof(string))] or [DuckDbSerializeAs(typeof({Enum.GetUnderlyingType(typeof(T))}))] to this enum or field.");
        }

        public static void ShowSpanForDebugging(Span<OffsetAndCount> span)
        {
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public unsafe static Span<T> GetVectorData<T>(nint vector, int size)
        {
            return new Span<T>(Methods.duckdb_vector_get_data((_duckdb_vector*)vector), size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static void AssignSpanItem<T>(Span<T> span, int index, T value) => span[index] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        public static T ReadSpanItem<T>(Span<T> span, int index) => span[index];

        public static IEnumerable<Box<T>> BoxMany<T>(IEnumerable<T> items)
        {
            return items.Select(x => new Box<T>() { Value = x });
        }

        [DebuggerStepThrough]
        public static T NewSkipCtor<T>() where T : class
        {
            return Unsafe.As<T>(RuntimeHelpers.GetUninitializedObject(typeof(T)));
        }

        [DebuggerStepThrough]
        public unsafe static int GetChunkSize(nint chunk) => checked((int)Methods.duckdb_data_chunk_get_size((_duckdb_data_chunk*)chunk));

        public static int GetTotalItems(Span<OffsetAndCount> span, nint validityVector)
        {
            ulong num = 0;
            for (int i = 0; i < span.Length; i++)
            {
                // Ranges can overlap, e.g. DuckDB reuses the same list if it's identical.
                if (IsPresent(validityVector, i))
                    num = Math.Max(num, span[i].End);
            }
            return checked((int)num);
        }
        public static int GetArraysTotalItems(int objectsLength, nint validityVector, ulong fixedLength)
        {
            ulong num = 0;
            for (int i = 0; i < objectsLength; i++)
            {
                if (IsPresent(validityVector, i))
                    num += fixedLength;
            }
            return checked((int)num);
        }

        [DebuggerStepThrough]
        public static int GetSublistOffset(Span<OffsetAndCount> span, int rowId) => (int)span[rowId].Offset;
        [DebuggerStepThrough]
        public static int GetSublistSize(Span<OffsetAndCount> span, int rowId) => (int)span[rowId].Count;

        [DebuggerStepThrough]
        public static bool IsDefaultStructValue<T>(T value) where T : struct => value.Equals(default(T));

        public static void RegisterSerializer(Type type, RootSerializer? serializer)
        {
            SerializerCreationContext.RootSerializerCache.TryAdd(type, serializer);
        }

        public static void RegisterDeserializer(Type type, RootDeserializer deserializer, string? deserializerTypeHash)
        {
            var hash = StructuralTypeHash.Parse(deserializerTypeHash);
            SerializerCreationContext.RootDeserializerCache.TryAdd((type, hash), deserializer);
            SerializerCreationContext.StructuralHashToRegisteredClrType.TryAdd(hash, type);
        }


        public static DuckString SerializeEnumAsString<T>(T value, NativeArenaSlim arena)
        {
            return SerializeString(value.ToString(), arena);
        }

        [SkipLocalsInit]
        public static T DeserializeEnumFromString<T>(DuckString str) where T : unmanaged
        {
            if (str.Length == 0) return default;
            var anonymousPrefix = "Anonymous_"u8;
            if (str.Span.StartsWith(anonymousPrefix))
            {
                if (Utf8Parser.TryParse(str.Span.Slice(anonymousPrefix.Length), out uint num, out var consumed) && anonymousPrefix.Length + consumed == str.Length)
                {
                    if (Unsafe.SizeOf<T>() == 4) return BitCast<uint, T>(num);
                    else if (Unsafe.SizeOf<T>() == 2) return BitCast<ushort, T>(checked((ushort)num));
                    else if (Unsafe.SizeOf<T>() == 1) return BitCast<byte, T>(checked((byte)num));
                }
            }
            var maxLength = Encoding.UTF8.GetMaxCharCount(str.Length);
            Span<char> buffer = maxLength < 1024 ? stackalloc char[maxLength] : new char[maxLength];
            var succeeded = Utf8.ToUtf16(str.Span, buffer, out _, out var written);
            if (succeeded != System.Buffers.OperationStatus.Done) throw new Exception();
            return Enum.Parse<T>(buffer.Slice(0, written));
        }

        public static ref T GetReferenceToNullableWrappedValue<T>(T?[] arr, int index) where T : struct
        {
            return ref Unsafe.AsRef(in Nullable.GetValueRefOrDefaultRef<T>(in arr[index]));
        }

        [DebuggerStepThrough]
        public static T[] RentArray<T>(int minLength)
        {
            var arr = ArrayPool<T>.Shared.Rent(minLength);
            return arr;
        }
        [DebuggerStepThrough]
        public static T[] RentArrayZeroed<T>(int minLength)
        {
            var arr = ArrayPool<T>.Shared.Rent(minLength);
            Array.Clear(arr);
            return arr;
        }

        [DebuggerStepThrough]
        public static void ReleaseArray<T>(T[] array) => ArrayPool<T>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        [DebuggerStepThrough]
        public static T[] CreateArray<T>(int length) => length == 0 ? Array.Empty<T>() : new T[length];


        public static TBuffer CopyToFixedLengthArray<TBuffer, TItem>(TItem[] source, int offset, int length) where TBuffer : new()
        {
            var buffer = new TBuffer();
            var bufferAsSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, TItem>(ref buffer), length);
            source.AsSpan(offset, length).CopyTo(bufferAsSpan);
            return buffer;
        }
        public static void CopyFromFixedLengthArray<TBuffer, TItem>(TBuffer buffer, TItem[] destination, int offset, int length) where TBuffer : new()
        {
            var bufferAsSpan = MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, TItem>(ref buffer), length);
            bufferAsSpan.CopyTo(destination.AsSpan(offset));
        }
    }
}

