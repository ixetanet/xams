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
        Guid entityId = context.Entity.GetValue<Guid>(primaryKey);
        var entry = context.DataRepository.GetDbContext<BaseDbContext>().ChangeTracker
            .Entries().FirstOrDefault(e =>
                context.Entity != null && e.Entity.GetType().Name == context.TableName &&
                e.Entity.GetValue<Guid>(primaryKey) == entityId);
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
        // Dictionary<string, HashSet<Guid>> uniqueRecords = new();

        // Delete from the highest depth to lowest
        while (maxDepth > -1)
        {
            // Create Pipeline Contexts and get Pre-Entities first
            Dictionary<Guid, PipelineDependency> pipelineDependencies = new();
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
                    pipelineDependencies.Add(id, new PipelineDependency()
                    {
                        Dependency = dependency,
                        PipelineContext = new PipelineContext()
                    });
                    continue;
                }
                
                // If this record is already being deleted, skip
                if (!context.DataService.TrackDelete(dependency.Type.Name, id))
                {
                    continue;
                }
                
                // If a pipelineDependency was previously created as IsNullable (to set nullable reference to null), remove it
                // as instead of updating the reference, we will delete the record
                if (pipelineDependencies.ContainsKey(id))
                {
                    pipelineDependencies.Remove(id);
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
                pipelineDependencies.Add(id, new PipelineDependency()
                {
                    PipelineContext = newPipelineContext,
                    Dependency = dependency
                });
            }

            // Get the Pre-Entities in Batches for Entities with Service Logic 
            // Don't check security for the dependent records
            // Not checking for security on cascade\dependent records is default behavior for delete in most CRM systems
            await context.DataService
                .BatchPreEntitySecurity(pipelineDependencies
                    .Where(x => !string.IsNullOrEmpty(x.Value.PipelineContext.TableName))
                    .Where(x => Cache.Instance.GetTableMetadata(x.Value.PipelineContext.TableName).HasDeleteServiceLogic)
                    .Select(x => x.Value.PipelineContext).ToList(), false);

            // Execute Deletes
            int toDeleteCount = 0;
            foreach (var pipelineDependency in pipelineDependencies)
            {
                var id = pipelineDependency.Key;
                var dependency = pipelineDependency.Value.Dependency;
                var tableMetadata = Cache.Instance.GetTableMetadata(dependency.Type.Name);

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
                // and If we plan to delete the entity, don't update the reference
                if (dependency.IsNullable && !context.DataService.TrackingDelete(dependency.Type.Name, id))
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
                
                var response = await Pipelines.Delete.Execute(pipelineDependency.Value.PipelineContext);
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

    public class PipelineDependency
    {
        public required PipelineContext PipelineContext { get; set; } 
        public required Dependency Dependency { get; set; }
    }
    
}