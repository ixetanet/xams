using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Pipeline.Stages.Shared;
using Xams.Core.Repositories;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeResultEntity : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        if (context.SystemParameters.ReturnEmpty)
        {
            return new Response<object?>()
            {
                Succeeded = true,
                Data = null
            };
        }

        object? id = context.Entity?.GetId() ?? null;

        if (id == null)
        {
            throw new Exception("Id is null");
        }
        
        // If the _system_return_entity parameter is set, return the entity as is,
        // this is used for creates, updates, deletes from a Job \ Action \ Service class
        // Returns the entity as a c# class versus a dynamic object
        if (context.SystemParameters.ReturnEntity)
        {
            Response<object?> readEntity = await context.DataRepository.Find(context.TableName, id, false);
            if (readEntity.Data == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Failed to find entity."
                };
            }
            return new Response<object?>()
            {
                Succeeded = true,
                Data = readEntity.Data
            };
        }
        
        Response<ReadOutput> readDynamicEntity = await context.DataRepository.Read(context.UserId, new ReadInput()
        {
            id = id,
            tableName = context.TableName,
            fields = ["*"]
        }, new ReadOptions()
        {
            BypassSecurity = true,
            NewDataContext = false, // Needs to be false because the data hasn't been committed yet
            Permissions = [$"TABLE_{context.TableName}_READ_SYSTEM"],
        });

        var results = context.Entity;
        if (readDynamicEntity.Data != null)
        {
            List<object> returnResults = 
                await AppendUIInfo.Set(context, readDynamicEntity.Data);
            returnResults = AppendOutputParameters.Set(context, returnResults);

            results = returnResults.FirstOrDefault();
        }

        return new Response<object?>()
        {
            Succeeded = true,
            Data = results
        };
    }

    
}