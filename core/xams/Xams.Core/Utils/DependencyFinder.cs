using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Base;

namespace Xams.Core.Utils;

public class DependencyFinder
{
    public static List<Dependency> GetDependencies(Type targetType, DbContext dbContext, int depth = 0, Dependency? parent = null)
    {
        var result = new List<Dependency>();
        var allEntities = Cache.Instance.TableMetadata.Select(x => x.Value.Type).ToList();

        foreach (var entityType in allEntities)
        {
            var properties = entityType.GetProperties();
            foreach (var property in properties)
            {
                var isForeignKey = dbContext.Model.FindEntityType(entityType)!
                    .GetForeignKeys().Any(fk => fk.PrincipalEntityType.ClrType == targetType && fk.Properties.Any(p => p.Name == property.Name));
                

                if (isForeignKey)
                {
                    var dependency = new Dependency
                    {
                        Depth = depth,
                        Type = entityType,
                        PropertyName = property.Name,
                        IsNullable = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>),
                        Dependencies = new List<Dependency>(), // Initialized for potential nested dependencies
                        Parent = parent
                    };

                    // If the property is not nullable or not a Guid, continue to traverse
                    if (dependency.IsNullable && property.PropertyType == typeof(Guid?))
                    {
                        dependency.Dependencies = null; // End traversal here, as cascading will not occur
                    }
                    else
                    {
                        // Recursively find nested dependencies
                        dependency.Dependencies = GetDependencies(entityType, dbContext, depth + 1, dependency);
                    }

                    result.Add(dependency);
                }
            }
        }

        return result;
    }
    
    public static int GetMaxDepth(List<Dependency> dependencies)
    {
        int maxDepth = 0;
        foreach (var dependency in dependencies)
        {
            if (dependency.Depth > maxDepth)
            {
                maxDepth = dependency.Depth;
            }
            if (dependency.Dependencies != null)
            {
                int childDepth = GetMaxDepth(dependency.Dependencies);
                if (childDepth > maxDepth)
                {
                    maxDepth = childDepth;
                }
            }
        }

        return maxDepth;
    }
    
    public static async Task<Dictionary<Guid, RecordInfo>> GetPostOrderTraversal(List<Dependency> dependencies, Guid id, BaseDbContext dbContext, int depth = 0, Dictionary<Guid, RecordInfo>? recordInfoDict = null)
    {
        if (recordInfoDict == null)
        {
            recordInfoDict = new Dictionary<Guid, RecordInfo>();
        }
        foreach (var dependency in dependencies)
        {
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(dbContext, dependency.Type);
            IQueryable query = dynamicLinq.Query
                .Where($"{dependency.PropertyName} == @0", id)
                .Select($"new({dependency.Type.Name}Id)");
            List<dynamic> queryResults = await query.ToDynamicListAsync();
        
            foreach (dynamic queryResult in queryResults)
            {
                Type resultType = queryResult.GetType();
                Guid resultId = (Guid)resultType.GetProperty($"{dependency.Type.Name}Id")!.GetValue(queryResult);

                if (!recordInfoDict.ContainsKey(resultId))
                {
                    recordInfoDict[resultId] = new RecordInfo()
                    {
                        Dependency = dependency,
                        Count = 1,
                        Depth = depth,
                    };
                }
                else
                {
                    recordInfoDict[resultId].Count++;
                }
                
                if (dependency.Dependencies != null && dependency.Dependencies.Any())
                {
                    await GetPostOrderTraversal(dependency.Dependencies, resultId, dbContext, depth + 1, recordInfoDict);    
                }
            }
        }

        return recordInfoDict.OrderByDescending(x => x.Value.Depth)
            .ToDictionary(x => x.Key, x => x.Value);
    }
    
    public static void LogDependencyTree(List<Dependency> dependencies, int depth = 0)
    {
        foreach (var dependency in dependencies)
        {
            Console.WriteLine($"{new string(' ', depth * 2)}{dependency.Type.Name} IsNullable:{dependency.IsNullable}");
            if (dependency.Dependencies != null)
            {
                LogDependencyTree(dependency.Dependencies, depth + 1);    
            }
            
        }
    }

    public class RecordInfo
    {
        public Dependency Dependency { get; set; } = null!;
        public int Depth { get; set; }
        public int Count { get; set; }
    }
}

public class Dependency
{
    public int Depth { get; set; }
    public Type Type { get; set; } = null!;
    public string PropertyName { get; set; } = null!;
    public bool IsNullable { get; set; }
    public Dependency? Parent { get; set; }
    public List<Dependency>? Dependencies { get; set; }
}