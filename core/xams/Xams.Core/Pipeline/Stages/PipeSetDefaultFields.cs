using System.Reflection;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeSetDefaultFields : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var response = await SetStringFields(context);
        if (!response.Succeeded)
        {
            return response;
        }

        SetDefaults(context);
        
        return await base.Execute(context);
    }

    private void SetDefaults(PipelineContext options)
    {
        if (options.DataOperation is not (DataOperation.Create or DataOperation.Update))
        {
            return;
        }
        
        if (options.DataOperation == DataOperation.Create)
        {
            // If there's no assignment, assign the record to the user
            var owningUserId = options.Entity.GetType().GetProperty("OwningUserId")?.GetValue(options.Entity);
            var owningTeamId = options.Entity.GetType().GetProperty("OwningTeamId")?.GetValue(options.Entity);
            if (owningUserId == null && owningTeamId == null)
            {
                options.Entity.GetType().GetProperty("OwningUserId")?.SetValue(options.Entity, options.UserId);
            }

            options.Entity.GetType().GetProperty("CreatedById")?.SetValue(options.Entity, options.UserId);
            options.Entity.GetType().GetProperty("CreatedDate")?.SetValue(options.Entity, DateTime.UtcNow);

            options.Entity.GetType().GetProperty("IsActive")?.SetValue(options.Entity, true);
        }
        
        if (options.DataOperation is DataOperation.Update)
        {
            // Ensure the user cannot change the CreatedBy\CreatedDate fields
            var createdById = options.PreEntity.GetType().GetProperty("CreatedById")?.GetValue(options.PreEntity);
            var createdDate = options.PreEntity.GetType().GetProperty("CreatedDate")?.GetValue(options.PreEntity);
            options.Entity.GetType().GetProperty("CreatedById")?.SetValue(options.Entity, createdById);
            options.Entity.GetType().GetProperty("CreatedDate")?.SetValue(options.Entity, createdDate);


            // If no owningUser or owningTeam is specified, use the existing record's values
            var owningUserId = options.Entity.GetType().GetProperty("OwningUserId")?.GetValue(options.Entity);
            var owningTeamId = options.Entity.GetType().GetProperty("OwningTeamId")?.GetValue(options.Entity);
            if (owningUserId == null && owningTeamId == null)
            {
                options.Entity.GetType().GetProperty("OwningUserId")?.SetValue(options.Entity,
                    options.PreEntity.GetType().GetProperty("OwningUserId")?.GetValue(options.PreEntity));
                options.Entity.GetType().GetProperty("OwningTeamId")?.SetValue(options.Entity,
                    options.PreEntity.GetType().GetProperty("OwningTeamId")?.GetValue(options.PreEntity));
            }
        }

        options.Entity.GetType().GetProperty("UpdatedById")?.SetValue(options.Entity, options.UserId);
        options.Entity.GetType().GetProperty("UpdatedDate")?.SetValue(options.Entity, DateTime.UtcNow);
    }

    private async Task<Response<object?>> SetStringFields(PipelineContext context)
    {
        if (context.DataOperation is not (DataOperation.Create or DataOperation.Update))
        {
            return new Response<object?>()
            {
                Succeeded = true
            };
        }
        var lookupInfos = context.Entity.EntityExt().GetUISetFieldFromLookupInfo();
        foreach (var lookupInfo in lookupInfos)
        {
            // TODO: Add lookup cache to the DataService
            // First check the cache, this may have been set by the DataService if this is a Bulk operation
            // if (context.LookupCache.TryGetValue(lookupInfo.Id, out object cacheLookupEntity))
            // {
            //     string lookupValue = EntityUtil.GetLookupNameValue(cacheLookupEntity);
            //     lookupInfo.Property.SetValue(context.Entity, lookupValue);
            //     continue;
            // }
            
            Type tableNameType = lookupInfo.LookupType;
            Type tableNameUnderlyingType = Nullable.GetUnderlyingType(tableNameType) ?? tableNameType;
            
            Response<object?> readOutput =
                await context.DataRepository.Find(tableNameUnderlyingType.Name, lookupInfo.Id, true);
            
            // If the results aren't in the database (hasn't been committed yet) check change tracking
            if (readOutput.Data == null)
            {
                var changeTracker = context.DataRepository.GetDbContext<IXamsDbContext>().ChangeTracker;
                var entityEntry = changeTracker.Entries()
                    .FirstOrDefault(x => x.Entity.GetType() == tableNameUnderlyingType
                                         && x.Property($"{tableNameUnderlyingType.Name}Id")
                                             .CurrentValue as Guid? == lookupInfo.Id);
            
                if (entityEntry == null)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage =
                            $"Could not find {tableNameUnderlyingType.Name} with ID {lookupInfo.Id} to set string field."
                    };
                }
                
                lookupInfo.Property.SetValue(context.Entity, EntityUtil.GetLookupNameValue(entityEntry.Entity));
                continue;
            }
            
            var lookupEntity = readOutput.Data;
            string lookupNameValue = EntityUtil.GetLookupNameValue(lookupEntity);
            lookupInfo.Property.SetValue(context.Entity, lookupNameValue);   
        }

        return new Response<object?>()
        {
            Succeeded = true
        };
    }
}