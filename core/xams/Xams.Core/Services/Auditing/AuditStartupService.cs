using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Entities;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[ServiceStartup(StartupOperation.Pre, Int32.MinValue)]
public class AuditStartupService : IServiceStartup
{
    public static readonly string AuditRetentionSetting = "AUDIT_HISTORY_RETENTION_DAYS";
    internal const string AuditRefreshSystemRecord = "AuditLastRefresh";

    // Serializes refreshes so an older configuration cannot replace a newer one
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public async Task<Response<object?>> Execute(StartupContext startupContext)
    {
        var db = startupContext.DataService.GetDbContext<IXamsDbContext>();
        var auditEnabled = db.GetAuditEnabled();
        db.SetAuditEnabled(false);
        try
        {
            Console.WriteLine("Creating Audit Data");
            await SyncAuditRecords(db);

            var response = await CacheAuditRecords(db);
            if (!response.Succeeded)
            {
                return response;
            }

            await GetAuditSettings(startupContext);
        }
        finally
        {
            db.SetAuditEnabled(auditEnabled);
        }

        return ServiceResult.Success();
    }

    /// <summary>
    /// Brings the Audit and AuditField records in line with the model: merges duplicates (keeping any enabled
    /// flag), removes records for tables and fields that no longer exist, and adds missing ones.
    /// </summary>
    internal static async Task SyncAuditRecords(IXamsDbContext db)
    {
        var audits = await db.AuditsBase.AsNoTracking().OrderBy(x => x.AuditId).ToListAsync();
        var auditFields = await db.AuditFieldsBase.AsNoTracking().OrderBy(x => x.AuditFieldId).ToListAsync();

        var auditableFields = new Dictionary<string, HashSet<string>>();
        HashSet<string> AuditableFields(string tableName)
        {
            if (!auditableFields.TryGetValue(tableName, out var fields))
            {
                fields = GetAuditableFields(db, tableName);
                auditableFields[tableName] = fields;
            }

            return fields;
        }

        var removedAudits = new HashSet<Audit>();
        var changedAudits = new HashSet<Audit>();
        var removedFields = new HashSet<AuditField>();
        var changedFields = new HashSet<AuditField>();

        // One Audit per table: merge duplicates into the first
        var auditsByTable = new Dictionary<string, Audit>();
        foreach (var audit in audits)
        {
            if (string.IsNullOrEmpty(audit.Name) || !Cache.Instance.TableMetadata.ContainsKey(audit.Name))
            {
                removedAudits.Add(audit);
                continue;
            }

            if (!auditsByTable.TryGetValue(audit.Name, out var kept))
            {
                auditsByTable[audit.Name] = audit;
                continue;
            }

            kept.IsCreate |= audit.IsCreate;
            kept.IsUpdate |= audit.IsUpdate;
            kept.IsDelete |= audit.IsDelete;
            changedAudits.Add(kept);
            removedAudits.Add(audit);
            foreach (var auditField in auditFields.Where(x => x.AuditId == audit.AuditId))
            {
                auditField.AuditId = kept.AuditId;
                changedFields.Add(auditField);
            }
        }

        // One AuditField per auditable property: merge duplicates, remove the rest
        var tableByAuditId = auditsByTable.ToDictionary(x => x.Value.AuditId, x => x.Key);
        var fieldsByKey = new Dictionary<(Guid, string), AuditField>();
        foreach (var auditField in auditFields)
        {
            if (!tableByAuditId.TryGetValue(auditField.AuditId, out var tableName) ||
                string.IsNullOrEmpty(auditField.Name) ||
                !AuditableFields(tableName).Contains(auditField.Name))
            {
                removedFields.Add(auditField);
                continue;
            }

            if (!fieldsByKey.TryGetValue((auditField.AuditId, auditField.Name), out var kept))
            {
                fieldsByKey[(auditField.AuditId, auditField.Name)] = auditField;
                continue;
            }

            kept.IsCreate |= auditField.IsCreate;
            kept.IsUpdate |= auditField.IsUpdate;
            kept.IsDelete |= auditField.IsDelete;
            changedFields.Add(kept);
            removedFields.Add(auditField);
        }

        foreach (var auditField in removedFields)
        {
            db.Remove(auditField);
        }

        foreach (var auditField in changedFields.Except(removedFields))
        {
            db.Update(auditField);
        }

        await db.SaveChangesAsync();

        foreach (var audit in removedAudits)
        {
            db.Remove(audit);
        }

        foreach (var audit in changedAudits.Except(removedAudits))
        {
            db.Update(audit);
        }

        // Add records for new tables and fields
        foreach (var tableName in Cache.Instance.TableMetadata.Keys)
        {
            if (!auditsByTable.TryGetValue(tableName, out var audit))
            {
                audit = new Audit { AuditId = Guid.NewGuid(), Name = tableName, IsTable = true };
                auditsByTable[tableName] = audit;
                db.Add(audit);
            }

            foreach (var fieldName in AuditableFields(tableName))
            {
                if (!fieldsByKey.ContainsKey((audit.AuditId, fieldName)))
                {
                    db.Add(new AuditField { AuditFieldId = Guid.NewGuid(), AuditId = audit.AuditId, Name = fieldName });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// The mapped scalar properties of a table. Navigation properties have no audit values.
    /// </summary>
    private static HashSet<string> GetAuditableFields(IXamsDbContext db, string tableName)
    {
        var entityType = db.Model.FindEntityType(Cache.Instance.GetTableMetadata(tableName).Type);
        return entityType?.GetProperties()
            .Where(x => x.PropertyInfo != null)
            .Select(x => x.Name)
            .ToHashSet() ?? [];
    }

    /// <summary>
    /// Reloads the audit configuration if the AuditLastRefresh marker changed since the last load. The new
    /// configuration replaces the cached one in a single step.
    /// </summary>
    public static async Task<Response<object?>> CacheAuditRecords(IXamsDbContext db)
    {
        await RefreshLock.WaitAsync();
        try
        {
            // Read the marker before the configuration, so a change committed during the load is picked up
            // by the next refresh
            var refreshTime = await GetRefreshTime(db);
            if (refreshTime == Cache.Instance.AuditRefreshTime)
            {
                return ServiceResult.Success();
            }

            Cache.Instance.SetTableAuditInfo(await LoadAuditInfo(db));
            Cache.Instance.AuditRefreshTime = refreshTime;
            return ServiceResult.Success();
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private static async Task<DateTime> GetRefreshTime(IXamsDbContext db)
    {
        var systemRecord = await db.SystemsBase.AsNoTracking()
            .Where(x => x.Name == AuditRefreshSystemRecord)
            .OrderBy(x => x.SystemId)
            .FirstOrDefaultAsync();
        if (systemRecord == null)
        {
            var value = DateTime.UtcNow.ToString("O");
            await Queries.UpdateSystemRecord(db, AuditRefreshSystemRecord, value);
            return DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        // An unreadable marker still loads once; any later change writes a readable one
        return DateTime.TryParse(systemRecord.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
            out var refreshTime)
            ? refreshTime
            : DateTime.MaxValue;
    }

    /// <summary>
    /// Builds the audit configuration by table. Duplicate Audit or AuditField records are merged and an enabled
    /// flag on any of them wins.
    /// </summary>
    internal static async Task<IReadOnlyDictionary<string, Cache.AuditInfo>> LoadAuditInfo(IXamsDbContext db)
    {
        var audits = await db.AuditsBase.AsNoTracking().ToListAsync();
        var auditFields = (await db.AuditFieldsBase.AsNoTracking().ToListAsync()).ToLookup(x => x.AuditId);

        var tableAuditInfo = new Dictionary<string, Cache.AuditInfo>();
        foreach (var audit in audits.Where(x => !string.IsNullOrEmpty(x.Name)))
        {
            if (!tableAuditInfo.TryGetValue(audit.Name!, out var auditInfo))
            {
                auditInfo = new Cache.AuditInfo();
                tableAuditInfo[audit.Name!] = auditInfo;
            }

            auditInfo.IsCreateAuditEnabled |= audit.IsCreate;
            auditInfo.IsUpdateAuditEnabled |= audit.IsUpdate;
            auditInfo.IsDeleteAuditEnabled |= audit.IsDelete;

            foreach (var auditField in auditFields[audit.AuditId].Where(x => !string.IsNullOrEmpty(x.Name)))
            {
                if (!auditInfo.FieldAuditInfos.TryGetValue(auditField.Name!, out var fieldAuditInfo))
                {
                    fieldAuditInfo = new Cache.FieldAuditInfo();
                    auditInfo.FieldAuditInfos[auditField.Name!] = fieldAuditInfo;
                }

                fieldAuditInfo.IsCreateAuditEnabled |= auditField.IsCreate;
                fieldAuditInfo.IsUpdateAuditEnabled |= auditField.IsUpdate;
                fieldAuditInfo.IsDeleteAuditEnabled |= auditField.IsDelete;
            }
        }

        return tableAuditInfo;
    }

    public async Task GetAuditSettings(StartupContext context)
    {
        var db = context.DataService.GetDbContext<IXamsDbContext>();
        await Queries.GetCreateSetting(db, AuditRetentionSetting, "30");
    }
}
