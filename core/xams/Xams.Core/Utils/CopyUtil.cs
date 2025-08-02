using System.Linq.Dynamic.Core;
using System.Reflection;
using Xams.Core.Base;

namespace Xams.Core.Utils;

public static class CopyUtil
{
    /// <summary>
    /// Deep copy an entity
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="entityType"></param>
    /// <param name="entityId"></param>
    /// <param name="excludedEntities">Which entities should not be copied and maintain their original relationship</param>
    /// <returns></returns>
    public static async Task<dynamic> DeepCopy(IXamsDbContext dbContext, Type entityType, Guid entityId,
        string[] excludedEntities)
    {
        var dependencies = DependencyFinder.GetDependencies(entityType, dbContext);
        var maxDepth = DependencyFinder.GetMaxDepth(dependencies);

        var traversalSettings = new DependencyFinder.PostOrderTraversalSettings()
        {
            Id = entityId,
            Dependencies = dependencies,
            DbContextFactory = () => dbContext,
            ReturnEntity = true
        };

        var postOrderTraversal = (await DependencyFinder.GetPostOrderTraversal(traversalSettings))
            .OrderBy(x => x.Value.Depth)
            .ToDictionary();

        Dictionary<string, EntityInfo> copiedEntities = new();

        var primaryMetadata = entityType.Metadata();
        DynamicLinq dLinq = new DynamicLinq(dbContext, entityType);
        IQueryable query = dLinq.Query;
        query = query.Where($"{primaryMetadata.PrimaryKey} == @0", entityId);
        var results = await query.ToDynamicListAsync();
        var primaryEntity = results.FirstOrDefault();
        if (primaryEntity == null)
        {
            throw new Exception("Entity not found");
        }

        var primaryId = Guid.NewGuid();
        copiedEntities[$"{primaryMetadata.TableName}_{entityId}"] = new EntityInfo
        {
            Id = primaryId,
            Entity = primaryEntity
        };
        primaryMetadata.PrimaryKeyProperty.SetValue(primaryEntity, primaryId);
        
        dbContext.ChangeTracker.Clear();
        dbContext.Add(primaryEntity);

        var depth = 0;
        while (depth <= maxDepth)
        {
            var records = postOrderTraversal
                .Where(x => x.Value.Depth == depth)
                .ToList();
            depth++;
            if (!records.Any())
            {
                continue;
            }

            foreach (var recordDependency in records)
            {
                var id = recordDependency.Key;
                var entity = recordDependency.Value.Entity;
                var type = entity.GetType();
                var metadata = (Cache.MetadataInfo)Cache.Instance.TableTypeMetadata[type];
                if (excludedEntities.Contains(metadata.TableName))
                {
                    continue;
                }

                foreach (var dependency in recordDependency.Value.Dependencies)
                {
                    var tableMetadata = dependency.Type.Metadata();
                    
                    // Change the id of this entity
                    var newId = Guid.NewGuid();
                    tableMetadata.PrimaryKeyProperty.SetValue(entity, newId);
                    var entKey = $"{tableMetadata.TableName}_{id}";
                    if (!copiedEntities.TryGetValue(entKey, out _))
                    {
                        copiedEntities[entKey] = new EntityInfo()
                        {
                            Id = id,
                            Entity = entity
                        };
                    }
                    else
                    {
                        continue;
                    }
                    
                    // Update all the relational fields
                    PropertyInfo[] properties = entity.GetType().GetProperties();
                    foreach (PropertyInfo property in properties)
                    {
                        if (!property.Name.EndsWith("Id"))
                        {
                            continue;
                        }
                        
                        var relPropName = property.Name.Substring(0, property.Name.Length - 2);
                        var relProp = properties.FirstOrDefault(x => x.Name == relPropName);
                        if (relProp == null)
                        {
                            continue;
                        }

                        var relId = property.GetValue(entity);
                        if (relId == null)
                        {
                            continue;
                        }
                        
                        var relPropMetadata = relProp.PropertyType.Metadata();

                        if (excludedEntities.Contains(relPropMetadata.TableName))
                        {
                            continue;
                        }

                        var relKey = $"{relPropMetadata.TableName}_{relId.ToString()}";
                        if (copiedEntities.TryGetValue(relKey, out var value))
                        {
                            property.SetValue(entity, value.Id);
                            relProp.SetValue(entity, value.Entity);
                        }
                    }
                }
                dbContext.Add(entity);
            }
        }

        return primaryEntity;
    }
    
    public class EntityInfo
    {
        public required object Id { get; set; }
        public required object Entity { get; set; }
    }
}