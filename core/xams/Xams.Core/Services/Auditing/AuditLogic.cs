using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

public static class AuditLogic
{
    public static int NameMaxLength = 250;
    public static int NewValueMaxLength = 8000;
    public static int OldValueMaxLength = 8000;
    
    public static async Task Audit(IXamsDbContext db, CancellationToken cancellationToken = default)
    {
        if (!db.GetAuditEnabled())
        {
            return;
        }
        
        var dataService = db.GetDataService();
        
        // Track entities for auditing
        var auditOperations = new List<AuditOperation>();

        // Populate lists from context change tracker
        foreach (var entry in db.ChangeTracker.Entries())
        {
            var auditInfo = Cache.Instance.TableAuditInfo[entry.Entity.Metadata().TableName];
            if (auditInfo.IsCreateAuditEnabled && entry.State == EntityState.Added)
            {
                auditOperations.Add(new AuditOperation()
                {
                    Entity = entry.Entity,
                    DataOperation = DataOperation.Create,
                    PreEntity = null, 
                });
            }
            if (auditInfo.IsUpdateAuditEnabled && entry.State == EntityState.Modified)
            {
                auditOperations.Add(new AuditOperation()
                {
                    Entity = entry.Entity,
                    DataOperation = DataOperation.Update,
                    PreEntity = null,
                });
            }
            if (auditInfo.IsDeleteAuditEnabled && entry.State == EntityState.Deleted)
            {
                auditOperations.Add(new AuditOperation()
                {
                    Entity = entry.Entity,
                    PreEntity = null,
                    DataOperation = DataOperation.Delete,
                });
            }
        }
        
        if (!auditOperations.Any())
        {
            return;
        }
        
        var tableGroups = (from auditOperation in auditOperations
            where auditOperation.Entity.GetType() != typeof(Entities.System)
            group auditOperation by auditOperation.Entity.GetType().Metadata().TableName into g
            select new
            {
                TableName = g.Key,
                Operations = g.ToList()
            }).ToList();

        var newDb = dataService.GetDataRepository().CreateNewDbContext();
        // Handle deletes
        foreach (var tableOperation in tableGroups)
        {
            var deletes = tableOperation.Operations
                .Where(x => x.DataOperation == DataOperation.Delete)
                .ToList();
            if (!deletes.Any())
            {
                continue;
            }
            var deleteIds = deletes
                .Where(x => x.PreEntity == null)
                .Select(x => x.Entity.GetId()).ToList();
            var entityType = tableOperation.Operations.First().Entity.GetType();
            var deleteResults = await DynamicLinq.BatchRequest(newDb, entityType, deleteIds);
            var deleteDict = deleteResults.ToDictionary(x => ((object)x).GetId(), x => x);
            
            foreach (var auditOperation in deletes)
            {
                var options = new AuditHistoryRecordOptions()
                {
                    TransactionDb = db,
                    NewDb = newDb,
                    TableName = auditOperation.Entity.Metadata().TableName,
                    Entity = auditOperation.Entity,
                    PreEntity = deleteDict[auditOperation.Entity.GetId()],
                    DataOperation = auditOperation.DataOperation,
                    ExecutingUserId = dataService.GetExecutionUserId(),
                };
                var auditHistory = AddAuditHistoryRecord(options);
                await AddCreateUpdateDeleteDetails(options, auditHistory);
            }
        }
        
        // Handle Updates
        foreach (var tableOperation in tableGroups)
        {
            var updates = tableOperation.Operations
                .Where(x => x.DataOperation == DataOperation.Update)
                .ToList();
            if (!updates.Any())
            {
                continue;
            }
            // Attempt to resolve pre-entities for updates
            foreach (var auditOperation in updates)
            {
                auditOperation.PreEntity = dataService.PreEntities(auditOperation.Entity.GetType(), auditOperation.Entity.GetId());
            }
            var updateIds = updates
                .Where(x => x.PreEntity == null)
                .Select(x => x.Entity.GetId()).ToList();
            var entityType = tableOperation.Operations.First().Entity.GetType();
            var updateResults = await DynamicLinq.BatchRequest(newDb, entityType, updateIds);
            var updateDict = updateResults.ToDictionary(x => ((object)x).GetId(), x => x);
            foreach (var auditOperation in updates)
            {
                var options = new AuditHistoryRecordOptions()
                {
                    TransactionDb = db,
                    NewDb = newDb,
                    TableName = auditOperation.Entity.Metadata().TableName,
                    Entity = auditOperation.Entity,
                    PreEntity = auditOperation.PreEntity ?? updateDict[auditOperation.Entity.GetId()], 
                    DataOperation = auditOperation.DataOperation,
                    ExecutingUserId = dataService.GetExecutionUserId(),
                };
                var auditHistory = AddAuditHistoryRecord(options);
                await AddCreateUpdateDeleteDetails(options, auditHistory);
            }
        }
        
        // Handle Creates
        foreach (var tableOperation in tableGroups)
        {
            var creates = tableOperation.Operations
                .Where(x => x.DataOperation == DataOperation.Create)
                .ToList();
            if (!creates.Any())
            {
                continue;
            }
            foreach (var auditOperation in creates)
            {
                var options = new AuditHistoryRecordOptions()
                {
                    TransactionDb = db,
                    NewDb = newDb,
                    TableName = auditOperation.Entity.Metadata().TableName,
                    Entity = auditOperation.Entity,
                    PreEntity = null, // No pre-entity for create
                    DataOperation = auditOperation.DataOperation,
                    ExecutingUserId = dataService.GetExecutionUserId(),
                };
                var auditHistory = AddAuditHistoryRecord(options);
                await AddCreateUpdateDeleteDetails(options, auditHistory);
            }
        }

        if (auditOperations.Any())
        {
            db.SetAuditEnabled(false);
            await db.SaveChangesAsync(cancellationToken);
            db.SetAuditEnabled(true);
        }
        
        return;
    }

    public class AuditOperation
    {
        public required object Entity { get; set; }
        public object? PreEntity { get; set; }
        public DataOperation DataOperation { get; set; }
    }

    public class AuditHistoryRecordOptions
    {
        public required IXamsDbContext TransactionDb { get; set; }
        public required IXamsDbContext NewDb { get; set; }
        public required string TableName { get; set; }
        public DataOperation DataOperation { get; set; }
        public Guid ExecutingUserId { get; set; }
        public object? Entity { get; set; }
        public object? PreEntity { get; set; }
        public ReadInput? ReadInput { get; set; }
        public ReadOutput? ReadOutput { get; set; }
    }

    public static object AddAuditHistoryRecord(AuditHistoryRecordOptions options)
    {
        string? name = string.Empty;
        object? id = null;
        if (options.Entity != null &&
            options.DataOperation is DataOperation.Create or DataOperation.Update or DataOperation.Delete)
        {
            name = options.DataOperation is DataOperation.Delete
                ? options.PreEntity?.GetNameFieldValue()
                : options.Entity.GetNameFieldValue();
            // Get the ID value - this could be any type, not just Guid
            id = options.Entity.GetId();
        }

        if (options.DataOperation is DataOperation.Read)
        {
            name = options.TableName;
        }

        var auditHistory = new Dictionary<string, dynamic?>
        {
            ["Name"] = name.Truncate(NameMaxLength),
            ["TableName"] = options.TableName,
            ["Operation"] = options.DataOperation.ToString(),
            ["UserId"] = options.ExecutingUserId,
            ["CreatedDate"] = DateTime.UtcNow
        };
        if (options.DataOperation is DataOperation.Create or DataOperation.Update or DataOperation.Delete)
        {
            auditHistory["EntityId"] = id;
        }

        if (options.DataOperation is DataOperation.Read)
        {
            auditHistory["Query"] = JsonSerializer.Serialize(options.ReadInput);
            auditHistory["Results"] = JsonSerializer.Serialize(options.ReadOutput);
        }

        var auditHistoryMetadata = Cache.Instance.TableMetadata["AuditHistory"];
        var addEntity = EntityUtil.DictionaryToEntity(auditHistoryMetadata.Type, auditHistory);
        options.TransactionDb.Add(addEntity);
        return addEntity;
    }

    public static async Task AddCreateUpdateDeleteDetails(AuditHistoryRecordOptions options, object auditHistory)
    {
        var entity = options.Entity ?? throw new InvalidOperationException("Entity cannot be null.");
        var entityMetadata = options.Entity.Metadata();
        var auditHistoryDetailMetadata = Cache.Instance.TableMetadata["AuditHistoryDetail"];
        var auditInfo = Cache.Instance.TableAuditInfo[entityMetadata.TableName];
        var db = options.TransactionDb;

        if (options.DataOperation is DataOperation.Create)
        {
            foreach (var entityProperty in entityMetadata.Type.GetEntityProperties())
            {
                if (!auditInfo.FieldAuditInfos.TryGetValue(entityProperty.Name, out var info))
                {
                    continue;
                }

                if (!info.IsCreateAuditEnabled)
                {
                    continue;
                }

                var lookupType = entityMetadata.Type.GetLookupType(entityProperty.Name);
                var auditHistoryDetail = new Dictionary<string, dynamic?>();
                auditHistoryDetail["AuditHistory"] = auditHistory;
                auditHistoryDetail["FieldName"] = entityProperty.Name;
                auditHistoryDetail["FieldType"] =
                    lookupType != null ? "Lookup" : entityProperty.GetUnderlyingType().Name;

                // Is this Guid property a relationship to another entity?
                if (lookupType != null)
                {
                    var lookupMetadata = Cache.Instance.GetTableMetadata(lookupType);
                    DynamicLinq dynamicLinq = new DynamicLinq(options.NewDb, lookupType);
                    var lookupIdValue = entity.GetValue(entityProperty.Name);
                    if (lookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupMetadata.PrimaryKey} == @0", lookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["NewValue"] = ((object)lookupEntity).GetNameFieldValue();
                        auditHistoryDetail["TableName"] = lookupMetadata.TableName;
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

        if (options.DataOperation is DataOperation.Update)
        {
            foreach (var entityProperty in entityMetadata.Type.GetEntityProperties())
            {
                if (!auditInfo.FieldAuditInfos.TryGetValue(entityProperty.Name, out var info))
                {
                    continue;
                }

                if (!info.IsUpdateAuditEnabled)
                {
                    continue;
                }

                if (!EntityUtil.ValueChanged(options.Entity, options.PreEntity, entityProperty.Name))
                {
                    continue;
                }

                var preEntity = options.PreEntity;
                var lookupType = entityMetadata.Type.GetLookupType(entityProperty.Name);
                var auditHistoryDetail = new Dictionary<string, dynamic?>();
                auditHistoryDetail["AuditHistory"] = auditHistory;
                auditHistoryDetail["FieldName"] = entityProperty.Name;
                auditHistoryDetail["FieldType"] =
                    lookupType != null ? "Lookup" : entityProperty.GetUnderlyingType().Name;

                // Is this Guid property a relationship to another entity?
                if (lookupType != null)
                {
                    var lookupMetadata = Cache.Instance.GetTableMetadata(lookupType);
                    DynamicLinq dynamicLinq = new DynamicLinq(options.NewDb, lookupType);
                    var newLookupIdValue = entity.GetValue(entityProperty.Name);
                    if (newLookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupMetadata.PrimaryKey} == @0", newLookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["NewValue"] = ((object)lookupEntity).GetNameFieldValue();
                        auditHistoryDetail["NewValueId"] = entity.GetValue(entityProperty.Name);
                        auditHistoryDetail["TableName"] = lookupMetadata.TableName;
                    }

                    var oldLookupIdValue = preEntity.GetValue(entityProperty.Name);
                    if (oldLookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupMetadata.PrimaryKey} == @0", oldLookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["OldValue"] = ((object)lookupEntity).GetNameFieldValue();
                        auditHistoryDetail["OldValueId"] = preEntity.GetValue(entityProperty.Name);
                        auditHistoryDetail["TableName"] = lookupMetadata.TableName;
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

        if (options.DataOperation is DataOperation.Delete)
        {
            foreach (var entityProperty in entityMetadata.Type.GetEntityProperties())
            {
                if (!auditInfo.FieldAuditInfos.TryGetValue(entityProperty.Name, out var info))
                {
                    continue;
                }

                if (!info.IsDeleteAuditEnabled)
                {
                    continue;
                }

                var preEntity = options.PreEntity;
                var lookupType = entityMetadata.Type.GetLookupType(entityProperty.Name);
                var auditHistoryDetail = new Dictionary<string, dynamic?>();
                if (auditHistoryDetail == null)
                {
                    throw new InvalidOperationException("AuditHistoryDetail dictionary cannot be null.");
                }

                auditHistoryDetail["AuditHistory"] = auditHistory;
                auditHistoryDetail["FieldName"] = entityProperty.Name;
                auditHistoryDetail["FieldType"] =
                    lookupType != null ? "Lookup" : entityProperty.GetUnderlyingType().Name;

                // Is this Guid property a relationship to another entity?
                if (lookupType != null)
                {
                    var lookupMetadata = Cache.Instance.GetTableMetadata(lookupType);
                    DynamicLinq dynamicLinq = new DynamicLinq(options.NewDb, lookupType);
                    var lookupIdValue = preEntity?.GetValue(entityProperty.Name);
                    if (lookupIdValue != null)
                    {
                        var query = dynamicLinq.Query.Where($"{lookupMetadata.PrimaryKey} == @0", lookupIdValue);
                        var lookupEntity = (await query.ToDynamicListAsync()).FirstOrDefault() ??
                                           throw new InvalidOperationException("Lookup entity not found.");
                        auditHistoryDetail["OldValue"] = ((object)lookupEntity).GetNameFieldValue();
                        auditHistoryDetail["TableName"] = Cache.Instance.GetTableMetadata(lookupType).TableName;
                        auditHistoryDetail["OldValueId"] = preEntity?.GetValue(entityProperty.Name);
                    }
                }
                else
                {
                    auditHistoryDetail["OldValue"] = FormatValue(preEntity?.GetValue(entityProperty.Name))
                        ?.Truncate(OldValueMaxLength);
                }

                var addEntity = EntityUtil.DictionaryToEntity(auditHistoryDetailMetadata.Type, auditHistoryDetail);
                db.Add(addEntity);
            }
        }
    }

    public static string? FormatValue(object? value)
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