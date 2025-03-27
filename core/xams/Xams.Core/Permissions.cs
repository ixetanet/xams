using System.Text.RegularExpressions;

namespace Xams.Core;

public static class Permissions
{
    public enum PermissionLevel
    {
        System,
        Team,
        User
    }

    public static PermissionLevel? GetHighestPermission(string[]? permissions)
    {
        if (permissions == null)
        {
            return null;
        }

        var system = permissions.FirstOrDefault(x => x.EndsWith("_SYSTEM"));
        if (system != null)
        {
            return PermissionLevel.System;
        }

        var team = permissions.FirstOrDefault(x => x.EndsWith("_TEAM"));
        if (team != null)
        {
            return PermissionLevel.Team;
        }

        var user = permissions.FirstOrDefault(x => x.EndsWith("_USER"));
        if (user != null)
        {
            return PermissionLevel.User;
        }

        return null;
    }

    public static async Task<string[]> GetUserTablePermissions(Guid userId,
        string tableName,
        string[] operations)
    {
        List<string> tablePermissions = new();
        foreach (var operation in operations)
        {
            tablePermissions.Add($"TABLE_{tableName}_{operation}_USER");
            tablePermissions.Add($"TABLE_{tableName}_{operation}_TEAM");
            tablePermissions.Add($"TABLE_{tableName}_{operation}_SYSTEM");
        }
            
        string[] permissions = await PermissionCache.GetUserPermissions(userId, tablePermissions.ToArray());
        return permissions;
    }

    public static async Task<string[]> GetUserTablePermissions(Guid userId,
        string[] tableNames, string operation)
    {
        List<string> tablePermission = new();
        foreach (var tableName in tableNames)
        {
            tablePermission.AddRange([
                $"TABLE_{tableName}_{operation}_USER",
                $"TABLE_{tableName}_{operation}_TEAM",
                $"TABLE_{tableName}_{operation}_SYSTEM"
            ]);
        }

        string[] permissions = await PermissionCache.GetUserPermissions(userId, tablePermission.ToArray());
        return permissions;
    }
        
    internal static List<TablePermission> ToTablePermissions(this string[] permissions)
    {
        List<TablePermission> tablePermissions = new();
        Regex crudRegex = new Regex("TABLE_([A-Za-z0-9_]+)_(UPDATE|READ|DELETE|CREATE|ASSIGN)_(USER|TEAM|SYSTEM)");
        Regex importExportRegex = new Regex("TABLE_([A-Za-z0-9_]+)_(IMPORT|EXPORT)");
        foreach (var permission in permissions)
        {
            var match = crudRegex.Match(permission);
            if (!match.Success)
            {
                match = importExportRegex.Match(permission);
            }

            if (!match.Success)
            {
                continue;
            }

            string tableName = match.Groups[1].Value;
            TablePermission? tablePermission = tablePermissions.FirstOrDefault(x => x.Table == tableName);
            if (tablePermission == null)
            {
                tablePermission = new TablePermission()
                {
                    Table = tableName,
                    Permissions = [permission]
                };
                tablePermissions.Add(tablePermission);
            }
            else
            {
                tablePermission.Permissions.Add(permission);
            }
        }
        return tablePermissions;
    }
}

public class TablePermission
{
    public required string Table { get; set; }
    public required List<string> Permissions { get; set; }
}
