using AgileObjects.ReadableExpressions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DuckDbSharp.Reflection
{
    internal class CSharpCodeWriter
    {
        private readonly TextWriter writer;
        private string? currentNamespace;
        public CSharpCodeWriter(TextWriter writer)
        {
            this.writer = writer;
        }
        internal void Write(Type[] types)
        {
            foreach (var type in types)
            {
                Write(type);
            }
        }

        public void Write(Type type)
        {
            WriteNamespace(type.Namespace);
            if (DuckDbUtils.IsEnum(type))
            {
                WriteEnum(type);
            }
            else
            {
                WriteClass(type);
            }
        }


        public void Complete()
        {
            if (currentNamespace != null)
            {
                WriteLine("}");
                currentNamespace = null;
            }
        }

        public void WriteClass(Type type)
        {
            WriteLine("    [DuckDbGeneratedType]");
            WriteLine($"    public class {type.Name}");
            WriteLine("    {");
            foreach (var member in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {

                var duckName = DuckDbUtils.GetDuckName(member);
                if (duckName == member.Name)
                    WriteLine($@"        [DuckDbInclude]");
                else
                    WriteLine($@"        [DuckDbInclude(""{duckName}"")]");

                Write("        public ");
                var isNeverNull = member.GetCustomAttribute<NotNullAttribute>() != null;
                //if (isNeverNull && member.FieldType.IsClass)
                //    Write("required ");
                WriteTypeReference(member.FieldType, !isNeverNull);
                Write(" ");
                Write(member.Name);
                WriteLine(";");
            }
            WriteLine("    }");
        }

        public void WriteEnum(Type type)
        {
            WriteLine("    [DuckDbGeneratedType]");
            Write($"    public enum {type.Name} : ");
            WriteTypeReference(type.GetField("value__", BindingFlags.Instance | BindingFlags.Public).FieldType);
            WriteLine();
            WriteLine("    {");
            foreach (var member in Enum.GetValues(type))
            {
                Write("        ");
                Write(member.ToString());
                Write(" = ");
                Write(Convert.ToUInt64(member).ToString());
                WriteLine(",");
            }
            WriteLine("    }");

        }

        public void WriteLine()
        {
            writer.WriteLine();
        }

        public void WriteTypeReference(Type type, bool nullableReference = true)
        {
            if (Nullable.GetUnderlyingType(type) is { } nn)
            {
                WriteTypeReference(nn, false);
                Write("?");
                return;
            }
            var f = PrimitiveTypes.FirstOrDefault(x => x.Type == type);
            if (f != default)
            {
                Write(f.CSharp);
                if (type == typeof(string) && nullableReference)
                    Write("?");
                return;
            }
            if (type.IsArray)
            {
                WriteTypeReference(type.GetElementType(), false);
                Write("[]");
                if (nullableReference) Write("?");
                return;
            }
            Write(type.FullName.Replace("+", "."));
            if (!type.IsValueType && nullableReference)
                Write("?");
        }

        public void Write(string v)
        {
            writer.Write(v);
        }

        internal readonly static (Type Type, string CSharp)[] PrimitiveTypes = new[]
        {
            (typeof(void), "void"),
            (typeof(string), "string"),
            (typeof(bool), "bool"),
            (typeof(byte), "byte"),
            (typeof(sbyte), "sbyte"),
            (typeof(ushort), "ushort"),
            (typeof(short), "short"),
            (typeof(uint), "uint"),
            (typeof(int), "int"),
            (typeof(ulong), "ulong"),
            (typeof(long), "long"),
            (typeof(float), "float"),
            (typeof(double), "double"),
            (typeof(nint), "nint"),
            (typeof(nuint), "nuint"),
        };

        public void WriteLine(string str)
        {
            writer.WriteLine(str);
        }

        internal void Write(Expression body, Func<Delegate, string?> delegateToMethod, string indent)
        {
            var csharp = body.ToReadableString(settings =>
            {
                return settings.UseFullyQualifiedTypeNames
                .AddTranslatorFor(ExpressionType.Invoke, (expr, fallback) =>
                {
                    var invocation = (InvocationExpression)expr;
                    var deleg = (Delegate)((ConstantExpression)invocation.Expression).Value!;
                    var method = delegateToMethod(deleg);
                    if (method != null)
                        return $"{method}({string.Join(", ", invocation.Arguments.Select(x => x.ToReadableString(settings => settings.UseFullyQualifiedTypeNames)))})";
                    return fallback(expr);
                });
            }).Replace("DuckDbSharp.SerializationHelpers", nameof(SerializationHelpers));
            WriteIndented(csharp, indent);
        }


        private void WriteIndented(string csharp, string indent)
        {
            var lines = csharp.Replace("\r", string.Empty).Split('\n');
            foreach (var line in lines)
            {
                Write(indent);
                WriteLine(line);
            }
        }

        internal void WriteNamespace(string ns)
        {
            if (ns != currentNamespace)
            {
                if (currentNamespace != null)
                {
                    WriteLine("}");
                }
                currentNamespace = ns;
                Write("namespace ");
                Write(currentNamespace);
                WriteLine();
                WriteLine("{");
            }
        }

        public void Write(IEnumerable<ParameterExpression> parameters)
        {
            var any = false;
            foreach (var item in parameters)
            {
                if (any) Write(", ");
                any = true;
                WriteTypeReference(item.Type, false);
                Write(" ");
                Write(item.Name);
            }
        }

        public void WriteString(string str)
        {
            Write(Expression.Constant(str).ToReadableString());
        }

        private void Write(char c)
        {
            writer.Write(c);
        }
    }
}

