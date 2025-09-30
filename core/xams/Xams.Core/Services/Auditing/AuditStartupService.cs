using System.Linq.Dynamic.Core;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

[ServiceStartup(StartupOperation.Post)]
public class AuditStartupService : IServiceStartup
{
    public static readonly string AuditRetentionSetting = "AUDIT_HISTORY_RETENTION_DAYS";

    public async Task<Response<object?>> Execute(StartupContext startupContext)
    {
        var response = await CreateDeleteAuditRecords(startupContext);
        if (!response.Succeeded)
        {
            return response;
        }

        response = await CacheAuditRecords(startupContext.DataService.GetDbContext<IXamsDbContext>());
        if (!response.Succeeded)
        {
            return response;
        }

        await GetAuditSettings(startupContext);

        return ServiceResult.Success();
    }

    private async Task<Response<object?>> CreateDeleteAuditRecords(StartupContext context)
    {
        try
        {
            Console.WriteLine($"Creating Audit Data");
            var db = context.DataService.GetDbContext<IXamsDbContext>();
            db.SetAuditEnabled(false);

            // Query for all the audit and audit field records
            var auditMetadata = Cache.Instance.GetTableMetadata("Audit");
            var auditLinq = new DynamicLinq(db, auditMetadata.Type);
            var auditQuery = auditLinq.Query;
            var audits = (await auditQuery.ToDynamicListAsync()).ToList<object>();

            var auditFieldMetadata = Cache.Instance.GetTableMetadata("AuditField");
            var auditFieldLinq = new DynamicLinq(db, auditFieldMetadata.Type);
            var auditFieldQuery = auditFieldLinq.Query;
            var auditFields = (await auditFieldQuery.ToDynamicListAsync()).ToList<object>();

            // Normalize the data for faster performance
            var lookup = auditFields.Select(x => new Lookup()
            {
                AuditId = x.GetValue<Guid>("AuditId"),
                TableName = audits.First(y => y.GetValue<Guid>("AuditId") == x.GetValue<Guid>("AuditId"))
                    .GetValue<string>("Name"),
                FieldName = x.GetValue<string>("Name"),
                Entity = x
            }).ToList();

            // If the table no longer exists - delete audit records
            List<object> removeAudits = new List<object>();
            foreach (var audit in audits)
            {
                if (!Cache.Instance.TableMetadata.ContainsKey(audit.GetValue<string>("Name")))
                {
                    removeAudits.Add(audit);
                    db.Remove(audit);
                }
            }

            // Delete child AuditField records from deleted tables
            foreach (var entity in removeAudits)
            {
                // Get all the audit fields for the entity
                var removeAuditFields = auditFields
                    .Where(a => a.GetValue<Guid>("AuditId") == entity.GetValue<Guid>("AuditId"));
                db.RemoveRange(removeAuditFields);

                // Remove the audit record from lookup
                lookup.RemoveAll(x => x.AuditId == entity.GetValue<Guid>("AuditId"));
            }
            
            // Delete specific fields that have been removed
            foreach (var item in lookup)
            {
                var type = Cache.Instance.GetTableMetadata(item.TableName).Type;
                var property = type.GetProperty(item.FieldName); 
                // Exclude navigation properties
                if (property == null || type.GetProperty($"{item.FieldName}Id") != null)
                {
                    db.Remove(item.Entity);
                }
            }

            // Audit records to add
            List<NewAudit> newAudits = new List<NewAudit>();
            foreach (var kvp in Cache.Instance.TableMetadata)
            {
                // If the audit record already exists, skip
                var audit = audits.FirstOrDefault(x => x.GetValue<string>("Name") == kvp.Key); 
                if (audit != null)
                {
                    continue;
                }

                var newAudit = new Dictionary<string, dynamic?>();
                newAudit["Name"] = kvp.Key;
                newAudit["IsTable"] = true;
                var entity = EntityUtil.DictionaryToEntity(auditMetadata.Type, newAudit);
                newAudits.Add(new NewAudit()
                {
                    TableName = kvp.Key,
                    Entity = entity
                });
                db.Add(entity);
            }

            // Save to create audit records and get ids
            await db.SaveChangesAsync();

            audits = (await auditQuery.ToDynamicListAsync()).ToList<object>();
            // Create new fields for all new audits
            foreach (var audit in newAudits)
            {
                var entityProperties = Cache.Instance.GetTableMetadata(audit.TableName)
                    .Type.GetEntityProperties();
                foreach (var entityProperty in entityProperties)
                {
                    if (!entityProperty.IsPrimitive() && 
                        entityProperties.Any(x => x.Name == $"{entityProperty.Name}Id"))
                    {
                        // Skip navigation properties
                        continue;
                    }
                    var auditField = new Dictionary<string, dynamic?>();
                    auditField["AuditId"] = audit.Entity.GetValue<Guid>("AuditId");
                    auditField["Name"] = entityProperty.Name;
                    var entity = EntityUtil.DictionaryToEntity(auditFieldMetadata.Type, auditField);
                    lookup.Add(new Lookup()
                    {
                        AuditId = auditField["AuditId"],
                        TableName = audit.TableName,
                        FieldName = entityProperty.Name,
                        Entity = entity
                    });
                    db.Add(entity);
                }
            }

            // Create any missing audit fields
            foreach (var kvp in Cache.Instance.TableMetadata)
            {
                foreach (var entityProperty in kvp.Value.Type.GetEntityProperties())
                {
                    var auditField = lookup
                        .FirstOrDefault(x => x.TableName == kvp.Key && x.FieldName == entityProperty.Name);
                    if (auditField != null)
                    {
                        continue;
                    }

                    var newAuditField = new Dictionary<string, dynamic?>();
                    newAuditField["AuditId"] = audits.First(x => x.GetValue<string>("Name") == kvp.Key).GetValue<Guid>("AuditId");
                    newAuditField["Name"] = entityProperty.Name;
                    var entity = EntityUtil.DictionaryToEntity(auditFieldMetadata.Type, newAuditField);
                    db.Add(entity);
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return ServiceResult.Success();
    }

    public static async Task<Response<object?>> CacheAuditRecords(IXamsDbContext db)
    {
        // Load last refresh time
        bool refreshCache = false;
        var systemType = Cache.Instance.GetTableMetadata("System").Type;
        var dynamicLinq = new DynamicLinq(db, systemType);
        var query = dynamicLinq.Query.Where("Name == @0", "AuditLastRefresh");
        var systemResults = await query.ToDynamicListAsync();
        if (!systemResults.Any())
        {
            var system = new Dictionary<string, dynamic?>()
            {
                ["Name"] = "AuditLastRefresh",
                ["Value"] = DateTime.UtcNow.ToString("O")
            };
            var entity = EntityUtil.DictionaryToEntity(systemType, system);
            db.Add(entity);
            await db.SaveChangesAsync();
            Cache.Instance.AuditRefreshTime = DateTime.Parse(entity.GetValue<string>("Value"));
            refreshCache = true;
        }
        else
        {
            // Only refresh the Cache if there has been changes to the Audit \ AuditField tables
            var lastRefreshDate = DateTime.Parse(((object)systemResults.First()).GetValue<string>("Value"));
            if (lastRefreshDate != Cache.Instance.AuditRefreshTime)
            {
                Cache.Instance.AuditRefreshTime = lastRefreshDate;
                refreshCache = true;
            }
        }

        if (!refreshCache)
        {
            return ServiceResult.Success();
        }

        Cache.Instance.TableAuditInfo.Clear();
        // Load Audit Info
        var auditType = Cache.Instance.GetTableMetadata("Audit").Type;
        var auditFieldType = Cache.Instance.GetTableMetadata("AuditField").Type;
        var audits = (await DynamicLinq.FindAll(db, auditType))
            .Select(x => (object)x).ToList();
        var auditFields = (await DynamicLinq.FindAll(db, auditFieldType))
            .Select(x => (object)x).ToList();

        foreach (var audit in audits)
        {
            var tableName = audit.GetValue<string>("Name");
            var id = audit.GetId();
            // Check if the ID is a Guid
            if (!(id is Guid guidId))
            {
                // If not a Guid, we need to handle this case
                // For now, we'll skip this audit record
                continue;
            }
            var auditFieldsForAudit = auditFields
                .Where(x => x.GetValue<Guid>("AuditId") == (Guid)id).ToList();

            var auditInfo = new Cache.AuditInfo()
            {
                IsCreateAuditEnabled = audit.GetValue<bool>("IsCreate"),
                IsReadAuditEnabled = audit.GetValue<bool>("IsRead"),
                IsUpdateAuditEnabled = audit.GetValue<bool>("IsUpdate"),
                IsDeleteAuditEnabled = audit.GetValue<bool>("IsDelete")
            };

            foreach (var auditField in auditFieldsForAudit)
            {
                string? fieldName = auditField.GetValue<string?>("Name");
                if (string.IsNullOrEmpty(fieldName))
                {
                    continue;
                }

                auditInfo.FieldAuditInfos.Add(fieldName, new Cache.FieldAuditInfo()
                {
                    IsCreateAuditEnabled = auditField.GetValue<bool>("IsCreate"),
                    IsUpdateAuditEnabled = auditField.GetValue<bool>("IsUpdate"),
                    IsDeleteAuditEnabled = auditField.GetValue<bool>("IsDelete")
                });
            }

            Cache.Instance.TableAuditInfo.TryAdd(tableName, auditInfo);
        }

        return ServiceResult.Success();
    }

    public async Task GetAuditSettings(StartupContext context)
    {
        var db = context.DataService.GetDbContext<IXamsDbContext>();
        await Queries.GetCreateSetting(db, AuditRetentionSetting, "30");
    }

    public class NewAudit
    {
        public required string TableName { get; set; }
        public required object Entity { get; set; }
    }

    public class Lookup
    {
        public Guid AuditId { get; set; }
        public required string TableName { get; set; }
        public required string FieldName { get; set; }
        public required object Entity { get; set; }
    }
}
