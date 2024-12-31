using DuckDbSharp.Bindings;
using DuckDbSharp.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DuckDbSharp.Functions
{
    public unsafe static class FunctionUtils
    {
        private static (Type FinalElementType, Func<object, object> FinalTransformer, object? Result) TryGetTransformer(Type methodReturnType, object? result, bool mandatory)
        {

            var elementType = TryGetElementType(methodReturnType);
            Type finalElementType;
            Func<object, object> transformer;

            if (elementType != null)
            {
                if (elementType == typeof(object))
                {
                    if (result == null && !mandatory) return default;
                    result = TypeSniffedEnumerable.Create((IEnumerable)result!, out elementType);
                    if (elementType == typeof(object))
                    {
                        return (typeof(Box<int>), x => Array.Empty<Box<int>>(), result);
                    }
                }


                if (IsStructure(elementType))
                {

                    // IEnumerable<Something>
                    finalElementType = elementType;
                    transformer = ret => ret;
                }
                else
                {
                    // IEnumerable<long>
                    var boxType = typeof(Box<>).MakeGenericType(elementType);
                    finalElementType = boxType;
                    var boxMany = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.BoxMany)).MakeGenericMethod(elementType);
                    transformer = ret =>
                    {
                        var baseItems = ret;
                        return boxMany.Invoke(null, new object[] { baseItems });
                    };
                }
            }
            else
            {
                if (methodReturnType == typeof(void)) // Void method
                {
                    finalElementType = typeof(Box<int>);
                    transformer = ret =>
                    {
                        return Array.Empty<Box<int>>();
                    };
                }
                else if (IsStructure(methodReturnType)) // Scalar function returning a single struct.
                {
                    finalElementType = methodReturnType;
                    transformer = ret => new[] { ret };
                }
                else // Scalar function returning a single primitive type.
                {
                    finalElementType = typeof(Box<>).MakeGenericType(methodReturnType);
                    var valueField = finalElementType.GetField(nameof(Box<object>.Value), BindingFlags.Public | BindingFlags.Instance);
                    transformer = ret =>
                    {
                        var singleton = ret;
                        var box = Activator.CreateInstance(finalElementType);
                        valueField.SetValue(box, singleton);
                        return new[] { box };
                    };
                }

            }
            return (finalElementType, transformer, result);

        }

        internal static FunctionInfo RegisterTableFunction(_duckdb_connection* conn, string name, Delegate deleg)
        {
            return RegisterTableFunctionCore(conn, deleg.Method, name, deleg.Target);
        }
        internal static FunctionInfo RegisterTableFunction(_duckdb_connection* conn, MethodInfo method)
        {
            return RegisterTableFunctionCore(conn, method, GetRegistrationNameForMethod(method), null);
        }
        internal static FunctionInfo RegisterScalarFunction(_duckdb_connection* conn, string name, Delegate deleg)
        {
            return RegisterScalarFunctionCore(conn, deleg.Method, name, deleg.Target);
        }
        internal static FunctionInfo RegisterScalarFunction(_duckdb_connection* conn, MethodInfo method)
        {
            return RegisterScalarFunctionCore(conn, method, GetRegistrationNameForMethod(method), null);
        }
        internal static string GetRegistrationNameForMethod(MethodInfo m) => DuckDbUtils.ToDuckCaseFunction(m.Name);
        private unsafe static FunctionInfo RegisterScalarFunctionCore(_duckdb_connection* conn, MethodInfo method, string name, object? delegateTarget)
        {
            using var scalarFunctionName = (ScopedString)name;
            var parameters = method.GetParameters();
            if (parameters.Length == 0) throw new NotSupportedException("Scalar functions with zero arguments are not supported.");

            Func<object[], object> baseInvoke = args => method.Invoke(delegateTarget, args);
            var funcInfo = new FunctionInfo
            {
                Name = name,
                Method = method,
                DelegateTarget = delegateTarget,
                Parameters = parameters,
            };

            var clrReturnType = method.ReturnType;

            if (clrReturnType == typeof(object) || clrReturnType == typeof(void)) throw new ArgumentException("Scalar function must return a non-void, non-System.Object type.");
            
            var nonNullClrReturnType = Nullable.GetUnderlyingType(clrReturnType) ?? clrReturnType;
            

            var func = Methods.duckdb_create_scalar_function();
            Methods.duckdb_scalar_function_set_name(func, scalarFunctionName);
            Methods.duckdb_scalar_function_set_return_type(func, DuckDbTypeCreator.CreateLogicalType(nonNullClrReturnType, null));



            
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var lt = DuckDbTypeCreator.CreateLogicalType(param.ParameterType, null);
                Methods.duckdb_scalar_function_add_parameter(func, lt);
                //var structuralType = DuckDbStructuralType.CreateStructuralType(lt);
                //colTypes[i] = DuckDbTypeCreator.CreateGetter(new FieldInfo2("Arg" + i, ))
            }
            var argChunkClrType = typeof(ValueTuple).Assembly.GetType("System.ValueTuple`" + parameters.Length)?.MakeGenericType(parameters.Select(x => x.ParameterType).ToArray());
            if (argChunkClrType == null) throw new Exception("Could not find a System.ValueTuple with the required number of type parameters.");
            funcInfo.ScalarArgumentChunkType = DuckDbStructuralType.CreateStructuralType(argChunkClrType);
            funcInfo.ScalarArgumentDeserializer = SerializerCreationContext.Global.CreateRootDeserializer(argChunkClrType, funcInfo.ScalarArgumentChunkType);

            //funcInfo.ScalarArgumentDeserializer = DuckDbStructuralType.CreateStructuralType()
            Methods.duckdb_scalar_function_set_extra_info(func, CreateGcHandle(funcInfo), &BindingUtils.FreeGcHandle);
            Methods.duckdb_scalar_function_set_function(func, &ExecuteScalarFunction);
            
            BindingUtils.CheckState(Methods.duckdb_register_scalar_function(conn, func));
            funcInfo.PointerScalarFn = func;
            return funcInfo;
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void ExecuteScalarFunction(duckdb_function_info_ptr p, _duckdb_data_chunk* chunk, _duckdb_vector* vector)
        {
            object[]? argsArray = null;
            t_scalarFunctionRecursionLevel++;
            try
            {
                var funcInfo = BindingUtils.ReadGcHandle<FunctionInfo>(Methods.duckdb_scalar_function_get_extra_info(p));
                var itemCount = checked((int)Methods.duckdb_data_chunk_get_size(chunk));
                var colcount = checked((int)Methods.duckdb_data_chunk_get_column_count(chunk));

                var argumentTuples = funcInfo.ScalarArgumentDeserializer((nint)chunk, new DuckDbDeserializationContext());
                var returnClrType = funcInfo.Method.ReturnType;


                var returnSerializer = SerializerCreationContext.Global.CreateRootVectorSerializer(returnClrType);
                var returnVector = Array.CreateInstance(returnClrType, itemCount);

                argsArray = new object[colcount];
                for (int rowIdx = 0; rowIdx < itemCount; rowIdx++)
                {
                    for (int argIdx = 0; argIdx < colcount; argIdx++)
                    {
                        argsArray[argIdx] = ((ITuple)argumentTuples.GetValue(rowIdx))[argIdx];
                    }

                    var result = funcInfo.Method.Invoke(funcInfo.DelegateTarget, argsArray);
                    returnVector.SetValue(result, rowIdx);
                }


                // 
                // HACK: Scalar functions don't have a duckdb_init_info, so how do we dispose the arena after DuckDB finishes reading our data?
                // For now, let's use reusable thread-locals.
                // HACK: we also key by destination vector pointer, otherwise if we call two string functions for the same row, we overwrite the data for the first invocation.
                t_scalarFunctionArenaForRecursionLevel ??= new Dictionary<nint, NativeArenaSlim>[64];
                ref Dictionary<nint, NativeArenaSlim> arenaDict = ref t_scalarFunctionArenaForRecursionLevel[t_scalarFunctionRecursionLevel - 1];
                arenaDict ??= new();
                if (!arenaDict.TryGetValue((nint)vector, out var arena))
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    arena = new();
#pragma warning restore CA2000 // Dispose objects before losing scope
                    arenaDict.Add((nint)vector, arena);
                }
                
                arena.Reset();
                returnSerializer(returnVector, (nint)vector, arena);

            }
            catch (Exception ex)
            {
                using var error = (ScopedString)(ex.ToString() + (argsArray != null ? " - Provided arguments: " + string.Join(", ", argsArray.Select(x =>
                {
                    if (x == null) return "NULL";
                    var str = x.ToString() ?? string.Empty;
                    if (str.Length > 20) return string.Concat(str.AsSpan(0, 20), "...");
                    return str;
                })) : null));
                Methods.duckdb_scalar_function_set_error(p, error);
            }
            finally
            {
                t_scalarFunctionRecursionLevel--;
            }

        }

        internal static void FreeScalarFunctionArenasForCurrentThreadAndRecursionLevel()
        {
            if (t_scalarFunctionArenaForRecursionLevel == null) return;
            var dict = t_scalarFunctionArenaForRecursionLevel[t_scalarFunctionRecursionLevel];
            if (dict == null) return;
            foreach (var item in dict.Values)
            {
                item.Dispose();
            }
            dict.Clear();
        }
        [ThreadStatic]
        private static int t_scalarFunctionRecursionLevel;
        [ThreadStatic]
        private static Dictionary<nint, NativeArenaSlim>?[]? t_scalarFunctionArenaForRecursionLevel;

        private static FunctionInfo RegisterTableFunctionCore(_duckdb_connection* conn, MethodInfo method, string name, object? delegateTarget)
        {
            using var tableFunctionName = (ScopedString)name;
            var parameters = method.GetParameters();

            Func<object[], object> baseInvoke = args => method.Invoke(delegateTarget, args);
            var funcInfo = new FunctionInfo
            {
                Name = name,
                Method = method,
                DelegateTarget = delegateTarget,
                Parameters = parameters,
            };

            if (method.ReturnType != typeof(object))
            {
                var transformer = TryGetTransformer(method.ReturnType, null, mandatory: false);
                if (transformer != default)
                {
                    funcInfo.FinalElementType = transformer.FinalElementType;
                    funcInfo.Transformer = transformer.FinalTransformer;
                }
            }


            var func = Methods.duckdb_create_table_function();
            Methods.duckdb_table_function_set_name(func, tableFunctionName);
            foreach (var param in parameters)
            {
                var lt = DuckDbTypeCreator.CreateLogicalType(param.ParameterType, null);
                Methods.duckdb_table_function_add_parameter(func, lt);
            }
            Methods.duckdb_table_function_set_init(func, &TableFunctionInit);
            Methods.duckdb_table_function_set_bind(func, &BindFunctionInit);
            Methods.duckdb_table_function_set_local_init(func, &TableLocalInit);
            Methods.duckdb_table_function_set_extra_info(func, CreateGcHandle(funcInfo), &BindingUtils.FreeGcHandle);
            Methods.duckdb_table_function_set_function(func, &EnumerateFunction);
            BindingUtils.CheckState(Methods.duckdb_register_table_function(conn, func));
            funcInfo.PointerTableFn = func;
            return funcInfo;
        }
        private static Type? TryGetElementType(Type ienumerable)
        {
            if (ienumerable == typeof(string) || ienumerable == typeof(byte[])) return null;
            Type? enu = ienumerable;
            if (!IsExactlyIEnumerable(enu))
                enu = enu.GetInterfaces().SingleOrDefault(x => IsExactlyIEnumerable(x));

            return enu?.GetGenericArguments().Single();
        }

        public static bool IsStructure(Type t)
        {
            DuckDbTypeCreator.GetDuckDbType(t, null, out _, out _, out _, out var structFields, out _);
            return structFields != null;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope")]
        static void TableFunctionInit(duckdb_init_info_ptr p)
        {

            var ptr = CreateGcHandle(new FunctionInitContext
            {

            });
            Methods.duckdb_init_set_init_data(p, ptr, &BindingUtils.FreeGcHandle);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void TableLocalInit(duckdb_init_info_ptr p)
        {
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void BindFunctionInit(duckdb_bind_info_ptr p)
        {
            try
            {
                var funcinfo = BindingUtils.ReadGcHandle<FunctionInfo>(Methods.duckdb_bind_get_extra_info(p));
                var paramCount = (int)Methods.duckdb_bind_get_parameter_count(p);
                var args = new object[funcinfo.Parameters.Length];
                for (var paramId = 0; paramId < paramCount; paramId++)
                {
                    var param = Methods.duckdb_bind_get_parameter(p, (ulong)paramId);
                    var clrParam = funcinfo.Parameters[paramId];
                    object? val;
                    var paramType = clrParam.ParameterType;
                    if (paramType == typeof(string))
                    {
                        val = BindingUtils.ToString(Methods.duckdb_get_varchar(param));
                    }
                    else if (paramType == typeof(int))
                    {
                        var num = Methods.duckdb_get_int64(param);
                        val = Convert.ChangeType(num, paramType);
                    }
                    else if (paramType == typeof(long))
                    {
                        val = Methods.duckdb_get_int64(param);
                    }
                    else throw new NotSupportedException();
                    args[(int)paramId] = val;
                }
                var bindctx = new BindContext
                {
                    Function = funcinfo,
                    Args = args,
                };

                if (funcinfo.FinalElementType != null)
                {
                    bindctx.FinalElementType = funcinfo.FinalElementType;
                    BindType(p, funcinfo.FinalElementType);
                }
                else
                {
                    var ret = funcinfo.Method.Invoke(funcinfo.DelegateTarget, args);
                    var retType = ret?.GetType() ?? typeof(string);
                    var transformer = TryGetTransformer(retType, ret, mandatory: true);
                    ret = transformer.Result;
                    BindType(p, transformer.FinalElementType);
                    bindctx.FinalElementType = transformer.FinalElementType;
                    bindctx.HasPrecomputedReturnValue = true;
                    bindctx.PrecomputedReturnValue = ret;
                    bindctx.LateTransformer = transformer.FinalTransformer;
                }


                Methods.duckdb_bind_set_bind_data(p, CreateGcHandle(bindctx), &BindingUtils.FreeGcHandle);
            }
            catch (Exception ex)
            {
                using var error = (ScopedString)ex.ToString();
                Methods.duckdb_bind_set_error(p, error);
            }
        }



        private static bool IsExactlyIEnumerable(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>);

        private static void* CreateGcHandle<T>(T obj)
        {
            var handle = GCHandle.Alloc(obj);
            return (void*)GCHandle.ToIntPtr(handle);
        }

        private static void BindType(duckdb_bind_info_ptr p, Type type)
        {
            foreach (var field in DuckDbTypeCreator.GetFields(type))
            {
                var numType = DuckDbTypeCreator.CreateLogicalType(field.FieldType, null);
                using var colName = (ScopedString)field.Name;
                Methods.duckdb_bind_add_result_column(p, colName, numType);
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static void EnumerateFunction(duckdb_function_info_ptr p, _duckdb_data_chunk* chunk)
        {
            try
            {
                var funcInfo = BindingUtils.ReadGcHandle<FunctionInfo>(Methods.duckdb_function_get_extra_info(p));

                var bind = BindingUtils.ReadGcHandle<BindContext>(Methods.duckdb_function_get_bind_data(p));
                var initCtx = BindingUtils.ReadGcHandle<FunctionInitContext>(Methods.duckdb_function_get_init_data(p));

                initCtx.Arena ??= new();
                initCtx.Arena.Reset();

                if (initCtx.Enumerator == null)
                {
                    object ret;
                    if (bind.HasPrecomputedReturnValue)
                    {
                        ret = bind.LateTransformer(bind.PrecomputedReturnValue);
                    }
                    else
                    {
                        ret = funcInfo.Transformer(funcInfo.Method.Invoke(funcInfo.DelegateTarget, bind.Args));
                    }

                    if (ret is Array)
                    {
                        if(ret.GetType() == typeof(object[]))
                            initCtx.Enumerator = ((IEnumerable)(EnumerateObjectArrayAsGenericMethod.MakeGenericMethod(bind.FinalElementType).Invoke(null, new object[] { ret })!)).GetEnumerator();
                        else
                            initCtx.Enumerator = ((IEnumerable)(EnumerateArrayGenericMethod.MakeGenericMethod(ret.GetType().GetElementType()).Invoke(null, new object[] { ret })!)).GetEnumerator();
                    }
                    else initCtx.Enumerator = ((IEnumerable)ret).GetEnumerator();
                }

                if (initCtx.RootSerializer == null)
                {
                    lock (SerializerCreationContext.Global)
                    {
                        initCtx.RootSerializer = SerializerCreationContext.Global.CreateRootSerializer(bind.FinalElementType);
                    }
                }

                //var extra = ReadGcHandle<FunctionInfo>(Methods.duckdb_function_get_init_data(p));


                var itemCount = initCtx.RootSerializer(initCtx.Enumerator, (nint)chunk, initCtx.Arena);


                Methods.duckdb_data_chunk_set_size(chunk, (ulong)itemCount);

            }
            catch (Exception ex)
            {
                using var error = (ScopedString)ex.ToString();
                Methods.duckdb_function_set_error(p, error);
            }

        }

        private readonly static MethodInfo EnumerateArrayGenericMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.EnumerateArrayGeneric), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        private readonly static MethodInfo EnumerateObjectArrayAsGenericMethod = typeof(SerializationHelpers).GetMethod(nameof(SerializationHelpers.EnumerateObjectArrayAsGeneric), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        internal static List<FunctionInfo> RegisterFunctions(_duckdb_connection* conn, Type type)
        {
            var registered = new List<FunctionInfo>();
            RegisterFunctions(conn, type, registered);
            return registered;
        }
        internal static List<FunctionInfo> RegisterFunctions(_duckdb_connection* conn, Assembly assembly)
        {
            var registered = new List<FunctionInfo>();
            Type[] types = GetTypesBestEffort(assembly);
            foreach (var type in types)
            {
                RegisterFunctions(conn, type, registered);
            }
            return registered;
        }

        public static Type[] GetTypesBestEffort(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
                Console.Error.WriteLine("Warning: some types could not be loaded: " + ex);
            }

            return types;
        }

        private static void RegisterFunctions(_duckdb_connection* conn, Type type, List<FunctionInfo> output)
        {
            //if (!type.Name.Contains("DuckDbFunctions")) return;
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                //Console.WriteLine(method);
                //Console.WriteLine(  attribute);
                //var a = method.GetCustomAttributes().FirstOrDefault();
                //if (method.GetCustomAttribute(attribute) != null)
                // var attr = (DuckDbFunctionAttribute?)method.GetCustomAttributes().Where(x => x.GetType().FullName == "DuckDbSharp.DuckDbFunctionAttribute").FirstOrDefault(x => x != null);
                var attr = (DuckDbFunctionAttribute?)method.GetCustomAttributes().OfType<DuckDbFunctionAttribute>().FirstOrDefault(x => x != null);

                if (attr != null)
                {
                    var isScalar = attr.IsScalar ?? TypeSniffedEnumerable.TryGetEnumerableElementType(method.ReturnType) == null;
                    
                    output.Add(isScalar ? RegisterScalarFunction(conn, method) : RegisterTableFunction(conn, method));
                }
            }
        }
    }


    public delegate int RootSerializer(IEnumerator enumerator, nint chunk, NativeArenaSlim arena);
    public delegate void RootVectorSerializer(Array array, nint vector, NativeArenaSlim arena);
    public delegate Array RootDeserializer(nint chunk, DuckDbDeserializationContext deserializationContext);
}

