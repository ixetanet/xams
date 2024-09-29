using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

/// <summary>
/// Service Permissions return a list of permissions that should exist in the system.
/// If the permissions do not exist, the system will create them, and the permission
/// existed prior but no longer does, the system will remove it.
/// </summary>
public interface IServicePermission
{
    public Task<Response<List<string>>> Execute(PermissionContext permissionContext);
}