using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using DuckDbSharp.Functions;
using DuckDbSharp.Reflection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using JsonConverter = Newtonsoft.Json.JsonConverter;

namespace DuckDbSharp
{

    public static class FuzzingTests
    {
        [Fact]
        public static void Fuzz()
        {
            using var fuzzing = new Fuzzing<DuckDbSharp.FuzzingTypes.MyClass>();
            fuzzing.GenerateCode("DuckDbFuzzingGeneratedTypes");
            fuzzing.TestRoundtrips(100, 1000);
        }
    }


    public class Fuzzing<T> : IDisposable
    {
        private readonly ThreadSafeTypedDuckDbConnection Connection;
        public T[]? Expected;
        private readonly JsonSerializerSettings JsonSettings;
        public Fuzzing()
        {
            Connection = ThreadSafeTypedDuckDbConnection.CreateInMemory();
            Connection.RegisterFunction("myfunc", () => Expected);
            JsonSettings = new Newtonsoft.Json.JsonSerializerSettings();
            JsonSettings.Converters.Add(new CustomEnumConverter());
            JsonSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
            JsonSettings.Converters.Add(new CustomDateTimeConverter());
        }

        class CustomDateTimeConverter : DateTimeConverterBase
        {
            public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
            {
                throw new NotSupportedException();
            }

            public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
            {
                if (value is DateTime a)
                {
                    if (a.Nanosecond == 0 && a.Microsecond == 0 && a.Millisecond == 0)
                    {
                        writer.WriteValue(a.ToString("yyyy-MM-dd HH:mm:ss"));
                        return;
                    }
                    // Avoids spurious diffs due to precision loss (ticks vs micros).
                    writer.WriteValue(a.ToString("yyyy-MM-dd HH:mm:ss.ffffff"));
                }
                else throw new NotSupportedException();
            }
        }


        public static T MakeRandom(Random r, bool allowNull = true) => (T)MakeRandom(typeof(T), r, allowNull: allowNull);
        public static object MakeRandom(Type t, Random r, bool allowNull = true)
        {
            if (t.IsArray)
            {
                if (allowNull && r.NextDouble() < 0.2) return null;
                var len = r.Next(16);
                var elementType = t.GetElementType();
                var arr = Array.CreateInstance(elementType, len);
                for (int i = 0; i < len; i++)
                {
                    arr.SetValue(MakeRandom(elementType, r), i);
                }
                return arr;
            }
            if (Nullable.GetUnderlyingType(t) is { } inner)
            {
                if (allowNull && r.NextDouble() < 0.2) return null;
                return MakeRandom(inner, r);
            }
            if (t.IsClass && allowNull && r.NextDouble() < 0.2) return null;
            if (t == typeof(int)) return r.Next(0, 50);
            if (t == typeof(long)) return r.NextInt64();
            if (t == typeof(string)) return r.Next(0, 1000).ToString();
            if (t == typeof(bool)) return r.NextDouble() > 0.5;
            if (t == typeof(byte)) return (byte)r.Next(0, 256);
            if (t.IsPrimitive) throw new NotImplementedException();
            var ctor = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault();
            if (ctor == null || ctor.GetParameters().Length == 0)
            {
                var obj = Activator.CreateInstance(t);
                var fields = t.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    field.SetValue(obj, MakeRandom(field.FieldType, r));
                }

                var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    prop.SetValue(obj, MakeRandom(prop.PropertyType, r));
                }
                return obj;
            }
            else
            {
                var parameters = ctor.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = MakeRandom(parameters[i].ParameterType, r);
                }
                return ctor.Invoke(args);
            }
        }




        private void PopulateWithRandomData(Random r, int minArrayLength, int maxArrayLength)
        {
            Expected = Enumerable.Range(0, r.Next(minArrayLength, maxArrayLength)).Select(x => MakeRandom(r, false)).ToArray();
        }


        public void TestRoundtrip(T[] data)
        {
            Expected = data;
            TestRoundtrip(-1);
        }
        private void TestRoundtrip(int seedForDebugging)
        {

            var expectedAsJson = JsonConvert.SerializeObject(Expected, Formatting.Indented, JsonSettings);
            string? actualAsDuckDbJson;
            bool nativeJsonIsEqual;
            if (AlsoCompareWithNativeJson)
            {
                actualAsDuckDbJson = Connection.ExecuteScalar<string>("select to_json(array_agg(z)) from myfunc() z");
                actualAsDuckDbJson = JToken.Parse(actualAsDuckDbJson).ToString(Formatting.Indented);

                nativeJsonIsEqual = actualAsDuckDbJson == expectedAsJson;
                if (!nativeJsonIsEqual)
                {
                    Console.WriteLine("DIFFERENT FROM JSON for seed (might be ok if some struct types are marked as nullish) " + seedForDebugging);
                }
            }
            else
            {
                nativeJsonIsEqual = true;
                actualAsDuckDbJson = null;
            }
            var actualFromQuery = Connection.Execute<T>("select * from myfunc()").ToArray();
            var actualFromQueryAsJson = JsonConvert.SerializeObject(actualFromQuery, Formatting.Indented, JsonSettings);
            var isEqual2 = actualFromQueryAsJson == expectedAsJson;
            if (!isEqual2)
            {
                Console.WriteLine("DIFFERENT FROM QUERY for seed" + seedForDebugging);
            }
            var shouldOutput = !nativeJsonIsEqual || !isEqual2 || (AlwaysExportFirstIterationOutput && seedForDebugging is 0 or -1);

            if (shouldOutput)
            {
                if (AlsoCompareWithNativeJson)
                    File.WriteAllText("fuzzing_actualJson.json", actualAsDuckDbJson);
                File.WriteAllText("fuzzing_expected.json", expectedAsJson);
                File.WriteAllText("fuzzing_actualDeser.json", actualFromQueryAsJson);
            }
            if (WaitForUserConfirmationBeforeProcedingIfJsonDiffers && (!isEqual2 || !nativeJsonIsEqual))
            {
                Console.WriteLine("Diffs found. Press ENTER to continue.");
                Console.ReadLine();
            }
            if (!isEqual2)
            {
                throw new Exception();
            }
        }
        public bool AlsoCompareWithNativeJson;
        public bool AlwaysExportFirstIterationOutput;
        public bool WaitForUserConfirmationBeforeProcedingIfJsonDiffers;

        public bool RoundtripsCorrectlyMultipleAttempts(T[] items, int attemptCount)
        {
            var results = Enumerable.Range(0, attemptCount).Select(x => RoundtripsCorrectly(items)).ToArray();
            if (results.Distinct().Count() == 1) return results[0];
            throw new Exception("Inconsistent results");
        }


        public bool RoundtripsCorrectly(T[] items)
        {
            if (AlsoCompareWithNativeJson) throw new NotSupportedException();
            Expected = items;
            var expectedAsJson = JsonConvert.SerializeObject(Expected, Formatting.Indented, JsonSettings);
            var actualFromQuery = Connection.Execute<T>("select * from myfunc()").ToArray();
            var actualFromQueryAsJson = JsonConvert.SerializeObject(actualFromQuery, Formatting.Indented, JsonSettings);
            return actualFromQueryAsJson == expectedAsJson;
        }


        public void TestRoundtrips(int iterations = 1000, int avgArrayLength = 100)
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                if (sw.ElapsedMilliseconds > 2000)
                {
                    Console.WriteLine($"Iteration: {i}");
                    sw.Restart();
                }
                PopulateWithRandomData(new Random(i), 0, avgArrayLength * 2);
                TestRoundtrip(i);
            }
        }

        public void GenerateCode(string aotSerializersNamespace)
        {
            if (Expected == null)
                PopulateWithRandomData(new Random(0), 10, 10);
            DuckDbUtils.GenerateCSharpTypes(new Reflection.CodeGenerationOptions
            {
                Connection = Connection,
                GenerateAotSerializers = true,
                Specifications = new SerializerSpecification[]
                {
                    new(typeof(T)),
                    new("select * from myfunc()", "MyClassQuery", typeof(T))
                },
                DestinationPath = @"..\..\..\FuzzingGenerated.cs",
                Namespace = aotSerializersNamespace,
            });
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

    }

    internal class CustomEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum;
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var type = value.GetType();
            var flagsAttr = type.GetCustomAttribute<FlagsAttribute>();
            if (flagsAttr == null)
            {
                writer.WriteValue(value.ToString());
                return;
            }
            var flags = DuckDbTypeCreator.GetFlagsEnumFields(type);
            writer.WriteStartObject();
            var valAsUlong = Convert.ToUInt64(value);
            foreach (var flag in flags)
            {
                writer.WritePropertyName(flag.DuckDbFieldName);
                writer.WriteValue((valAsUlong & (1ul << flag.FlagEnumShift)) != 0);
            }
            writer.WriteEndObject();
        }
    }
}

