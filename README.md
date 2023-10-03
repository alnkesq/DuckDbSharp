# DuckDbSharp

DuckDbSharp is a bidirectional interoperability layer between [DuckDB](https://github.com/duckdb/duckdb) and .NET.

Features
- Support for **deeply nested** structures and lists
- Expose .NET methods/collections as **table UDFs**
- Execute DuckDB queries from .NET
- Generates **static types** from SQL (incl. field nullness detection)
- Dynamic results are supported as well (as dynamic assemblies/types)
- **Performance-oriented** with minimal allocations
- Support for both normal and `[Flags]` enums
- Native AOT support
- Pass .NET collections as SQL parameters (either as array or as table)
- Results are streamed as `IEnumerable<>`
- Write DuckDB loadable extensions in C# (work in progress)

Notes:
- This is **not** an ADO.NET (System.Data) provider
  - Rationale: ADO.NET is flat-table oriented (sublists/subfields are not supported, despite these being probably among the best features of DuckDB).
  - Additionally, ADO.NET is very unergonomic to use unless you add an ORM on top of it. Most existing ORMs however don't work well with sublists/subfields. This library deserializes/serializes directly on top of CLR POCO objects, and can generate (and keep up to date) the type definitions for a better IDE experience.

- Exporting methods as scalar functions is not supported, only as table functions (DuckDB doesn't currently provide C APIs for that)
- Until a [small patch](https://github.com/duckdb/duckdb/pull/8788) is upstreamed to DuckDB, you'll have to compile your own DuckDB binary if you want to pass enums from C# to DuckDB.

## Usage

### Calling DuckDB from .NET (auto-generated types)
```sql
-- my_query.sql
SELECT
    42 AS column1,
    [1, 2, 3] AS column2,
    [{a: 1, b: 2}] as column3
```

```csharp
foreach (var user in db.ExecuteQuery_my_query())
{
    // "user" has an auto-generated type with all the fields and sub-fields of the SQL query above.
}
```
See [detailed instructions](#getting-started) below.

### Calling DuckDB from .NET (inline sql)
```csharp
using var db = ThreadSafeTypedDuckDbConnection.CreateInMemory();
foreach (var user in db.ExecuteQuery<User>("select * from user"))
{
    
}
```

You can also use value tuples (`ExecuteQuery<(string A, int B)>("select 'a', 42")`), but keep in mind that only column order matters, since tuple member names are erased at runtime.

### Calling .NET from DuckDB
```csharp
[DuckDbFunction]
public static IEnumerable<User> GetUsers(string country) { /*...*/ }
```

```sql
SELECT * FROM GetUsers('US')
```


## Getting started
- Add a reference to `DuckDbSharp`
- Create a [queries](https://github.com/alnkesq/DuckDbSharp/tree/main/tests/DuckDbSharp.Example/queries) directory with the `.sql` queries you want to be able to call from .NET
   - File name must be: `ReturnType query_name(paramtype1, paramtype2).sql`
   - If `ReturnType` is not specified, a type will be automatically generated based on the SQL schema of the result.
   - If your query is parameterized, specify the types of the parameters (e.g. `string` or `long`). Otherwise, parens are unnecessary.
- Call `GenerateCSharpTypes` as shown in the [example](https://github.com/alnkesq/DuckDbSharp/blob/main/tests/DuckDbSharp.Example/Program.cs), and run.
- Start using the now generated extension methods of `TypedDuckDbConnectionBase` (one for each query).
- Remember to commit the generated files as well. This is very important in order to be able to recompile old versions of the repository.


## Benchmarks
Time to read ~100,000 rows of Northwind customers. In all 4 cases, the final result is a `List<Customer>`.
|                        Library |     Mean |   Error |  StdDev | Description
|------------------------------ |---------:|--------:|--------:|--------------
| **DuckDbSharp (this project)** | **145.4 ms** | 2.63 ms | 2.46 ms |SELECT * FROM customer
| DuckDB.NET + Dapper | 177.2 ms | 2.88 ms | 2.69 ms | SELECT * FROM customer
| Protobuf-net | **131.3 ms** | 2.52 ms | 2.81 ms | Deserialize from MemoryStream of protos
| Newtonsoft JSON | 241.7 ms | 2.93 ms | 2.60 ms | Deserialize from MemoryStream of JSON

Note: while protobuf-net is slightly faster, its use cases is very different (serialization/deserialization only, with no query support)

## Advanced features
### Customizing (de)serialization
- `[DuckDbInclude]` and `[DuckDbIgnore]` always take the precedence over other rules. In their absence, `[ProtoMember]` from protobuf-net is also taken into account. Otherwise, only public fields and properties are taken into account.
- Enums are serialized as DuckDb enums, you can override this with `[DuckDbSerializeAs(typeof(string))]` or `[DuckDbSerializeAs(typeof(int))]` (or whatever their underlying type is).
- `[Flags]` enums are always serialized as structs of booleans, one for each bit.
- `[DuckDbDefaultValueIsNullish]` can be applied to structs, and it means that `default(SomeStruct)` should be represented as `NULL` in DuckDB.
### Reading and writing parquets
- You can use `DuckDbUtils.QueryParquet<T>()` and `DuckDbUtils.WriteParquet<T>()` to directly read/write `.parquet` files (no database required).
