using DuckDbSharp.Reflection;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace DuckDbSharp.Example
{

    class Program
    {
        static void Main(string[] args)
        {
            using var db = ThreadSafeTypedDuckDbConnection.CreateInMemory();


            if (args.Contains("--generate-types"))
            {
                DuckDbUtils.GenerateCSharpTypes(new CodeGenerationOptions
                {
                    Connection = db,
                    TryReuseTypes = new[] { typeof(Employee) },
                    GenerateAotSerializers = true,
                    Specifications = new SerializerSpecification[]
                    {
                        new SerializerSpecification(new DirectoryInfo("../../../queries"))
                    },
                    DestinationPath = "../../../Queries.cs",
                    Namespace = "DuckDbSharp.Example",
                });
            }
            else
            {
                AotSerializers.RegisterAll(); // Only necessary for Native AOT (optional in JIT mode).

                foreach (var team in db.ExecuteQuery_teams_with_employees())
                {
                    Console.WriteLine($"Team {team.TeamId}, Members: [{string.Join(", ", team.Members.AsEnumerable())}]");
                }
                var employee = db.ExecuteQuery_GetEmployeeById(5).Single();
                Console.WriteLine($"Employee: {employee}");
            }

        }

    }


    public class Employee
    {
        public long EmployeeId;
        public string FirstName;
        public string LastName;

        public override string ToString() => $"#{EmployeeId} {FirstName} {LastName}";
    }
}