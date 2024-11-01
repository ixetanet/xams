using System.Dynamic;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages.Shared;

public static class AppendUIInfo
{
    public static async Task<List<object>> Set(PipelineContext context, ReadOutput readOutput)
    {
        string tableName = context.TableName;
        List<object> results = new List<object>();
        
        string permissionNames =
            $"TABLE_{tableName}_UPDATE_USER, TABLE_{tableName}_UPDATE_TEAM, TABLE_{tableName}_UPDATE_SYSTEM, TABLE_{tableName}_DELETE_USER, TABLE_{tableName}_DELETE_TEAM, TABLE_{tableName}_DELETE_SYSTEM, TABLE_{tableName}_ASSIGN_USER, TABLE_{tableName}_ASSIGN_TEAM, TABLE_{tableName}_ASSIGN_SYSTEM";
        string[] permissions = (await context.SecurityRepository.UserPermissions(context.UserId, permissionNames)).Data!;
        string[] deletePermissions =
            permissions.Where(x => x.StartsWith($"TABLE_{tableName}_DELETE_")).ToArray();
        string[] updatePermissions =
            permissions.Where(x => x.StartsWith($"TABLE_{tableName}_UPDATE_")).ToArray();
        string[] assignPermissions =
            permissions.Where(x => x.StartsWith($"TABLE_{tableName}_ASSIGN_")).ToArray();

        List<Guid> userTeamIds = new List<Guid>();
        foreach (var entity in readOutput.results)
        {
            if (!entity.HasField("OwningTeamId"))
            {
                continue;
            }
            
            Guid? owningTeamId = entity.GetValue<Guid?>("OwningTeamId");
            if (owningTeamId != null)
            {
                userTeamIds.Add(owningTeamId.Value);
            }
        }
        userTeamIds = userTeamIds.Distinct().ToList();

        
        // Get teamIds in batches of 500
        List<Guid> verifiedUserTeamIds = new List<Guid>();
        int batchCount = 500;
        while (userTeamIds.Count > 0)
        {
            var teamIds = (await context.SecurityRepository.UserTeams(context.UserId, userTeamIds.Take(batchCount).ToArray())).Data;
            userTeamIds.RemoveRange(0, Math.Min(batchCount, userTeamIds.Count));
            if (teamIds != null)
            {
                verifiedUserTeamIds.AddRange(teamIds);
            }
        }
        

        // Jobs canTrigger
        string[] triggerPermissions = new string[0];
        if (tableName == "Job")
        {
            string[] jobNames = Cache.Instance.ServiceJobs
                .Select(x => x.Value)
                .Select(x => $"JOB_{x.ServiceJobAttribute.Name}")
                .ToArray();
            triggerPermissions = (await context.SecurityRepository.UserPermissions(context.UserId, jobNames));
        }

        // Set canDelete and canUpdate
        foreach (var entity in readOutput.results)
        {
            dynamic expandoObject;
            // This might already be an ExpandoObject if there was a join
            if (entity is ExpandoObject)
            {
                expandoObject = entity;
            }
            else
            {
                expandoObject = new ExpandoObject();
            }

            IDictionary<string, object?> expandoDictionary = ((IDictionary<string, object?>)expandoObject);
            foreach (var property in entity.GetType().GetEntityProperties())
            {
                ((IDictionary<string, object?>)expandoObject)[property.Name] = property.GetValue(entity);
            }

            Permissions.PermissionLevel? updatePermissionLevel = Permissions.GetHighestPermission(updatePermissions);
            Permissions.PermissionLevel? deletePermissionLevel = Permissions.GetHighestPermission(deletePermissions);
            Permissions.PermissionLevel? assignPermissionLevel = Permissions.GetHighestPermission(assignPermissions);

            bool canUpdate = false;
            bool canDelete = false;
            bool canAssign = false;
            bool canTrigger = false;
            if (updatePermissionLevel == Permissions.PermissionLevel.System)
            {
                canUpdate = true;
            }
            else if (updatePermissionLevel == Permissions.PermissionLevel.Team)
            {
                if (expandoDictionary.ContainsKey("OwningTeamId") && expandoObject.OwningTeamId != null)
                {
                    canUpdate = verifiedUserTeamIds.Contains(expandoObject.OwningTeamId);
                }

                if (expandoDictionary.ContainsKey("OwningUserId") && expandoObject.OwningUserId != null &&
                    !canUpdate)
                {
                    canUpdate = expandoObject.OwningUserId == context.UserId;
                }
            }
            else if (updatePermissionLevel == Permissions.PermissionLevel.User)
            {
                if (expandoDictionary.ContainsKey("OwningUserId") && expandoObject.OwningUserId != null)
                {
                    canUpdate = expandoObject.OwningUserId == context.UserId;
                }
            }

            if (deletePermissionLevel == Permissions.PermissionLevel.System)
            {
                canDelete = true;
            }
            else if (deletePermissionLevel == Permissions.PermissionLevel.Team)
            {
                if (expandoDictionary.ContainsKey("OwningTeamId") && expandoObject.OwningTeamId != null)
                {
                    canDelete = verifiedUserTeamIds.Contains(expandoObject.OwningTeamId);
                }

                if (expandoDictionary.ContainsKey("OwningUserId") && expandoObject.OwningUserId != null &&
                    !canDelete)
                {
                    canDelete = expandoObject.OwningUserId == context.UserId;
                }
            }
            else if (deletePermissionLevel == Permissions.PermissionLevel.User)
            {
                if (expandoDictionary.ContainsKey("OwningUserId") && expandoObject.OwningUserId != null)
                {
                    canDelete = expandoObject.OwningUserId == context.UserId;
                }
            }

            if (canUpdate && assignPermissionLevel != null)
            {
                canAssign = true;
            }
            
            if (tableName == "Job")
            {
                if (expandoDictionary.ContainsKey("Name") && !string.IsNullOrEmpty(expandoObject.Name))
                {
                    canTrigger = triggerPermissions.Contains($"JOB_{expandoObject.Name}");
                }
            
                expandoObject._ui_info_ = new
                {
                    canDelete, canUpdate, canAssign, canTrigger, tableName
                };
            
                results.Add(expandoObject);
            }
            else
            {
                expandoObject._ui_info_ = new
                {
                    canDelete, canUpdate, canAssign, tableName
                };

                results.Add(expandoObject);
            }
        }

        return results;
    }
}