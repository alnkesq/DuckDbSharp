using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DuckDbSharp.Bindings
{
    public unsafe static class BindingUtils
    {
        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        public static void FreeGcHandle(void* ptr)
        {
            var handle = GCHandle.FromIntPtr((nint)ptr);
            (handle.Target as IDisposable)?.Dispose();
            handle.Free();
        }


        [DebuggerStepThrough]
        public static void CheckState(duckdb_state duckdbState)
        {
            if (duckdbState != duckdb_state.DuckDBSuccess) throw new DuckDbException();
        }

        public static TPtr*[] ToPointerArray<T, TPtr>(T[] items, PointerSelector<T, TPtr> selector) where TPtr : unmanaged
        {
            var result = new TPtr*[items.Length];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = selector(items[i]);
            }
            return result;
        }

        public static T ReadGcHandle<T>(void* ptr) where T : class
        {
            var handle = GCHandle.FromIntPtr((nint)ptr);
            var target = handle.Target;
            if (target == null) throw new Exception();
            return (T)target;
        }



        public static T* Alloc<T>() where T : unmanaged
        {
            var mem = NativeMemory.AllocZeroed((nuint)sizeof(T));
            return (T*)mem;
        }
        public static void Free<T>(T* ptr) where T : unmanaged
        {
            NativeMemory.Free(ptr);
        }

        public static string? ToString(byte* v)
        {
            if (v == null) return null;
            return Marshal.PtrToStringUTF8((nint)v);
        }

        internal static T Move<T>(ref T ownedPtr)
        {
            var copy = ownedPtr;
            ownedPtr = default;
            return copy;
        }
    }

    public unsafe delegate TPtr* PointerSelector<T, TPtr>(T input) where TPtr : unmanaged;
}

