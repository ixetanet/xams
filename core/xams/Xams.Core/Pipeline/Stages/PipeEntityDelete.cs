using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeEntityDelete : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        // Check if the system should not perform post order traversal delete
        // If the parent pipeline is calling this pipeline, it should not perform post order traversal delete
        // because the parent pipeline will handle it
        if (!context.SystemParameters.NoPostOrderTraversalDelete)
        {
            var traversalResponse = await PostOrderTraversalDelete(context);
            if (!traversalResponse.Succeeded)
            {
                return traversalResponse;
            }
        }

        // If already being tracked, then remove and re-add to the change tracker
        // TODO: This should be optimized either by keeping track of entities in a Dictionary on the DbContext
        // TODO: Or another method (potentially disabling tracking and re-enabling https://learn.microsoft.com/en-us/ef/core/querying/tracking)
        string primaryKey = $"{context.TableName}Id";
        var entry = context.DataRepository.GetDbContext<BaseDbContext>().ChangeTracker
            .Entries().FirstOrDefault(e =>
                context.Entity != null && e.Entity.GetType().Name == context.TableName &&
                e.Entity.GetValue<Guid>(primaryKey) ==
                context.Entity.GetValue<Guid>(primaryKey));
        if (entry != null)
        {
            entry.State = EntityState.Detached;
        }

        var response = await context.DataRepository.Delete(context.Entity, context.SystemParameters.PreventSave);
        if (!response.Succeeded)
        {
            return response;
        }

        return await base.Execute(context);
    }

    private async Task<Response<object?>> PostOrderTraversalDelete(PipelineContext context)
    {
        Type entityType = Cache.Instance.GetTableMetadata(context.TableName).Type;

        if (context.Entity == null)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = $"Cannot delete entity with null entity."
            };
        }

        Guid entityId = context.Entity.GetValue<Guid>($"{entityType.Name}Id");

        if (entityId == Guid.Empty)
        {
            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = $"Cannot delete entity with empty ID."
            };
        }

        var dbContext = context.DataRepository.CreateNewDbContext<BaseDbContext>();
        var dependencies = DependencyFinder.GetDependencies(entityType, dbContext);
        var maxDepth = DependencyFinder.GetMaxDepth(dependencies);
        var postOrderTraversal =
            await DependencyFinder.GetPostOrderTraversal(dependencies, entityId, dbContext);

        // Sort by entities without Service Logic first
        postOrderTraversal = postOrderTraversal
            .OrderBy(x => Cache.Instance.GetTableMetadata(x.Value.Dependency.Type.Name).HasPostOpServiceLogic)
            .ToDictionary();

        // Keep track of entities that will be deleted to avoid duplicate deletes
        Dictionary<string, HashSet<Guid>> entityIds = new();

        // Delete from the highest depth to lowest
        while (maxDepth > -1)
        {
            // Create Pipeline Contexts and get Pre-Entities first
            Dictionary<Guid, PipelineContext> pipelineContexts = new();
            var records = postOrderTraversal
                .Where(x => x.Value.Depth == maxDepth).ToList();
            maxDepth--;
            if (!records.Any())
            {
                continue;
            }

            foreach (var recordDependency in records)
            {
                var id = recordDependency.Key;
                var dependency = recordDependency.Value.Dependency;
                var tableMetadata = Cache.Instance.GetTableMetadata(dependency.Type.Name);

                if (!entityIds.ContainsKey(dependency.Type.Name))
                {
                    entityIds.Add(dependency.Type.Name, new HashSet<Guid>());
                }

                if (entityIds[dependency.Type.Name].Contains(id))
                {
                    continue;
                }

                entityIds[dependency.Type.Name].Add(id);

                var fields = new Dictionary<string, dynamic>()
                {
                    { $"{dependency.Type.Name}Id", id }
                };

                // Prevent the save if there's no post operation service logic
                // Save when we reach an entity with post operating service logic or the end of the batch
                bool preventSave = !tableMetadata.HasPostOpServiceLogic;

                // If the reference is nullable, skip as this record will be updated
                if (dependency.IsNullable)
                {
                    continue;
                }

                var pipelineEntity = EntityUtil.ConvertToEntityId(dependency.Type, fields).Entity;

                var newPipelineContext = new PipelineContext()
                {
                    Parent = context, // TODO: Might want to make this the parent pipeline context that's causing the delete
                    TableName = dependency.Type.Name,
                    UserId = context.UserId,
                    Entity = pipelineEntity,
                    InputParameters = context.InputParameters,
                    OutputParameters = new Dictionary<string, JsonElement>(),
                    SystemParameters = new SystemParameters()
                    {
                        NoPostOrderTraversalDelete = true,
                        PreventSave = preventSave
                    },
                    DataOperation = DataOperation.Delete,
                    DataRepository = context.DataRepository,
                    SecurityRepository = context.SecurityRepository,
                    MetadataRepository = context.MetadataRepository,
                    DataService = context.DataService,
                };
                newPipelineContext.CreateServiceContext();
                pipelineContexts.Add(id, newPipelineContext);
            }

            // Get the Pre-Entities in Batches for Entities with Service Logic 
            // Don't check security for the dependent records
            // Not checking for security on cascade\dependent records is default behavior for delete in most CRM systems
            await context.DataService
                .BatchPreEntitySecurity(pipelineContexts
                    .Where(x => Cache.Instance.GetTableMetadata(x.Value.TableName).HasDeleteServiceLogic)
                    .Select(x => x.Value).ToList(), false);

            // Execute Deletes
            int toDeleteCount = 0;
            List<Guid> ids = new();
            foreach (var recordDependency in records)
            {
                var id = recordDependency.Key;
                var dependency = recordDependency.Value.Dependency;
                var tableMetadata = Cache.Instance.GetTableMetadata(dependency.Type.Name);
                ids.Add(id);

                // Prevent the save if there's no service logic
                // Save when we reach an entity with service logic or the end of the batch
                bool preventSave = false;
                if (!tableMetadata.HasPostOpServiceLogic)
                {
                    toDeleteCount++;
                    preventSave = true;
                }
                else if (toDeleteCount > 0)
                {
                    // Prevent Save is false, all pending deletes will be saved
                    toDeleteCount = 0;
                }

                // If the reference is nullable, update the record to null
                if (dependency.IsNullable)
                {
                    var entity = await dbContext.FindAsync(dependency.Type, id);
                    if (entity == null)
                    {
                        continue;
                    }

                    entity.SetValue(dependency.PropertyName, null);
                    await context.DataRepository.Update(entity, preventSave);
                    continue;
                }

                var newPipelineContext = pipelineContexts[id];
                var response = await Pipelines.Delete.Execute(newPipelineContext);
                if (!response.Succeeded)
                {
                    return response;
                }
            }

            if (toDeleteCount > 0)
            {
                await context.DataRepository.SaveChangesAsync();
            }
        }


        return new Response<object?>()
        {
            Succeeded = true
        };
    }
}