using System.Linq.Dynamic.Core;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Utils;

namespace Xams.Core.Pipeline.Stages;

public class PipeMultiSelectFields : BasePipelineStage
{
    public override async Task<Response<object?>> Execute(PipelineContext context)
    {
        // Only process on Create and Update operations
        if (context.DataOperation is not (DataOperation.Create or DataOperation.Update))
        {
            return await base.Execute(context);
        }

        // Entity and Fields must be available
        if (context.Entity == null || context.Fields == null)
        {
            return await base.Execute(context);
        }

        var response = await ProcessMultiSelectFields(context);
        if (!response.Succeeded)
        {
            return response;
        }

        return await base.Execute(context);
    }

    private async Task<Response<object?>> ProcessMultiSelectFields(PipelineContext context)
    {
        // Get entity metadata
        var metadata = Cache.Instance.GetTableMetadata(context.TableName);

        // Find all UIMultiSelect fields from cached metadata
        var multiSelectFields = metadata.MetadataOutput.fields
            .Where(f => f.multiSelect != null)
            .ToList();

        if (multiSelectFields.Count == 0)
        {
            return ServiceResult.Success();
        }

        // Get the owner ID from the entity (primary key value)
        var ownerIdValue = metadata.PrimaryKeyProperty.GetValue(context.Entity);
        if (ownerIdValue == null)
        {
            return ServiceResult.Error("Primary key value is null");
        }

        // Process each UIMultiSelect field
        foreach (var field in multiSelectFields)
        {
            var multiSelect = field.multiSelect;
            if (multiSelect == null) continue;

            // Check if this field was provided in the submission
            if (!context.Fields.ContainsKey(field.name))
            {
                continue;
            }

            // Get the array of target IDs from the fields dictionary
            // Handle both formats: ["id1", "id2"] or [{id: "id1", name: "Name1"}, ...]
            List<Guid> targetIds = new List<Guid>();
            try
            {
                var fieldValue = context.Fields[field.name];
                if (fieldValue is JsonElement jsonElement)
                {
                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            // Handle string format: ["guid1", "guid2"]
                            if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out Guid guid))
                            {
                                targetIds.Add(guid);
                            }
                            // Handle object format: [{id: "guid1", name: "John"}, ...]
                            else if (item.ValueKind == JsonValueKind.Object)
                            {
                                if (item.TryGetProperty("id", out JsonElement idElement))
                                {
                                    if (idElement.ValueKind == JsonValueKind.String && Guid.TryParse(idElement.GetString(), out Guid objGuid))
                                    {
                                        targetIds.Add(objGuid);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (fieldValue is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        // Handle string format
                        if (item is string str && Guid.TryParse(str, out Guid parsedGuid))
                        {
                            targetIds.Add(parsedGuid);
                        }
                        // Handle Guid format
                        else if (item is Guid guidItem)
                        {
                            targetIds.Add(guidItem);
                        }
                        // Handle object format with "id" property
                        else if (item is IDictionary<string, object> dict)
                        {
                            if (dict.ContainsKey("id"))
                            {
                                var idValue = dict["id"];
                                if (idValue is string idStr && Guid.TryParse(idStr, out Guid dictGuid))
                                {
                                    targetIds.Add(dictGuid);
                                }
                                else if (idValue is Guid dictGuidDirect)
                                {
                                    targetIds.Add(dictGuidDirect);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ServiceResult.Error($"Failed to parse {field.name} values: {ex.Message}");
            }

            // Get the junction table metadata from the multiSelect configuration
            var junctionMetadata = Cache.Instance.GetTableMetadata(multiSelect.junctionTable);
            var junctionType = junctionMetadata.Type;

            // For UPDATE: get existing junction records to determine what to add/remove
            List<Guid> existingTargetIds = new List<Guid>();
            if (context.DataOperation == DataOperation.Update)
            {
                var db = context.DataRepository.GetDbContext<IXamsDbContext>();
                var dLinq = new DynamicLinq(db, junctionType);
                IQueryable existingQuery = dLinq.Query;
                existingQuery = existingQuery.Where(
                    $"{multiSelect.junctionOwnerIdField} == @0",
                    ownerIdValue
                );

                var existingRecords = await existingQuery.ToDynamicListAsync();

                foreach (var record in existingRecords)
                {
                    var targetIdProp = record.GetType().GetProperty(multiSelect.junctionTargetIdField);
                    if (targetIdProp != null)
                    {
                        var targetIdValue = targetIdProp.GetValue(record);
                        if (targetIdValue is Guid guidValue)
                        {
                            existingTargetIds.Add(guidValue);
                        }
                    }
                }
            }

            // Determine which records to add and remove
            var toAdd = targetIds.Except(existingTargetIds).ToList();
            var toRemove = existingTargetIds.Except(targetIds).ToList();

            // Delete removed junction records
            foreach (var targetId in toRemove)
            {
                var db = context.DataRepository.GetDbContext<IXamsDbContext>();
                var dLinq = new DynamicLinq(db, junctionType);

                // Find the junction record to delete
                IQueryable junctionQuery = dLinq.Query;
                junctionQuery = junctionQuery.Where(
                    $"{multiSelect.junctionOwnerIdField} == @0 && {multiSelect.junctionTargetIdField} == @1",
                    ownerIdValue,
                    targetId
                );

                var junctionRecords = await junctionQuery.ToDynamicListAsync();

                if (junctionRecords.Count() > 0)
                {
                    var junctionRecord = junctionRecords[0];

                    // Get the junction record ID
                    var junctionIdProp = junctionRecord.GetType().GetProperty(junctionMetadata.PrimaryKey);
                    if (junctionIdProp != null)
                    {
                        var junctionId = junctionIdProp.GetValue(junctionRecord);

                        // Find the full entity to delete
                        var junctionEntity = await db.FindAsync(junctionType, junctionId);
                        if (junctionEntity != null)
                        {
                            // Delete using DataService to execute the full pipeline
                            var deleteResp = await context.DataService.Delete(
                                context.UserId,
                                junctionEntity,
                                null,
                                context
                            );

                            if (!deleteResp.Succeeded)
                            {
                                return deleteResp;
                            }
                        }
                    }
                }
            }

            // Create new junction records
            foreach (var targetId in toAdd)
            {
                var junctionData = new Dictionary<string, dynamic>
                {
                    { multiSelect.junctionOwnerIdField, ownerIdValue },
                    { multiSelect.junctionTargetIdField, targetId }
                };

                // Convert to entity and create using DataService to execute the full pipeline
                var junctionEntity = EntityUtil.DictionaryToEntity(junctionType, junctionData);

                var createResp = await context.DataService.Create(
                    context.UserId,
                    junctionEntity,
                    null,
                    context
                );

                if (!createResp.Succeeded)
                {
                    return createResp;
                }
            }
        }

        return ServiceResult.Success();
    }
}
