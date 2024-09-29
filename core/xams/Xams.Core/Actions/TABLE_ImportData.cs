using System.Linq.Dynamic.Core;
using System.Reflection;
using ClosedXML.Excel;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Actions;

[ServiceAction("TABLE_ImportData")]
public class TABLE_ImportData : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (!context.Parameters.ContainsKey("tableName") || string.IsNullOrEmpty(context.Parameters["tableName"].GetString()))
        {
            return ServiceResult.Error($"Data Import requires a table name");
        }

        if (!context.Parameters.ContainsKey("operation") || string.IsNullOrEmpty(context.Parameters["operation"].GetString()))
        {
            return ServiceResult.Error($"Data Import requires an operation");
        }
        
        var db = context.DataRepository.GetDbContext<BaseDbContext>();
        string tableName = context.Parameters["tableName"].GetString()!;
        string operation = context.Parameters["operation"].GetString()!;
        
        // Verify permissions
        string[] permissions = await Permissions.GetUserPermissions(db, context.ExecutingUserId, [$"TABLE_{tableName}_IMPORT"
        ]);

        if (permissions.Length == 0)
        {
            return ServiceResult.Error($"Missing import permissions for {tableName}");
        }

        if (string.IsNullOrEmpty(operation))
        {
            return ServiceResult.Error($"Data Import requires an operation");
        }
        
        string[] operations = operation.Split(',');
        
        var tableMetadata = Cache.Instance.GetTableMetadata(tableName);

        if (tableMetadata == null)
        {
            return ServiceResult.Error($"Table {tableName} not found");
        }
        
        Type tableType = tableMetadata.Type;
        
        using var workbook = new XLWorkbook(context.File.OpenReadStream());
        
        if (!workbook.Worksheets.Any())
        {
            return ServiceResult.Error("No worksheets found in file");
        }
        
        var worksheet = workbook.Worksheets.First();
        var headerRow = worksheet.Row(1);
        List<string> headers = new();
        
        int primaryKeyColumn = -1;

        // Verify table has fields for all headers
        foreach (var headerCell in headerRow.Cells(true))
        {
            if (string.IsNullOrEmpty(headerCell.GetString()))
            {
                return ServiceResult.Error($"No header found for column {headerCell.Address}");
            }
            
            string headerName = headerCell.GetString().Trim();
            if (headerName == tableMetadata.PrimaryKey)
            {
                primaryKeyColumn = headerCell.Address.ColumnNumber;
            }
            
            var property = tableType.GetProperty(headerName);
            if (property == null)
            {
                return ServiceResult.Error($"Field {headerName} not found in table {tableName}");
            }
            
            headers.Add(headerName);
        }
        
        // If this is only and update and there's no primary key column, return an error
        if (primaryKeyColumn == -1 && operations.Contains("update") && operations.Length == 1)
        {
            return ServiceResult.Error($"Import missing primary key column {tableName}Id.");
        }
        
        // Validate the integrity of the data
        List<string> errors = new List<string>();
        List<RecordImport> recordImports = new List<RecordImport>();
        foreach (var row in worksheet.Rows(2, worksheet.LastRowUsed().RowNumber()))
        {
            object? record = null;
            // Is this a create or update?
            ImportType importType = ImportType.Create;
            if (primaryKeyColumn != -1)
            {
                string primaryKeyValue = row.Cell(primaryKeyColumn).GetString();
                if (!string.IsNullOrEmpty(primaryKeyValue))
                {
                    Guid primaryKeyGuid = Guid.Empty;
                    if (!Guid.TryParse(primaryKeyValue, out primaryKeyGuid))
                    {
                        errors.Add($"Invalid primary key value {primaryKeyValue} in row {row.RowNumber()}, column {row.Cell(primaryKeyColumn).Address}");
                        continue;
                    }
                    DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(db, tableType);
                    record = (await dynamicLinq.Query.Where($"{tableMetadata.PrimaryKey} == @0", primaryKeyGuid)
                        .ToDynamicArrayAsync()).FirstOrDefault();
                    if (record != null)
                    {
                        importType = ImportType.Update;
                    }
                }
                else
                {
                    // If this is only and update and there's no primary key column, return an error
                    if (operations.Contains("update") && operations.Length == 1)
                    {
                        errors.Add($"Row {row.RowNumber()}: {tableName}Id is required for updates.");
                        continue;
                    }
                }
            }
            
            // Populate all of the fields
            Dictionary<string, dynamic> fields = new();
            foreach (var cell in row.Cells(1, headerRow.LastCellUsed().Address.ColumnNumber))
            {
                if (cell.Address.ColumnNumber == primaryKeyColumn)
                {
                    continue;
                }
                
                string value = cell.GetString();
                if (string.IsNullOrEmpty(value))
                {
                    continue;
                }
                
                string headerName = headerRow.Cell(cell.Address.ColumnNumber).GetString().Trim();
                var property = tableType.GetProperty(headerName);
                if (property.GetCustomAttribute(typeof(UIRequiredAttribute)) != null && string.IsNullOrEmpty(value))
                {
                    errors.Add($"Row {row.RowNumber()}, column {cell.Address}: {headerName} is required");
                    continue;
                }
                
                if (property.GetCustomAttribute(typeof(UIHideAttribute)) != null && !string.IsNullOrEmpty(value))
                {
                    errors.Add($"Row {row.RowNumber()}, column {cell.Address}: {headerName} cannot be set from the UI");
                    continue;
                }


                bool isIdLookupField = headerName.Length > 2 && headerName.EndsWith("Id") 
                                                             && tableType.GetProperty(headerName.Substring(0, headerName.Length - 2)) != null
                                                             && (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?));
                
                // If this is a lookup Id field, but a Guid wasn't provided, continue
                Guid lookupId = Guid.Empty;
                if (isIdLookupField && !Guid.TryParse(value, out lookupId))
                {
                    if (!string.IsNullOrEmpty(value))
                    {
                        errors.Add($"Row {row.RowNumber()}, column {cell.Address.ColumnLetter}: {headerName} must be a valid Guid");
                    }
                    continue;
                }

                // If this is a lookup Id field and we have a Guid, verify the record exists
                if (isIdLookupField && lookupId != Guid.Empty)
                {
                    string namePropertyName = headerName.Substring(0, headerName.Length - 2);
                    var nameProperty = tableType.GetProperty(namePropertyName);
                    if (nameProperty != null)
                    {
                        Type? lookupType = Nullable.GetUnderlyingType(nameProperty.PropertyType);
                        if (lookupType == null)
                        {
                            lookupType = nameProperty.PropertyType;
                        }
                        
                        DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(db, lookupType);
                        var lookupRecord = (await dynamicLinq.Query.Where($"{lookupType.Name}Id == @0", lookupId)
                            .ToDynamicListAsync()).FirstOrDefault();
                        if (lookupRecord == null)
                        {
                            errors.Add($"Row {row.RowNumber()}, column {cell.Address.ColumnLetter}: {headerName} with Id {lookupId} not found");
                            continue;
                        }
                    }
                }

                string idPropertyName = $"{headerName}Id";
                var idProperty = tableType.GetProperty(idPropertyName);
                bool isNameLookupField = idProperty != null && (idProperty.PropertyType == typeof(Guid) || idProperty.PropertyType == typeof(Guid?));
                // If this is a lookup name field, check if an id was provided
                if (isNameLookupField && headers.Contains(idPropertyName))
                {
                    // If an id was provided, continue, otherwise, query for the id
                    var idCellValue = row.Cell(headers.IndexOf(idPropertyName) + 1).GetString();
                    if (!string.IsNullOrEmpty(idCellValue) && Guid.TryParse(idCellValue, out _))
                    {
                        continue;
                    }

                    Type? lookupType = Nullable.GetUnderlyingType(property.PropertyType);
                    if (lookupType == null)
                    {
                        lookupType = property.PropertyType;
                    }

                    string lookupNameField = string.Empty;
                    var lookupNameProperty = lookupType.GetProperties()
                        .FirstOrDefault(x => x.GetCustomAttribute(typeof(UINameAttribute)) != null);
                    var lookupPrimaryKeyProperty = lookupType.GetProperty($"{lookupType.Name}Id");

                    if (lookupNameProperty != null)
                    {
                        lookupNameField = lookupNameProperty.Name;
                    }

                    if (string.IsNullOrEmpty(lookupNameField) && lookupType.GetProperty("Name") != null)
                    {
                        lookupNameField = "Name";
                    }
                    
                    // If the lookup name field can't be resolved, continue
                    if (string.IsNullOrEmpty(lookupNameField))
                    {
                        continue;
                    }
        
                    // If this is an "Option" add the option filter
                    UIOptionAttribute? uiOptionAttribute = idProperty.GetCustomAttribute(typeof(UIOptionAttribute)) as UIOptionAttribute;
                    
                    DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(db, lookupType);
                    IQueryable query = dynamicLinq.Query;
                    query = query.Where($"{lookupNameField} == @0", value);
                    if (uiOptionAttribute != null)
                    {
                        query = query.Where($"Name == @0", uiOptionAttribute.Name);
                    }
                    var lookupRecord = (await query.ToDynamicListAsync()).FirstOrDefault();
                    if (lookupRecord != null)
                    {
                        lookupId = lookupPrimaryKeyProperty.GetValue(lookupRecord);
                        if (lookupId != null)
                        {
                            fields.Add(idPropertyName, lookupId.ToString());
                            continue;
                        }
                    }
                    
                    errors.Add($"Row {cell.Address.RowNumber}: {lookupType.Name} with name {value} not found");
                    continue;
                }
                
                
                fields.Add(headerRow.Cell(cell.Address.ColumnNumber).GetString(), value);    
            }

            if (importType is ImportType.Create)
            {
                var mapEntityResult = EntityUtil.ConvertToEntity(tableType, fields);
                if (mapEntityResult.Error)
                {
                    errors.Add($"Row {row.RowNumber()}: {mapEntityResult.Message}");
                    continue;
                }

                record = mapEntityResult.Entity;
            }
            else if (importType is ImportType.Update)
            {
                var mapEntityResult = EntityUtil.MergeFields(record, fields);
                if (mapEntityResult.Error)
                {
                    errors.Add($"Row {row.RowNumber()}: {mapEntityResult.Message}");
                    continue;
                }

                record = mapEntityResult.Entity;
            }
            
            recordImports.Add(new RecordImport()
            {
                ImportType = importType,
                Record = record,
                RowNumber = row.RowNumber()
            });
        }

        if (errors.Count > 0)
        {
            return ServiceResult.Error(new
            {
                errors,
            });
        }

        foreach (var recordImport in recordImports)
        {
            Response<object?>? response = null;
            if (recordImport.ImportType is ImportType.Create && operations.Contains("create"))
            {
                response = await context.DataService.Create(context.ExecutingUserId, recordImport.Record);
            }
            else if (recordImport.ImportType is ImportType.Update && operations.Contains("update"))
            {
                response = await context.DataService.Update(context.ExecutingUserId, recordImport.Record);
            }

            // If the response is null, continue (ie: we're only processing creates or updates)
            if (response == null)
            {
                continue;
            }
            
            if (!response.Succeeded)
            {
                errors.Add($"Row {recordImport.RowNumber}: {response?.FriendlyMessage ?? "An error occurred while importing data"}");
            }
        }
        
        if (errors.Count > 0)
        {
            return ServiceResult.Error(new
            {
                errors,
            });
        }

        return ServiceResult.Success();
    }

    public enum ImportType
    {
        Create,
        Update,
    }
    public class RecordImport
    {
        public required ImportType ImportType { get; init; }
        public required object? Record { get; init; }
        public required int RowNumber { get; init; }
    }
}
