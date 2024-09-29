using System.ComponentModel.DataAnnotations;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// This Creates the AuditHistory records for the AuditHistory table
/// </summary>
[BulkService(BulkStage.Post, int.MaxValue)]
internal class AuditHistoryBulkService : IBulkService
{
    public static int NameMaxLength = -1;
    public static int NewValueMaxLength = -1;
    public static int OldValueMaxLength = -1;

    public async Task<Response<object?>> Execute(BulkServiceContext context)
    {
        if (!Cache.Instance.IsAuditEnabled)
        {
            return ServiceResult.Success();
        }
        GetMaxLength();
        var db = context.GetDbContext<BaseDbContext>();
        var newDb = context.AllServiceContexts.First().DataRepository.CreateNewDbContext<BaseDbContext>();
        // Handle Read service contexts
        var readServiceContexts = context.AllServiceContexts
            .Where(sc => sc.DataOperation is DataOperation.Read)
            .ToList();

        if (readServiceContexts.Any())
        {
            foreach (var readServiceContext in readServiceContexts)
            {
                var auditInfo = Cache.Instance.TableAuditInfo[readServiceContext.TableName];
                if (auditInfo.IsReadAuditEnabled)
                {
                    AddAuditHistoryRecord(readServiceContext);    
                }
            }
        }

        // Handle Create \ Update \ Delete service contexts
        var cudServiceContexts = context.AllServiceContexts
            .Where(sc => sc.DataOperation is DataOperation.Create or DataOperation.Update or DataOperation.Delete)
            .ToList();

        if (cudServiceContexts.Any())
        {
            // What was saved to the database may be different from what's currently in the context,
            // so we need to get the latest from the database
            var tableGroups = (from serviceContext in cudServiceContexts
                where (serviceContext.DataOperation is DataOperation.Create &&
                       Cache.Instance.TableAuditInfo[serviceContext.TableName].IsCreateAuditEnabled) ||
                      (serviceContext.DataOperation is DataOperation.Update && Cache.Instance
                          .TableAuditInfo[serviceContext.TableName].IsUpdateAuditEnabled) ||
                      (serviceContext.DataOperation is DataOperation.Delete && Cache.Instance
                          .TableAuditInfo[serviceContext.TableName].IsDeleteAuditEnabled)
                group serviceContext by serviceContext.TableName
                into g
                select new
                {
                    TableName = g.Key,
                    ServiceContexts = g.ToList()
                }).ToList();

            var entDict = new Dictionary<Guid, ServiceContext>();
            if (entDict == null) throw new ArgumentNullException(nameof(entDict));
            foreach (var tableGroup in tableGroups)
            {
                var auditInfo = Cache.Instance.TableAuditInfo[tableGroup.TableName];
                if (auditInfo is { IsCreateAuditEnabled: false, IsUpdateAuditEnabled: false, IsDeleteAuditEnabled: false })
                {
                    continue;
                }
                // Put all the entities into a dictionary for faster access
                foreach (var serviceContext in tableGroup.ServiceContexts)
                {
                    entDict.Add(serviceContext.GetId(), serviceContext);
                }

                var tableType = Cache.Instance.GetTableMetadata(tableGroup.TableName).Type;
                
                // Deletes
                var deleteIds = tableGroup.ServiceContexts
                    .Where(x => x.DataOperation == DataOperation.Delete)
                    .Select(sc => sc.GetId()).ToList();
                // Query for deletes outside of this transaction to retrieve the data as it existed before the delete
                var deleteResults = await DynamicLinq<BaseDbContext>.BatchRequest(newDb, tableType, deleteIds, 100);
                
                // Updates and Creates
                var updateIds = tableGroup.ServiceContexts
                    .Where(x => x.DataOperation != DataOperation.Delete)
                    .Select(sc => sc.GetId()).ToList();
                var updateResults = await DynamicLinq<BaseDbContext>.BatchRequest(db, tableType, updateIds, 100);
                updateResults.AddRange(deleteResults);
                foreach (var entity in updateResults.Select(x => (object)x).ToList())
                {
                    var serviceContext = entDict[entity.GetIdValue(tableType)];
                    var auditHistory = AddAuditHistoryRecord(serviceContext, entity);
                    await AddCreateUpdateDeleteDetails(serviceContext, auditHistory);
                }
            }
        }

        if (context.AllServiceContexts.Any())
        {
            await db.SaveChangesAsync();
        }


        return ServiceResult.Success();
    }

    private object AddAuditHistoryRecord(ServiceContext context, object? entity = null)
    {
        var db = context.GetDbContext<BaseDbContext>();
        var entityMetadata = Cache.Instance.GetTableMetadata(context.TableName);
        string? name = string.Empty;
        Guid? id = null;
        if (entity != null &&
            context.DataOperation is DataOperation.Create or DataOperation.Update or DataOperation.Delete)
        {
            name = context.DataOperation is DataOperation.Delete
                ? context.GetPreEntity<object>().GetNameFieldValue(entityMetadata.Type)
                : entity.GetNameFieldValue(entityMetadata.Type);
            id = entity.GetIdValue(entityMetadata.Type);
        }

        if (context.DataOperation is DataOperation.Read)
        {
            name = context.ReadInput?.tableName;
        }

        var auditHistory = new Dictionary<string, dynamic?>
        {
            ["Name"] = name.Truncate(NameMaxLength),
            ["TableName"] = context.TableName,
            ["Operation"] = context.DataOperation.ToString(),
            ["UserId"] = context.ExecutingUserId,
            ["CreatedDate"] = DateTime.UtcNow
        };
        if (context.DataOperation is DataOperation.Create or DataOperation.Update or DataOperation.Delete)
        {
            auditHistory["EntityId"] = id;
        }

        if (context.DataOperation is DataOperation.Read)
        {
            auditHistory["Query"] = JsonSerializer.Serialize(context.ReadInput);
            auditHistory["Results"] = JsonSerializer.Serialize(context.ReadOutput);
        }

        var auditHistoryMetadata = Cache.Instance.TableMetadata["AuditHistory"];
        var addEntity = EntityUtil.DictionaryToEntity(auditHistoryMetadata.Type, auditHistory);
        db.Add(addEntity);
        return addEntity;
    }

    private async Task AddCreateUpdateDeleteDetails(ServiceContext context, object auditHistory)
    {
        var newDbContext = context.DataRepository.CreateNewDbContext();
        var entity = context.GetEntity<object>();
        var entityMetadata = Cache.Instance.GetTableMetadata(context.TableName);
        var auditHistoryDetailMetadata = Cache.Instance.TableMetadata["AuditHistoryDetail"];
        var auditInfo = Cache.Instance.TableAuditInfo[context.TableName];
        var db = context.GetDbContext<BaseDbContext>();

        if (context.DataOperation is DataOperation.Create)
        {
            foreach (var entityProperty in entityMetadata.Type.GetEntityProperties())
            {
                if (!auditInfo.FieldAuditInfos[entityProperty.Name].IsCreateAuditEnabled)
                {
                    continue;
                }
                var lookupType = entityMetadata.Type.GetLookupType(entityProperty.Name);
                var auditHistoryDetail = new Dictionary<string, dynamic?>();
                auditHistoryDetail["AuditHistory"] = auditHistory;
                auditHistoryDetail["Name"] = entityProperty.Name;
                auditHistoryDetail["FieldType"] =
                    lookupType != null ? "Lookup" : entityProperty.GetUnderlyingType().Name;

                // Is this Guid property a relationship to another entity?
                if (lookupType != null)
                {
                    DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(newDbContext, lookupType);
                    var lookupIdValue = entity.GetValue(entityProperty.Name);
                    if (lookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupType.Name}Id == @0", lookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["NewValue"] = ((object)lookupEntity).GetNameFieldValue(lookupType);
                        auditHistoryDetail["EntityName"] = lookupType.Name;
                        auditHistoryDetail["NewValueId"] = entity.GetValue(entityProperty.Name);
                    }
                }
                else
                {
                    auditHistoryDetail["NewValue"] =
                        FormatValue(entity.GetValue(entityProperty.Name))?.Truncate(NewValueMaxLength);
                }

                var addEntity = EntityUtil.DictionaryToEntity(auditHistoryDetailMetadata.Type, auditHistoryDetail);
                db.Add(addEntity);
            }
        }

        if (context.DataOperation is DataOperation.Update)
        {
            foreach (var entityProperty in entityMetadata.Type.GetEntityProperties())
            {
                if (!auditInfo.FieldAuditInfos[entityProperty.Name].IsUpdateAuditEnabled)
                {
                    continue;
                }
                
                if (!context.ValueChanged(entityProperty.Name))
                {
                    continue;
                }

                var preEntity = context.GetPreEntity<object>();
                var lookupType = entityMetadata.Type.GetLookupType(entityProperty.Name);
                var auditHistoryDetail = new Dictionary<string, dynamic?>();
                auditHistoryDetail["AuditHistory"] = auditHistory;
                auditHistoryDetail["Name"] = entityProperty.Name;
                auditHistoryDetail["FieldType"] =
                    lookupType != null ? "Lookup" : entityProperty.GetUnderlyingType().Name;

                // Is this Guid property a relationship to another entity?
                if (lookupType != null)
                {
                    DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(newDbContext, lookupType);
                    var newLookupIdValue = entity.GetValue(entityProperty.Name);
                    if (newLookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupType.Name}Id == @0", newLookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["NewValue"] = ((object)lookupEntity).GetNameFieldValue(lookupType);
                        auditHistoryDetail["NewValueId"] = entity.GetValue(entityProperty.Name);
                        auditHistoryDetail["EntityName"] = lookupType.Name;
                    }

                    var oldLookupIdValue = preEntity.GetValue(entityProperty.Name);
                    if (oldLookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupType.Name}Id == @0", oldLookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["OldValue"] = ((object)lookupEntity).GetNameFieldValue(lookupType);
                        auditHistoryDetail["OldValueId"] = preEntity.GetValue(entityProperty.Name);
                        auditHistoryDetail["EntityName"] = lookupType.Name;
                    }
                }
                else
                {
                    auditHistoryDetail["NewValue"] =
                        FormatValue(entity.GetValue(entityProperty.Name))?.Truncate(NewValueMaxLength);
                    auditHistoryDetail["OldValue"] = FormatValue(preEntity.GetValue(entityProperty.Name))
                        ?.Truncate(OldValueMaxLength);
                }

                var addEntity = EntityUtil.DictionaryToEntity(auditHistoryDetailMetadata.Type, auditHistoryDetail);
                db.Add(addEntity);
            }
        }

        if (context.DataOperation is DataOperation.Delete)
        {
            foreach (var entityProperty in entityMetadata.Type.GetEntityProperties())
            {
                if (!auditInfo.FieldAuditInfos[entityProperty.Name].IsDeleteAuditEnabled)
                {
                    continue;
                }
                
                var preEntity = context.GetPreEntity<object>();
                var lookupType = entityMetadata.Type.GetLookupType(entityProperty.Name);
                var auditHistoryDetail = new Dictionary<string, dynamic?>();
                auditHistoryDetail["AuditHistory"] = auditHistory;
                auditHistoryDetail["Name"] = entityProperty.Name;
                auditHistoryDetail["FieldType"] =
                    lookupType != null ? "Lookup" : entityProperty.GetUnderlyingType().Name;

                // Is this Guid property a relationship to another entity?
                if (lookupType != null)
                {
                    DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(newDbContext, lookupType);
                    var lookupIdValue = preEntity.GetValue(entityProperty.Name);
                    if (lookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupType.Name}Id == @0", lookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["OldValue"] = ((object)lookupEntity).GetNameFieldValue(lookupType);
                        auditHistoryDetail["EntityName"] = lookupType.Name;
                        auditHistoryDetail["OldValueId"] = preEntity.GetValue(entityProperty.Name);
                    }
                }
                else
                {
                    auditHistoryDetail["OldValue"] = FormatValue(preEntity.GetValue(entityProperty.Name))
                        ?.Truncate(OldValueMaxLength);
                }

                var addEntity = EntityUtil.DictionaryToEntity(auditHistoryDetailMetadata.Type, auditHistoryDetail);
                db.Add(addEntity);
            }
        }
    }

    private void GetMaxLength()
    {
        var auditHistoryDetailMetadata = Cache.Instance.TableMetadata["AuditHistoryDetail"];
        if (NewValueMaxLength == -1 || OldValueMaxLength == -1 || NameMaxLength == -1)
        {
            var newValueProp = auditHistoryDetailMetadata.Type.GetProperty("NewValue");
            if (newValueProp == null)
            {
                throw new InvalidOperationException("NewValue property not found on AuditHistoryDetail");
            }

            var oldValueProp = auditHistoryDetailMetadata.Type.GetProperty("OldValue");
            if (oldValueProp == null)
            {
                throw new InvalidOperationException("OldValue property not found on AuditHistoryDetail");
            }

            var nameProp = auditHistoryDetailMetadata.Type.GetProperty("Name");
            if (nameProp == null)
            {
                throw new InvalidOperationException("Name property not found on AuditHistoryDetail");
            }

            var newMaxLengthAttr = newValueProp.GetCustomAttribute<MaxLengthAttribute>();
            if (newMaxLengthAttr == null)
            {
                throw new InvalidOperationException(
                    "MaxLengthAttribute not found on NewValue property on AuditHistoryDetail");
            }

            var oldMaxLengthAttr = oldValueProp.GetCustomAttribute<MaxLengthAttribute>();
            if (oldMaxLengthAttr == null)
            {
                throw new InvalidOperationException(
                    "MaxLengthAttribute not found on OldValue property on AuditHistoryDetail");
            }

            var nameMaxLengthAttr = nameProp.GetCustomAttribute<MaxLengthAttribute>();
            if (nameMaxLengthAttr == null)
            {
                throw new InvalidOperationException(
                    "MaxLengthAttribute not found on Name property on AuditHistoryDetail");
            }

            NewValueMaxLength = newMaxLengthAttr.Length;
            OldValueMaxLength = oldMaxLengthAttr.Length;
            NameMaxLength = nameMaxLengthAttr.Length;
        }
    }

    private string? FormatValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss tt");
        }

        return value.ToString();
    }
}