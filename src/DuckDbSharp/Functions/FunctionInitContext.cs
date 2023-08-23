using DuckDbSharp.Bindings;
using System;
using System.Collections;

namespace DuckDbSharp.Functions
{
    class FunctionInitContext : IDisposable
    {
        public IEnumerator? Enumerator;
        public NativeArenaSlim? Arena;
        public RootSerializer? RootSerializer;

        public void Dispose()
        {
            (Enumerator as IDisposable)?.Dispose();
            Arena?.Dispose();
        }
    }


}
