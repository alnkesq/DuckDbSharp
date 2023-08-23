using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DuckDbSharp
{

    internal abstract class TypeSniffedEnumerable : IEnumerable
    {
        internal object first;
        internal IEnumerator rest;
        internal IEnumerable original;
        internal Type elementType;
        public static IEnumerable Create(IEnumerable original, out Type elementType)
        {
            if (original is TypeSniffedEnumerable tse)
            {
                elementType = tse.elementType;
                return original;
            }
            elementType = TryGetEnumerableElementType(original.GetType());
            if (elementType != null && elementType != typeof(object)) return original;
            var enumerator = original.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                (enumerator as IDisposable)?.Dispose();
                elementType = typeof(object);
                return Array.Empty<object>();
            }
            var first = enumerator.Current;
            if (first == null) throw new InvalidOperationException("First element of weakly typed collection was null, unable to determine a type for the collection.");
            elementType = first.GetType();
            tse = (TypeSniffedEnumerable)Activator.CreateInstance(typeof(TypeSniffedEnumerable<>).MakeGenericType(elementType))!;
            tse.first = first;
            tse.rest = enumerator;
            tse.original = original;
            tse.elementType = elementType;
            return tse;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetUntypedEnumerator();
        }

        internal abstract IEnumerator GetUntypedEnumerator();

        public static Type? TryGetEnumerableElementType(Type? maybeEnumerable)
        {
            if (maybeEnumerable == null) return null;
            if (IsFalseEnumerable(maybeEnumerable)) return null;
            var enumerable =
                GetTypeDefinition(maybeEnumerable) == typeof(IEnumerable<>) ? maybeEnumerable :
                maybeEnumerable.GetInterfaces().FirstOrDefault(x => GetTypeDefinition(x) == typeof(IEnumerable<>));
            return enumerable != null ? enumerable.GetGenericArguments()[0] : null;
        }


        public static bool IsFalseEnumerable(Type type)
        {
            return type == typeof(string) || type == typeof(byte[]) ||
                (type.Namespace == "Newtonsoft.Json.Linq" && type.Name is "JToken" or "JValue" or "JObject" or "JProperty");
        }

        public static Type? GetTypeDefinition(Type? type)
        {
            return type != null && type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        }
    }

    internal class TypeSniffedEnumerable<T> : TypeSniffedEnumerable, IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator()
        {
            IEnumerator? enumerator = null;
            try
            {
                enumerator = Interlocked.Exchange(ref rest, null);
                if (enumerator != null)
                {
                    yield return (T)first;
                }
                else
                {
                    enumerator = original.GetEnumerator();
                }

                while (enumerator.MoveNext())
                {
                    yield return (T)enumerator.Current;
                }
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        internal override IEnumerator GetUntypedEnumerator()
        {
            return GetEnumerator();
        }
    }

}
