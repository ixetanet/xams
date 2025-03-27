using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
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

        if (context.Entity == null)
        {
            return new Response<object?>()
            {
                Succeeded = true
            };
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

        object entityId = context.Entity.GetValue<object>(Cache.Instance.GetTableMetadata(context.TableName).PrimaryKey);

        var dbContext = context.DataRepository.CreateNewDbContext<BaseDbContext>();
        var dependencies = DependencyFinder.GetDependencies(entityType, dbContext);
        var maxDepth = DependencyFinder.GetMaxDepth(dependencies);

        var traversalSettings = new DependencyFinder.PostOrderTraversalSettings()
        {
            Id = entityId,
            Dependencies = dependencies,
            // Use a DbContext in this transaction to ensure the latest records are retrieved
            DbContextFactory = () => context.DataRepository.GetDbContext<BaseDbContext>()
        };
        
        var postOrderTraversal = (await DependencyFinder.GetPostOrderTraversal(traversalSettings))
            .OrderByDescending(x => x.Value.Depth)
            .ToDictionary();
        
        
        // Sort by entities without Service Logic first
        postOrderTraversal = postOrderTraversal
            .OrderBy(x => Cache.Instance.GetTableMetadata(x.Value.Dependency.Type.Name).HasPostOpServiceLogic)
            .ToDictionary();
        
        
        // Delete from the highest depth to lowest
        while (maxDepth > -1)
        {
            // Create Pipeline Contexts and get Pre-Entities first
            Dictionary<object, PipelineDependency> pipelineDependencies = new();
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

                // var pipelineEntity = EntityUtil.ConvertToEntityId(dependency.Type, fields).Entity;
                var pipelineEntity = EntityUtil.ConvertToEntityId(dependency.Type, new Input
                {
                    id = id
                }).Entity;

                var newPipelineContext = new PipelineContext()
                {
                    Parent = context, // TODO: Might want to make this the parent pipeline context that's causing the delete
                    TableName = EntityUtil.GetTableName(dependency.Type, EntityUtil.DbContext?.GetType() ?? throw new Exception("DbContext not yet initialized")).TableName,
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
                .BatchPreEntity(pipelineDependencies
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