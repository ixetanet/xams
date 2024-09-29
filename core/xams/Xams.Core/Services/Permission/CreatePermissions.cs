using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Permission;

[ServicePermission]
public class CreatePermissions : IServicePermission
{
    public Task<Response<List<string>>> Execute(PermissionContext permissionContext)
    {
        // Create Job Permissions
        List<string> permissionNames = new();
        foreach (var serviceJob in Cache.Instance.ServiceJobs)
        {
            var serviceJobInfo = serviceJob.Value;
            permissionNames.Add($"JOB_{serviceJobInfo.ServiceJobAttribute.Name}");
        }
        return Task.FromResult(ServiceResult.Success(permissionNames));
    }
}