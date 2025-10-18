using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

#pragma warning disable CS0183 // 'is' expression's given expression is always of the provided type

namespace Xams.Core.Services;

public class QueryFactory
{
    private readonly IXamsDbContext _dbContext;
    private readonly QueryOptions _queryOptions;
    private readonly ReadInput _readInput;

    public QueryFactory(IXamsDbContext dbContext, QueryOptions queryOptions, ReadInput readInput)
    {
        _dbContext = dbContext;
        _queryOptions = queryOptions;
        _readInput = readInput;
    }

    public Query Create()
    {
        if (!Validate(_readInput))
        {
            throw new Exception("Invalid query");
        }

        // Copy the readInput as it will be modified
        ReadInput? readInputCopy = JsonSerializer.Deserialize<ReadInput>(JsonSerializer.Serialize(_readInput));

        if (readInputCopy == null)
        {
            throw new Exception("Failed to copy readInput");
        }

        // If '*' is in the fields, replace it with all fields
        OmitHiddenFields(readInputCopy);

        // Remove UIMultiSelect fields as they don't map to database columns
        RemoveMultiSelectFields(readInputCopy);

        // If the query doesn't include OwningUserId or OwningTeamId, add it
        AddOwnerFields(readInputCopy);

        // If denormalize is enabled, ensure join fields are included
        AddJoinFieldsForDenormalize(readInputCopy);

        // Ensure all fields are distinct
        readInputCopy.fields = readInputCopy.fields.Distinct().ToArray();
        if (readInputCopy.joins != null)
        {
            foreach (var join in readInputCopy.joins)
            {
                join.fields = join.fields.Distinct().ToArray();
            }
        }


        var query = Base(readInputCopy);

        // add joins
        if (readInputCopy.joins != null)
        {
            foreach (var join in readInputCopy.joins)
            {
                join.alias ??= join.toTable;

                // Required to make the join 
                if (!join.fields.Contains(join.toField))
                {
                    join.fields = [..join.fields, join.toField];
                }

                var joinReadInput = new ReadInput()
                {
                    tableName = join.toTable,
                    fields = join.fields,
                };
                var joinQuery = Base(joinReadInput, join.alias);
                if (join.type == "left")
                {
                    query.LeftJoin($"{join.fromTable}.{join.fromField}", $"{join.alias}.{join.toField}", joinQuery);
                }
                else
                {
                    query.Join($"{join.fromTable}.{join.fromField}", $"{join.alias}.{join.toField}", joinQuery);
                }
            }
        }

        AddFilters(query, readInputCopy);

        AddExcept(query);

        AddOrder(query);

        if (readInputCopy.distinct == true)
        {
            query.Distinct();
        }

        return query;
    }

    public Query Base(ReadInput readInput, string fieldPrefix = "root")
    {
        if (!_queryOptions.Permissions.Any())
        {
            throw new Exception("Permissions are required");
        }
        
        var highestPermission = Permissions.GetHighestPermission(_queryOptions.Permissions, readInput.tableName);

        // Only select the fields that are needed for select and filter
        Query query;
        if (readInput.tableName == "User")
        {
            if (highestPermission == Permissions.PermissionLevel.System)
            {
                query = new Query(_dbContext, readInput.fields, fieldPrefix).From("User");
                if (readInput.id != null)
                {
                    query.Where($"{fieldPrefix}_UserId == @0", readInput.GetId());
                }
            }
            else if (highestPermission == Permissions.PermissionLevel.Team)
            {
                var joinQuery = new Query(_dbContext, ["TeamId"], "system_base")
                    .From("TeamUser")
                    .Where("system_base_UserId == @0", _queryOptions.UserId);

                query = new Query(_dbContext, readInput.fields, fieldPrefix)
                    .From("User")
                    .Join("User.UserId", "TeamUser.UserId", "system_tu", [])
                    .Join("system_tu.TeamId", "system_base.TeamId", joinQuery);
            }
            else
            {
                query = new Query(_dbContext, readInput.fields, fieldPrefix).From("User")
                    .Where($"{fieldPrefix}_UserId == @0", _queryOptions.UserId);
            }
        }
        else if (readInput.tableName == "Team")
        {
            if (highestPermission == Permissions.PermissionLevel.System)
            {
                query = new Query(_dbContext, readInput.fields, fieldPrefix).From("Team");
                if (readInput.id != null)
                {
                    query.Where($"{fieldPrefix}_TeamId == @0", readInput.GetId());
                }
            }
            else if (highestPermission == Permissions.PermissionLevel.Team)
            {
                query = new Query(_dbContext, readInput.fields, fieldPrefix)
                    .From("Team")
                    .Join("Team.TeamId", "TeamUser.TeamId", "system_tu", [])
                    .Where("system_tu_UserId == @0", _queryOptions.UserId);
            }
            else
            {
                query = new Query(_dbContext, readInput.fields, fieldPrefix).From("Team")
                    .Where($"{fieldPrefix}_TeamId == @0", _queryOptions.UserId);
            }
        }
        else
        {
            List<Query> queries = new List<Query>();
            string[] joinOns = { "user" };

            Type targetType = Cache.Instance.GetTableMetadata(readInput.tableName).Type;
            bool hasOwningUserId = targetType.GetProperties().FirstOrDefault(x => x.Name == "OwningUserId") !=
                                   null;
            bool hasOwningTeamId = targetType.GetProperties().FirstOrDefault(x => x.Name == "OwningTeamId") !=
                                   null;
            
            // If this table has a OwningUserId or OwningTeamId field, we need to union the two queries together
            if ((hasOwningUserId || hasOwningTeamId) && highestPermission == Permissions.PermissionLevel.Team)
            {
                joinOns = ["user", "team"];
            }

            foreach (var joinOn in joinOns)
            {
                var subQuery = new Query(_dbContext, readInput.fields, fieldPrefix).From(readInput.tableName);
                if (readInput.id != null)
                {
                    subQuery.Where(
                        $"{fieldPrefix}_{Cache.Instance.GetTableMetadata(readInput.tableName).PrimaryKey} == @0",
                        readInput.GetId());
                }

                // Security - Filter by user permissions
                AddSecurityFilter(subQuery, joinOn, hasOwningUserId, hasOwningTeamId, highestPermission);

                queries.Add(subQuery);
            }

            // Join the two queries together
            query = joinOns.Length > 1 ? queries[0].Union(queries[1]) : queries[0];
        }

        return query;
    }

    private void OmitHiddenFields(ReadInput readInput)
    {
        if (readInput.fields.Length > 0 && readInput.fields.Contains("*"))
        {
            // Get all fields on the table
            List<string> rootFields = new();
            var metadata = Cache.Instance.GetTableMetadata(readInput.tableName);
            var properties = metadata.Type.GetEntityProperties();
            foreach (var property in properties)
            {
                // Ignore the hidden fields
                if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null)
                    continue;

                // Skip discriminators
                var modelEntity = _dbContext.Model.FindEntityType(metadata.Type);
                if (modelEntity != null && !string.IsNullOrEmpty(modelEntity.GetDiscriminatorPropertyName())
                    && modelEntity.GetDiscriminatorPropertyName() == property.Name)
                    continue; 
                
                rootFields.Add(property.Name);
            }

            readInput.fields = rootFields.ToArray();
        }

        // Join fields
        if (readInput.joins != null)
        {
            foreach (var join in readInput.joins)
            {
                if (join.fields.Length > 0 && join.fields.Contains("*"))
                {
                    // Get all fields on the table
                    List<string> joinFields = new();
                    var metadata = Cache.Instance.GetTableMetadata(join.toTable);
                    var properties = metadata.Type.GetEntityProperties();
                    foreach (var property in properties)
                    {
                        // Ignore the hidden fields
                        if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null)
                            continue;
                        
                        // Skip discriminators
                        var modelEntity = _dbContext.Model.FindEntityType(metadata.Type);
                        if (modelEntity != null && !string.IsNullOrEmpty(modelEntity.GetDiscriminatorPropertyName())
                                                && modelEntity.GetDiscriminatorPropertyName() == property.Name)
                            continue;

                        joinFields.Add(property.Name);
                    }

                    join.fields = joinFields.ToArray();
                }
            }
        }

        // Except fields
        if (readInput.except != null)
        {
            foreach (var exclude in readInput.except)
            {
                OmitHiddenFields(exclude.query);
            }
        }
    }

    private void RemoveMultiSelectFields(ReadInput readInput)
    {
        // Remove UIMultiSelect fields from root table
        if (readInput.fields.Length > 0)
        {
            List<string> fieldsToKeep = new();
            var metadata = Cache.Instance.GetTableMetadata(readInput.tableName);
            var properties = metadata.Type.GetEntityProperties();

            foreach (var fieldName in readInput.fields)
            {
                var property = properties.FirstOrDefault(p => p.Name == fieldName);
                if (property != null)
                {
                    fieldsToKeep.Add(fieldName);
                }
            }

            readInput.fields = fieldsToKeep.ToArray();
        }

        // Remove UIMultiSelect fields from joins
        if (readInput.joins != null)
        {
            foreach (var join in readInput.joins)
            {
                if (join.fields.Length > 0)
                {
                    List<string> joinFieldsToKeep = new();
                    var joinMetadata = Cache.Instance.GetTableMetadata(join.toTable);
                    var joinProperties = joinMetadata.Type.GetEntityProperties();

                    foreach (var fieldName in join.fields)
                    {
                        var property = joinProperties.FirstOrDefault(p => p.Name == fieldName);
                        if (property != null)
                        {
                            joinFieldsToKeep.Add(fieldName);
                        }
                    }

                    join.fields = joinFieldsToKeep.ToArray();
                }
            }
        }

        // Recursively process except fields
        if (readInput.except != null)
        {
            foreach (var exclude in readInput.except)
            {
                RemoveMultiSelectFields(exclude.query);
            }
        }
    }

    private void AddOwnerFields(ReadInput readInput)
    {
        var metadata = Cache.Instance.GetTableMetadata(readInput.tableName);
        if (metadata == null)
        {
            throw new Exception($"Table {readInput.tableName} does not exist");
        }

        if (metadata.HasOwningUserField && !readInput.fields.Contains("OwningUserId"))
        {
            readInput.fields = readInput.fields.Append("OwningUserId").ToArray();
        }

        if (metadata.HasOwningTeamField && !readInput.fields.Contains("OwningTeamId"))
        {
            readInput.fields = readInput.fields.Append("OwningTeamId").ToArray();
        }

        // Add custom owning user fields
        foreach (var owningUserField in metadata.OwningUserFields)
        {
            if (!readInput.fields.Contains(owningUserField))
            {
                readInput.fields = readInput.fields.Append(owningUserField).ToArray();
            }
        }
    }

    private void AddJoinFieldsForDenormalize(ReadInput readInput)
    {
        if (readInput.denormalize != true || readInput.joins == null)
        {
            return;
        }

        // Add primary key to root fields (needed for identifying unique records during denormalization)
        var rootMetadata = Cache.Instance.GetTableMetadata(readInput.tableName);
        if (!readInput.fields.Contains(rootMetadata.PrimaryKey) && !readInput.fields.Contains("*"))
        {
            readInput.fields = readInput.fields.Append(rootMetadata.PrimaryKey).ToArray();
        }

        foreach (var join in readInput.joins)
        {
            // Ensure fromField is in the appropriate fields array (needed for matching records)
            if (join.fromTable == readInput.tableName)
            {
                // Add to root fields
                if (!readInput.fields.Contains(join.fromField) && !readInput.fields.Contains("*"))
                {
                    readInput.fields = [..readInput.fields, join.fromField];
                }
            }
            else
            {
                // Handle nested joins where fromTable is another join's alias
                var parentJoin = readInput.joins.FirstOrDefault(j => (j.alias ?? j.toTable) == join.fromTable);
                if (parentJoin != null)
                {
                    if (!parentJoin.fields.Contains(join.fromField) && !parentJoin.fields.Contains("*"))
                    {
                        parentJoin.fields = [..parentJoin.fields, join.fromField];
                    }
                }
            }

            // Add primary key to join fields (needed for identifying unique joined records)
            var joinMetadata = Cache.Instance.GetTableMetadata(join.toTable);
            if (!join.fields.Contains(joinMetadata.PrimaryKey) && !join.fields.Contains("*"))
            {
                join.fields = [..join.fields, joinMetadata.PrimaryKey];
            }

            // Note: toField is already auto-added at lines 70-73 in the Create() method
        }
    }

    private void AddSecurityFilter(Query query, string joinOn, bool hasOwningUserId, bool hasOwningTeamId, Permissions.PermissionLevel? highestPermission)
    {
        // Can this table only be managed with System level access?
        if (!hasOwningUserId && !hasOwningTeamId && !_queryOptions.Permissions.Any(x => x.EndsWith("_SYSTEM")))
        {
            query.Where($"{query.RootAlias}_{query.TableName}Id == @0", Guid.Empty);
            return;
        }

        // User can read user owned, Team can read user and team owned, System can read all
        if (highestPermission != null)
        {
            // If the user only has user level permissions and this entity doesn't have an OwningUser field return no results
            if (highestPermission is Permissions.PermissionLevel.User && !hasOwningUserId)
            {
                query.Where($"{query.RootAlias}_{query.TableName}Id == @0", Guid.Empty);
            }

            // If the user only has user level permissions and this entity has an OwningUser field, filter by the user's id
            if (highestPermission is Permissions.PermissionLevel.User && hasOwningUserId)
            {
                var metadata = Cache.Instance.GetTableMetadata(query.TableName);

                // Build OR condition for OwningUserId and all custom owning user fields
                if (metadata.OwningUserFields.Count > 0)
                {
                    var conditions = new List<string> { $"{query.RootAlias}_OwningUserId == @0" };
                    for (int i = 0; i < metadata.OwningUserFields.Count; i++)
                    {
                        conditions.Add($"{query.RootAlias}_{metadata.OwningUserFields[i]} == @0");
                    }
                    query.Where($"({string.Join(" || ", conditions)})", _queryOptions.UserId);
                }
                else
                {
                    query.Where($"{query.RootAlias}_OwningUserId == @0", _queryOptions.UserId);
                }
            }

            // If the user has team level access, show them records owned by their teams and by themselves
            if (highestPermission is Permissions.PermissionLevel.Team)
            {
                if (hasOwningTeamId && joinOn == "team")
                {
                    query.Join($"{query.TableName}.OwningTeamId", "TeamUser.TeamId", "system_tu",
                        Array.Empty<string>());
                    query.Where($"system_tu_UserId == @0", _queryOptions.UserId);
                }

                if (hasOwningUserId && joinOn == "user")
                {
                    var metadata = Cache.Instance.GetTableMetadata(query.TableName);

                    // Build OR condition for OwningUserId and all custom owning user fields
                    if (metadata.OwningUserFields.Count > 0)
                    {
                        var conditions = new List<string> { $"{query.RootAlias}_OwningUserId == @0" };
                        for (int i = 0; i < metadata.OwningUserFields.Count; i++)
                        {
                            conditions.Add($"{query.RootAlias}_{metadata.OwningUserFields[i]} == @0");
                        }
                        query.Where($"({string.Join(" || ", conditions)})", _queryOptions.UserId);
                    }
                    else
                    {
                        query.Where($"{query.RootAlias}_OwningUserId == @0", _queryOptions.UserId);
                    }
                }
            }
        }
    }

    private void AliasJoinFilters(Join[]? joins)
    {
        if (joins == null)
        {
            return;
        }

        // Move the join filters to the primary query
        foreach (var join in joins)
        {
            if (join.filters == null)
            {
                continue;
            }

            foreach (var filter in join.filters)
            {
                filter.field = $"{join.alias}.{filter.field}";
            }
        }
    }

    private void AddFilters(Query query, ReadInput readInput, string? logicalOperator = null)
    {
        if (readInput.filters == null || readInput.filters.Length == 0)
        {
            return;
        }

        // Add join filters
        AliasJoinFilters(readInput.joins);
        readInput.filters = readInput.filters.Concat(readInput.joins?
            .Where(x => x.filters != null)
            .SelectMany(x => x.filters ?? Array.Empty<Filter>()) ?? Array.Empty<Filter>()).ToArray();

        Type targetType = Cache.Instance.GetTableMetadata(query.TableName).Type;
        FilterData filterData =
            GetFilters(targetType, query, readInput.filters, readInput.joins, logicalOperator);

        if (filterData.Values.Count == 0)
        {
            return;
        }

        query.Where(filterData.Filter.ToString(), filterData.Values.ToArray());
    }

    private FilterData GetFilters(Type targetType, Query query, Filter[]? filters, Join[]? joins = null,
        string? logicalOperator = null, int index = 0)
    {
        FilterData filterData = new FilterData();

        if (filters == null)
        {
            return filterData;
        }

        logicalOperator ??= "&&";

        List<string> conditions = new();
        List<object> values = new();
        foreach (var filter in filters)
        {
            // Skip the logical grouping until later
            if (!string.IsNullOrEmpty(filter.logicalOperator))
            {
                continue;
            }

            if (string.IsNullOrEmpty(filter.field))
            {
                continue;
            }

            string table = $"{query.RootAlias}_"; // Default to the root table
            string field;

            // Get filter field type
            Type? fieldType;
            Type? underlyingType;
            bool isNullable = false;

            // If this is a filter on a joined table
            PropertyInfo? property;
            if (joins is { Length: > 0 } && filter.field.Contains('.'))
            {
                string[] fieldParts = filter.field.Split('.');
                string joinAlias = fieldParts[0];
                string joinField = fieldParts[1];
                table = joinAlias + "_";
                field = joinField;
                var join = joins.FirstOrDefault(x =>
                    x.alias == joinAlias || (string.IsNullOrEmpty(x.alias) && x.toTable == joinAlias));
                if (join == null)
                {
                    throw new Exception($"Join {joinAlias} does not exist");
                }

                // Get the type of the joined table
                Type joinType = Cache.Instance.GetTableMetadata(join.toTable).Type;
                if (joinType == null)
                {
                    throw new Exception($"Table {join.toTable} does not exist");
                }

                // Get the type of the field on the joined table
                property = joinType.GetProperty(joinField);
                if (property == null)
                {
                    throw new Exception($"Field {joinField} does not exist on {join.toTable}");
                }

                fieldType = property.PropertyType;
                underlyingType = Nullable.GetUnderlyingType(fieldType);
                if (underlyingType != null)
                {
                    fieldType = underlyingType;
                    isNullable = true;
                }
            }
            else
            {
                field = filter.field;
                property = targetType.GetProperty(filter.field);
                if (property == null)
                {
                    throw new Exception($"{targetType.Name} does not contain a field named {filter.field}");
                }

                fieldType = property.PropertyType;
                underlyingType = Nullable.GetUnderlyingType(fieldType);
                if (underlyingType != null)
                {
                    fieldType = underlyingType;
                    isNullable = true;
                }
            }

            if (fieldType == null)
            {
                throw new Exception($"{targetType.Name} does not contain a field named {filter.field}");
            }

            if (fieldType == typeof(Guid))
            {
                filter.@operator = string.IsNullOrEmpty(filter.@operator) ? "==" : filter.@operator;


                if (filter.@operator.ToLower() == "contains")
                {
                    if (string.IsNullOrEmpty(filter.value))
                    {
                        continue;
                    }

                    // This isn't working for Guids though ideally this would work
                    // conditions.Add($"Convert.ToString({table}{field}).Contains(@{index})");
                    // values.Add($"{filter.value}");
                    conditions.Add($"{table}{field} == @{index}");
                    values.Add(filter.value);
                }
                else if (Guid.TryParse(filter.value, out Guid id))
                {
                    if (string.IsNullOrEmpty(filter.@operator))
                    {
                        conditions.Add($"{table}{field} == @{index}");
                        values.Add(id);
                    }
                    else if (IsValidOperator(filter.@operator))
                    {
                        conditions.Add($"{table}{field} {filter.@operator} @{index}");
                        values.Add(id);
                    }
                }
                else
                {
                    bool useNull = filter.value == null || filter.value.Trim().ToLower() == "null";
                    if (string.IsNullOrEmpty(filter.@operator))
                    {
                        if (useNull)
                        {
                            conditions.Add($"{table}{field} == null");
                            values.Add("");
                        }
                        else
                        {
                            conditions.Add($"{table}{field} == @{index}");
                            values.Add(Guid.Empty);
                        }
                    }
                    else if (IsValidOperator(filter.@operator))
                    {
                        if (useNull)
                        {
                            conditions.Add($"{table}{field} {filter.@operator} null");
                            values.Add("");
                        }
                        else
                        {
                            conditions.Add($"{table}{field} {filter.@operator} @{index}");
                            values.Add(Guid.Empty);
                        }
                    }
                }
            }
            else if (ParseNumeric(table, field, index, filter, conditions, values, fieldType))
            {}
            else if (fieldType == typeof(DateTime))
            {
                if (string.IsNullOrEmpty(filter.value) || filter.value.Trim().ToLower() == "null")
                {
                    continue;
                }

                // If the value contains a 'T' and 'Z' it's likely an ISO 8601 UTC date time, use exact
                if (filter.value.Contains("T") && filter.value.Contains("Z"))
                {
                    
                    if (DateTime.TryParse(filter.value, out DateTime dateTime))
                    {
                        var utc = dateTime.Kind == DateTimeKind.Utc ? dateTime : dateTime.ToUniversalTime();
                        filter.@operator = string.IsNullOrEmpty(filter.@operator) ? "==" : filter.@operator;
                        if (IsValidOperator(filter.@operator))
                        {
                            conditions.Add($"{table}{field} {filter.@operator} @{index}");
                            values.Add(utc);
                        }
                    }
                    else
                    {
                        // If the date time can't be parsed, ensure no results are returned
                        conditions.Add($"1 == @{index}");
                        values.Add(2);
                    }
                    index++;
                    continue;
                }
                
                // The query can send the user's local date time offset in the format of "~-12" or "~+12"
                // But we only want to take this into account if the date time field has a time part
                var dateTimeFormatAttribute = property.GetCustomAttribute<UIDateFormatAttribute>();
                var hasTimePart = dateTimeFormatAttribute?.HasTimePart();

                filter.@operator = string.IsNullOrEmpty(filter.@operator) ? "==" : filter.@operator;
                Regex regUtcOffset = new Regex("~[-+]+[0-9][0-9]?");
                var match = regUtcOffset.Match(filter.value ?? "");
                if (!string.IsNullOrEmpty(match.Value))
                {
                    filter.value = filter.value?.Replace(match.Value, "") ?? "";    
                }
                
                var conditionField = $"{table}{field}{(isNullable ? ".Value" : "")}";
                
                if (!string.IsNullOrEmpty(match.Value) && hasTimePart == true)
                {
                    conditionField += $".AddHours({match.Value.Replace("~", "")})";
                }

                string[] parts = filter.value.Split(" ");
                if (filter.value.Contains("/"))
                {
                    parts = filter.value.Split("/");
                }
                else if (filter.value.Contains("-"))
                {
                    parts = filter.value.Split("-");
                }

                int valuesCount = values.Count;
                for (int j = 0; j < parts.Length; j++)
                {
                    string part = parts[j].Trim();
                    if (string.IsNullOrEmpty(part))
                    {
                        continue;
                    }

                    // If there's no date time format attribute, the date time will be shown in tables as 'MM/dd/yyyy'
                    if (filter.value.Contains('/') || filter.value.Contains('-') || dateTimeFormatAttribute == null)
                    {
                        // Use 2025-10-01 (ISO 8601 - YYYY-MM-DD)
                        part = part.TrimStart('0');
                        // Year Part
                        if (j == 0 || part.All(char.IsDigit) && part.Length == 4)
                        {
                            index = values.Count != valuesCount ? index + 1 : index;
                            conditions.Add($"{conditionField}.Year {filter.@operator} @{index}");
                            values.Add(part);
                        }
                        // Month Part
                        else if (j == 1 && part.All(char.IsDigit) && part.Length <= 2)
                        {
                            index = values.Count != valuesCount ? index + 1 : index;
                            conditions.Add($"{conditionField}.Month {filter.@operator} @{index}");
                            values.Add(part);
                        }
                        // Day part
                        else if (j == 2 && part.All(char.IsDigit) && part.Length <= 2)
                        {
                            index = values.Count != valuesCount ? index + 1 : index;
                            conditions.Add($"{conditionField}.Day {filter.@operator} @{index}");
                            values.Add(part);
                        }
                    }
                    else
                    {
                        if (part.All(char.IsDigit) && part.Length == 4)
                        {
                            index = values.Count != valuesCount ? index + 1 : index;
                            conditions.Add($"{conditionField}.Year {filter.@operator} @{index}");
                            values.Add(part);
                        }
                        else if (part.All(char.IsDigit) && part.Length <= 2)
                        {
                            index = values.Count != valuesCount ? index + 1 : index;
                            conditions.Add($"{conditionField}.Day {filter.@operator} @{index}");
                            values.Add(part);
                        }
                        else if (part.All(char.IsLetter))
                        {
                            string[] months =
                            [
                                "January", "February", "March", "April", "May", "June", "July", "August", "September",
                                "October", "November", "December"
                            ];
                            for (int i = 0; i < months.Length; i++)
                            {
                                string month = months[i];
                                if (month.Contains(part))
                                {
                                    index = values.Count != valuesCount ? index + 1 : index;
                                    conditions.Add($"{conditionField}.Month {filter.@operator} @{index}");
                                    values.Add(i + 1);
                                    break;
                                }
                            }
                        }
                    }
                }

                // If no parts were able to parse
                if (valuesCount == values.Count)
                {
                    // If none of the date parts from above match, return 0 results
                    conditions.Add($"1 == @{index}");
                    values.Add(2);
                }
            }
            else if (fieldType == typeof(Boolean))
            {
                filter.@operator = string.IsNullOrEmpty(filter.@operator) ? "==" : filter.@operator;
                if (bool.TryParse(filter.value, out bool value) && IsValidOperator(filter.@operator))
                {
                    conditions.Add($"{table}{field} {filter.@operator} @{index}");
                    values.Add(value);
                }
                else
                {
                    conditions.Add($"{table}{field} == @{index}");
                    values.Add(value);
                }
            }
            else if (fieldType == typeof(Char))
            {
                filter.@operator = string.IsNullOrEmpty(filter.@operator) ? "==" : filter.@operator;
                
                if (string.IsNullOrEmpty(filter.value))
                {
                    continue;
                }
                
                // If more than 1 character passed ensure no results are returned
                if (!string.IsNullOrEmpty(filter.value) && (filter.value.Length > 1))
                {
                    conditions.Add($"1 == @{index}");
                    values.Add(2);
                    continue;
                }

                if (IsValidOperator(filter.@operator))
                {
                    conditions.Add($"{table}{field} {filter.@operator} @{index}");
                    values.Add(filter.value ?? "null");    
                }
            }
            else // if (fieldType == typeof(string)) // Commented for the below TODO notes
            {
                if (filter.@operator == null || filter.@operator.ToLower() == "contains")
                {
                    conditions.Add($"{table}{field}.ToLower().Contains(@{index})");
                    values.Add(filter.value?.ToLower() ?? "null");
                }
                else if (IsValidOperator(filter.@operator))
                {
                    conditions.Add($"{table}{field} {filter.@operator} @{index}");
                    values.Add(filter.value ?? "null");
                }
            }
            // TODO: I'm not sure if we need to handle other types outside of the ones above,
            // TODO: This is commented out because when searches are done on an entity property it should be treated as a string
            // else
            // {
            //     try
            //     {
            //         var converter = TypeDescriptor.GetConverter(fieldType);
            //         var convertedObject = converter.ConvertFromString(filter.value);
            //         if (convertedObject == null)
            //         {
            //             throw new Exception($"Failed to convert {filter.value} to {fieldType.Name}");
            //         }
            //         if (IsValidOperator(filter.@operator))
            //         {
            //             conditions.Add($"{table}{field} {filter.@operator} @{index}");
            //             values.Add(convertedObject);
            //         }
            //         else
            //         {
            //             conditions.Add($"{table}{field} == @{index}");
            //             values.Add(convertedObject);
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         throw new Exception($"Failed to convert {filter.value} to {fieldType.Name}, {ex.Message}");
            //     }
            // }

            index += 1;
        }

        if (conditions.Any())
        {
            filterData.Filter = filterData.Filter.Append($"{string.Join($" {logicalOperator} ", conditions)}");
            filterData.Values = values;
        }

        foreach (var filter in filters)
        {
            if (!string.IsNullOrEmpty(filter.logicalOperator))
            {
                var childFilterData = GetFilters(targetType, query, filter.filters, joins, filter.logicalOperator,
                    index);
                string filterOperator = filterData.Filter.Length > 0 ? $" {logicalOperator} " : "";
                filterData.Filter.Append($" {filterOperator} ({childFilterData.Filter.ToString()})");
                filterData.Values.AddRange(childFilterData.Values);
            }
        }

        return filterData;
    }

    /// <summary>
    /// Parses a numeric filter and constructs the appropriate query condition.
    /// </summary>
    /// <param name="table">The table name</param>
    /// <param name="field">The field name</param>
    /// <param name="index">Parameter index</param>
    /// <param name="filter">Filter criteria</param>
    /// <param name="conditions">List of query conditions</param>
    /// <param name="values">List of parameter values</param>
    /// <param name="type">Type to check and parse against</param>
    /// <returns>True if the type is numeric and was processed; false otherwise</returns>
    private bool ParseNumeric(string table, string field, int index, Filter filter, List<string> conditions,
        List<object> values, Type type)
    {
        // Check if the type is a numeric type
        if (!IsNumericType(type))
        {
            return false;
        }

        filter.@operator = string.IsNullOrEmpty(filter.@operator) ? "==" : filter.@operator;

        bool isValidNumber;
        object? parsedValue;

        // Handle empty value for contains operator early
        if (filter.@operator.ToLower() == "contains")
        {
            if (string.IsNullOrEmpty(filter.value))
            {
                return true; // We've handled this case, even though we didn't add a condition
            }

            conditions.Add($"Convert.ToString({table}{field}).Contains(@{index})");
            values.Add(filter.value);
            return true;
        }

        // Try to parse the string to the appropriate numeric type
        try
        {
            if (type == typeof(int))
            {
                isValidNumber = int.TryParse(filter.value, out int val);
                parsedValue = isValidNumber ? val : 0;
            }
            else if (type == typeof(long) || type == typeof(Int64))
            {
                isValidNumber = long.TryParse(filter.value, out long val);
                parsedValue = isValidNumber ? val : 0L;
            }
            else if (type == typeof(float))
            {
                isValidNumber = float.TryParse(filter.value, out float val);
                parsedValue = isValidNumber ? val : 0f;
            }
            else if (type == typeof(double))
            {
                isValidNumber = double.TryParse(filter.value, out double val);
                parsedValue = isValidNumber ? val : 0d;
            }
            else if (type == typeof(decimal))
            {
                isValidNumber = decimal.TryParse(filter.value, out decimal val);
                parsedValue = isValidNumber ? val : 0m;
            }
            else if (type == typeof(short) || type == typeof(Int16))
            {
                isValidNumber = short.TryParse(filter.value, out short val);
                parsedValue = isValidNumber ? val : 0;
            }
            else if (type == typeof(byte))
            {
                isValidNumber = byte.TryParse(filter.value, out byte val);
                parsedValue = isValidNumber ? val : 0;
            }
            else if (type == typeof(uint))
            {
                isValidNumber = uint.TryParse(filter.value, out uint val);
                parsedValue = isValidNumber ? val : 0U;
            }
            else if (type == typeof(ulong) || type == typeof(UInt64))
            {
                isValidNumber = ulong.TryParse(filter.value, out ulong val);
                parsedValue = isValidNumber ? val : 0UL;
            }
            else if (type == typeof(ushort) || type == typeof(UInt16))
            {
                isValidNumber = ushort.TryParse(filter.value, out ushort val);
                parsedValue = isValidNumber ? val : 0;
            }
            else if (type == typeof(sbyte))
            {
                isValidNumber = sbyte.TryParse(filter.value, out sbyte val);
                parsedValue = isValidNumber ? val : 0;
            }
            else
            {
                // Fallback for any other numeric type
                try
                {
                    parsedValue = Convert.ChangeType(filter.value, type);
                    isValidNumber = true;
                }
                catch
                {
                    parsedValue = Activator.CreateInstance(type); // Default value
                    isValidNumber = false;
                }
            }
        }
        catch
        {
            isValidNumber = false;
            parsedValue = Activator.CreateInstance(type); // Default value for the type
        }

        if (isValidNumber && IsValidOperator(filter.@operator))
        {
            conditions.Add($"{table}{field} {filter.@operator} @{index}");
            values.Add(parsedValue ?? throw new Exception("Failed to convert value"));
        }
        else
        {
            conditions.Add($"{table}{field} == @{index}");
            values.Add(parsedValue ?? throw new Exception("Failed to convert value"));
        }

        return true;
    }
    
    private bool IsNumericType(Type type)
    {
        // Handle nullable numeric types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
        }

        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                return true;
            default:
                return false;
        }
    }


    private void AddOrder(Query query)
    {
        if (_readInput.orderBy == null || _readInput.orderBy.Length == 0)
        {
            return;
        }

        foreach (var orderBy in _readInput.orderBy)
        {
            if (orderBy.field.Contains("."))
            {
                query.OrderBy(orderBy.field.Replace(".", "_"), orderBy.order ?? "asc");
            }
            else
            {
                query.OrderBy($"root_{orderBy.field}", orderBy.order ?? "asc");
            }
        }
    }

    private void AddExcept(Query query)
    {
        if (_readInput.except == null || _readInput.except.Length == 0)
        {
            return;
        }

        foreach (var exclude in _readInput.except)
        {
            query.Except(exclude.fromField, new QueryFactory(_dbContext, _queryOptions, exclude.query).Create());
        }
    }

    private bool IsValidOperator(string? op)
    {
        // Ensure that the operator is valid
        if (!new[] { "==", "!=", ">", ">=", "<", "<=" }.Contains(op))
        {
            throw new Exception($"Invalid operator {op}.");
        }

        return true;
    }

    private static bool Validate(ReadInput readInput)
    {
        
        if (readInput.fields.Length > 0 && readInput.fields[0] != "*")
        {
            foreach (var field in readInput.fields)
            {
                var property = Cache.Instance.GetTableMetadata(readInput.tableName).Type.GetProperty(field);
                // Verify fields exist on all entities
                if (property == null)
                {
                    throw new Exception($"Field {field} does not exist on {readInput.tableName}");
                }

                // Verify fields are not hidden
                if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null)
                {
                    throw new Exception($"Field {field} is hidden on {readInput.tableName}");
                }
            }
        }


        // Make sure at least 1 field has been selected either on the root table or a joined table
        if ((readInput.fields == null || readInput.fields.Length == 0)
            && (readInput.joins == null || readInput.joins.Length == 0 ||
                readInput.joins.FirstOrDefault(x => x.fields.Length > 0) == null))
        {
            throw new Exception($"No fields selected on {readInput.tableName} or any joined tables");
        }


        // Verify * is in the right place
        if (readInput.fields is { Length: > 1 } && readInput.fields.Contains("*"))
        {
            throw new Exception("Wildcard * can only be used to select all fields");
        }

        ValidateFilters(readInput, readInput.filters);

        if (readInput.joins != null)
        {
            // Check for duplicate aliases
            var duplicateAliases = readInput.joins.GroupBy(x => x.alias).Where(g => g.Count() > 1).Select(y => y.Key)
                .ToList();
            if (duplicateAliases.Count > 0)
            {
                throw new Exception($"Duplicate aliases: {string.Join(", ", duplicateAliases)}");
            }

            foreach (var join in readInput.joins)
            {
                // Don't use reserved aliases
                if (!string.IsNullOrEmpty(join.alias))
                {
                    if (join.alias == "system_tu")
                    {
                        throw new Exception($"Cannot use alias system_tu is a reserved alias");
                    }

                    if (join.alias == "system_base")
                    {
                        throw new Exception($"Cannot use alias system_base is a reserved alias");
                    }
                }

                // Validate join type
                if (!string.IsNullOrEmpty(join.type))
                {
                    string joinTypeLower = join.type.ToLower();
                    if (joinTypeLower != "inner" && joinTypeLower != "left")
                    {
                        throw new Exception($"Join type must be 'inner' or 'left', not '{join.type}'");
                    }
                }

                // Verify from table exists in query
                bool isAliasJoin = readInput.joins.FirstOrDefault(x => x != join && x.alias == join.fromTable) != null;
                bool isRootJoin = readInput.tableName == join.fromTable;
                if (!(isAliasJoin || isRootJoin))
                {
                    throw new Exception($"Join from table {join.fromTable} does not exist in query");
                }

                if (isRootJoin && Cache.Instance.GetTableMetadata(join.fromTable).Type.GetProperty(join.fromField) ==
                    null)
                {
                    throw new Exception($"Field {join.fromField} does not exist on {join.fromTable}");
                }

                if (isAliasJoin)
                {
                    // Get aliased table name
                    string? aliasTableName = readInput.joins.FirstOrDefault(x => x.alias == join.fromTable)?.toTable;
                    if (string.IsNullOrEmpty(aliasTableName))
                    {
                        throw new Exception($"Alias {join.fromTable} does not exist in query");
                    }
                }

                Type metadataTable = Cache.Instance.GetTableMetadata(join.toTable).Type;

                if (metadataTable == null)
                {
                    throw new Exception($"Table {join.toTable} does not exist");
                }

                if (metadataTable.GetProperty(join.toField) == null)
                {
                    throw new Exception($"Field {join.toField} does not exist on {join.toTable}");
                }

                if (join.fields.Length > 0 && join.fields[0] == "*")
                {
                    continue;
                }

                if (join.fields.Length > 1 && join.fields.Contains("*"))
                {
                    throw new Exception("Wildcard * can only be used to select all fields");
                }

                foreach (var joinField in join.fields)
                {
                    var joinToTable = Cache.Instance.GetTableMetadata(join.toTable);
                    if (joinToTable == null)
                    {
                        throw new Exception($"Table {join.toTable} does not exist");
                    }

                    var property = joinToTable.Type.GetProperty(joinField);
                    if (property == null)
                    {
                        throw new Exception($"Field {joinField} does not exist on {join.toTable}");
                    }

                    // Verify fields are not hidden
                    if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null)
                    {
                        throw new Exception($"Field {joinField} is hidden on {join.toTable}");
                    }
                }
            }
        }

        if (readInput.except != null)
        {
            foreach (var exclude in readInput.except)
            {
                if (string.IsNullOrEmpty(exclude.fromField))
                {
                    throw new Exception("fromField is required on except");
                }

                if (exclude.query.fields == null || exclude.query.fields.Length == 0)
                {
                    throw new Exception($"except requires a field to be selected on {exclude.query.tableName}");
                }

                if (exclude.query.fields.Length > 1)
                {
                    throw new Exception($"except can only select one field on {exclude.query.tableName}");
                }

                if (exclude.query.fields.Length == 1 && exclude.query.fields[0].Trim() == "*")
                {
                    throw new Exception($"except cannot select all fields on {exclude.query.tableName}");
                }

                if (exclude.query.fields.Length == 1 && string.IsNullOrEmpty(exclude.query.fields[0].Trim()))
                {
                    throw new Exception($"except requires a field to be selected on {exclude.query.tableName}");
                }

                // Recursively validate the except query
                Validate(exclude.query);
            }
        }

        if (readInput.orderBy is { Length: > 0 })
        {
            foreach (var orderBy in readInput.orderBy)
            {
                if (!string.IsNullOrEmpty(orderBy.order) && !new[] { "asc", "desc" }.Contains(orderBy.order.ToLower()))
                {
                    throw new Exception($"OrderBy order must be asc or desc, not {orderBy.order}");
                }

                if (orderBy.field.Contains("."))
                {
                    string alias = orderBy.field.Split(".")[0];
                    var join = readInput.joins?.FirstOrDefault(x => x.alias == alias);
                    if (join == null)
                    {
                        throw new Exception($"Alias {alias} does not exist in query");
                    }

                    if (Cache.Instance.GetTableMetadata(join.toTable).Type.GetProperty(orderBy.field.Split(".")[1]) ==
                        null)
                    {
                        throw new Exception($"Field {orderBy.field} does not exist on {join.toTable}");
                    }
                }
                else if (Cache.Instance.GetTableMetadata(readInput.tableName).Type.GetProperty(orderBy.field) == null)
                {
                    throw new Exception($"Field {orderBy.field} does not exist on {readInput.tableName}");
                }
            }
        }

        return true;
    }

    public static void ValidateFilters(ReadInput readInput, Filter[]? filters)
    {
        if (filters == null)
        {
            return;
        }

        foreach (var filter in filters)
        {
            // Make sure this is either a logical grouping or a filter
            if (!string.IsNullOrEmpty(filter.logicalOperator))
            {
                if (!string.IsNullOrEmpty(filter.field) || !string.IsNullOrEmpty(filter.@operator) ||
                    !string.IsNullOrEmpty(filter.value))
                {
                    throw new Exception(
                        "Filter can only contain a logicalOperator or a field, operator, and value");
                }

                if (filter.logicalOperator is not ("AND" or "OR"))
                {
                    throw new Exception($"Logical Operator can only be AND or OR, not {filter.logicalOperator}");
                }
            }

            if (string.IsNullOrEmpty(filter.field))
            {
                continue;
            }

            // If this is a filter on a joined table
            if (filter.field.Contains('.'))
            {
                string alias = filter.field.Split('.')[0];
                string field = filter.field.Split('.')[1];
                var join = readInput.joins?.FirstOrDefault(x => x.alias == alias);
                if (join == null)
                {
                    throw new Exception($"Join {alias} does not exist");
                }

                // Get the type of the joined table
                Type? joinType = Cache.Instance.GetTableMetadata(join.toTable).Type;
                if (joinType == null)
                {
                    throw new Exception($"Table {join.toTable} does not exist");
                }

                // Get the type of the field on the joined table
                var property = joinType.GetProperty(field);
                if (property == null)
                {
                    throw new Exception($"Field {field} does not exist on {join.toTable}");
                }

                // If the field is hidden and none-queryable throw an exception
                if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) is UIHideAttribute
                    {
                        Queryable: false
                    })
                {
                    throw new Exception($"Field {field} is hidden on {join.toTable}");
                }
            }
            else
            {
                var property = Cache.Instance.GetTableMetadata(readInput.tableName).Type.GetProperty(filter.field);
                if (property == null)
                {
                    throw new Exception($"Field {filter.field} does not exist on {readInput.tableName}");
                }

                // If the field is hidden and none-queryable throw an exception
                if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) is UIHideAttribute
                    {
                        Queryable: false
                    })
                {
                    throw new Exception($"Field {filter.field} is hidden on {readInput.tableName}");
                }
            }

            if (filter.filters != null)
            {
                ValidateFilters(readInput, filter.filters);
            }
        }
    }

    public class QueryOptions
    {
        public required Guid UserId { get; init; }
        public required string[] Permissions { get; init; }
    }

    private class FilterData
    {
        public StringBuilder Filter { get; set; } = new();
        public List<object> Values { get; set; } = new();
    }
}