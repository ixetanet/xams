using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Pipeline.Stages.Shared;
using Xams.Core.Repositories;

namespace Xams.Core.Pipeline.Stages;

public class PipeEntityRead : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        var permissions = await Permissions.GetUserTablePermissions(context.UserId, context.TableName, ["READ"]);
        // If the teamsView parameter is set to true then only return records
        // the user can view due to their team ownership
        if (context.InputParameters.ContainsKey("teamsView") &&
            context.InputParameters["teamsView"].GetBoolean())
        {
            for (int i = 0; i < permissions.Length; i++)
            {
                if (permissions[i] == $"TABLE_{context.ReadInput?.tableName}_READ_SYSTEM")
                {
                    permissions[i] = $"TABLE_{context.ReadInput?.tableName}_READ_TEAM";
                }
            }
        }
        
        var readResponse = await context.DataRepository.Read(context.UserId, context.ReadInput, new ReadOptions()
        {
            Permissions = permissions
        });
        
        if (readResponse.Succeeded)
        {
            context.Entities = readResponse.Data.results;
            context.ReadOutput = readResponse.Data;
            List<object> results =
                await AppendUIInfo.Set(context, context.ReadOutput);
            results = AppendOutputParameters.Set(context, results);
            context.ReadOutput.results = results;
            context.ServiceContext.ReadOutput = context.ReadOutput;
        }
        else
        {
            return new Response<object?>()
            {
                Succeeded = readResponse.Succeeded,
                FriendlyMessage = readResponse.FriendlyMessage,
                LogMessage = readResponse.LogMessage,
            };
        }
        return await base.Execute(context);
    }
}