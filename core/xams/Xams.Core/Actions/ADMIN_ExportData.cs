using System.Linq.Dynamic.Core;
using System.Text;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Actions
{
    [ServiceAction("ADMIN_ExportData")]
    public class ADMIN_ExportData : IServiceAction
    {
        public async Task<Response<object?>> Execute(ActionServiceContext context)
        {
            var json = context.Parameters["tables"].GetRawText();
            var tables = JsonSerializer.Deserialize<DependencyInfo[]>(json);
            var sortedTables = TopologicalSort(tables);

            var dataContext = context.DataRepository.GetDbContext<IXamsDbContext>();
            List<TableExport> tableExports = new List<TableExport>();
            foreach (string table in sortedTables)
            {
                var dbContextType = Cache.Instance.GetTableMetadata(table);
                DynamicLinq dynamicLinq = new DynamicLinq(dataContext, dbContextType.Type);
            
                string strFields = EntityUtil.GetEntityFields(dbContextType.Type, null, string.Empty, EntityUtil.FieldModifications.NoRelatedFields);
            
                IQueryable query = dynamicLinq.Query.Select($"new({strFields})");
            
                var data = await query.ToDynamicListAsync();
            
                tableExports.Add(new TableExport()
                {
                    tableName = table,
                    data = data
                });
            }

            string serializedJson = JsonSerializer.Serialize(tableExports);

            var byteArray = Encoding.UTF8.GetBytes(serializedJson);
            var stream = new MemoryStream(byteArray);
            return new Response<object?>()
            {
                Succeeded = true,
                Data = new FileData()
                {
                    FileName = $"export_{DateTime.UtcNow}.json",
                    Stream = stream,
                    ContentType = "application/octet-stream"
                },
                ResponseType = ResponseType.File
            };
        }

        public class TableExport
        {
            public string tableName { get; set; }
            public dynamic data { get; set; }
        }

        public class DependencyInfo
        {
            public string name { get; set; }
            public DependentTable[] dependencies { get; set; }
        
        }

        public class DependentTable
        {
            public string name { get; set; }
        }
    
        public static List<string> TopologicalSort(DependencyInfo[] tables)
        {
            Dictionary<string, List<string>> adjacencyList = new Dictionary<string, List<string>>();
            foreach (var table in tables)
            {
                if (!adjacencyList.ContainsKey(table.name))
                    adjacencyList[table.name] = new List<string>();
                
                if (table.dependencies != null)
                {
                    foreach (var dep in table.dependencies)
                    {
                        if (!adjacencyList.ContainsKey(dep.name))
                            adjacencyList[dep.name] = new List<string>();

                        adjacencyList[dep.name].Add(table.name);
                    }
                }
            }

            HashSet<string> visited = new HashSet<string>();
            Stack<string> stack = new Stack<string>();

            foreach (var node in adjacencyList.Keys)
            {
                if (!visited.Contains(node))
                    Visit(node, adjacencyList, visited, stack);
            }

            return stack.ToList();
        }

        private static void Visit(string node, Dictionary<string, List<string>> adjacencyList, HashSet<string> visited, Stack<string> stack)
        {
            visited.Add(node);

            foreach (var neighbor in adjacencyList[node])
            {
                if (!visited.Contains(neighbor))
                    Visit(neighbor, adjacencyList, visited, stack);
            }

            stack.Push(node);
        }
    }
}