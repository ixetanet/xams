using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Jobs;
using Xams.Core.Services.Auditing;
using Xams.Core.Startup;
using Xams.Core.Utils;
using static Xams.Core.Attributes.DataOperation;
using static Xams.Core.Attributes.LogicStage;

namespace Xams.Core.Services.Logic;

[ServiceLogic("Setting", Update | Create | Delete, PreOperation)]
public class SettingService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        string? settingName;
        if (context.DataOperation is Create)
        {
            settingName = context.GetEntity<object>().GetValue<string?>("Name");
        }
        else
        {
            settingName = context.GetPreEntity<object>().GetValue<string?>("Name");
        }
        
        var db = context.GetDbContext<BaseDbContext>();
        string[] integerSettings =
        [
            JobStartupService.SettingName, AuditStartupService.AuditRetentionSetting,
        ];
        string[] boolSettings =
        [
            AuditStartupService.AuditEnabledSetting, SystemRecords.CachePermissionsSetting,
        ];
        
        if (context.DataOperation is Create or Update)
        {
            if (string.IsNullOrEmpty(settingName))
            {
                return ServiceResult.Error($"Setting name is required.");
            }

            string? settingValue = context.GetEntity<object>().GetValue<string?>("Value");
            if (integerSettings.Contains(settingName) || boolSettings.Contains(settingName))
            {
                if (context.ValueChanged("Name"))
                {
                    return ServiceResult.Error($"Cannot change {settingName} setting name.");
                }
            }
            
            if (integerSettings.Contains(settingName))
            {
                if (!int.TryParse(settingValue, out _))
                {
                    return ServiceResult.Error($"Setting value must be an integer.");    
                }
            }

            if (boolSettings.Contains(settingName))
            {
                if (!bool.TryParse(settingValue, out _))
                {
                    return ServiceResult.Error($"Setting value must be a boolean.");    
                }
            }

            if (AuditStartupService.AuditEnabledSetting == settingName)
            {
                // Update the audit cache refresh setting to force refresh of cache
                await Queries.UpdateSystemRecord(db, "AuditLastRefresh", DateTime.UtcNow.ToString("O"));
            }

            if (SystemRecords.CachePermissionsSetting == settingName)
            {
                if (bool.TryParse(settingValue, out bool isEnabled) && !isEnabled)
                {
                    // Clear any cached permissions
                    Permissions.CachedPermissions.Clear();
                    Permissions.CacheLastUpdate = string.Empty;
                }
            }
        }

        if (context.DataOperation is Delete)
        {
            if (integerSettings.Contains(settingName) || boolSettings.Contains(settingName))
            {
                return ServiceResult.Error($"Cannot delete {settingName} setting.");
            }
        }

        return ServiceResult.Success();
    }
}