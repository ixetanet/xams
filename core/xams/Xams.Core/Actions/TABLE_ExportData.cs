using System.Text.Json;
using ClosedXML.Excel;
using Xams.Core.Actions.Shared;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Actions;

[ServiceAction("TABLE_ExportData")]
public class TABLE_ExportData : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (context.Parameters == null)
        {
            return ServiceResult.Error($"Data Export requires parameters");
        }
        
        if (!context.Parameters.ContainsKey("query") || string.IsNullOrEmpty(context.Parameters["query"].GetRawText()))
        {
            return ServiceResult.Error($"Data Export requires a query");
        }
        
        var db = context.DataRepository.GetDbContext<BaseDbContext>();

        string query = context.Parameters["query"].GetRawText()!;
        
        // Verify valid query
        ReadInput? readInput;
        string tableName;
        try
        {
             readInput = JsonSerializer.Deserialize<ReadInput>(query);
             if (readInput == null)
             {
                 return ServiceResult.Error($"Invalid query");
             }

             tableName = readInput.tableName;
        }
        catch (Exception e)
        {
            return ServiceResult.Error($"Invalid query: {e.Message}");
        }
        
        // Verify permissions
        string[] permissions = await PermissionCache.GetUserPermissions(context.ExecutingUserId, new []{ $"TABLE_{tableName}_EXPORT" });

        if (permissions.Length == 0)
        {
            return ServiceResult.Error($"Missing export permissions for {tableName}");
        }
        
        var tableMetadata = Cache.Instance.GetTableMetadata(tableName);

        if (tableMetadata == null)
        {
            return ServiceResult.Error($"Table {tableName} not found");
        }
        
        Type tableType = tableMetadata.Type;
        
        var propertyInfos = TABLE_ImportExport.GetTableProperties(tableType);

        using (var workbook = new XLWorkbook())
        {
            // Get Data
            Response<ReadOutput> readResponse = await context.DataService.Read(context.ExecutingUserId, readInput);
            if (!readResponse.Succeeded)
            {
                return ServiceResult.Error(readResponse.FriendlyMessage ?? "Error reading data");
            }
            
            ReadOutput readOutput = readResponse.Data!;
            
            var worksheet = workbook.Worksheets.Add(tableName);
            
            // Create headers
            var row1 = worksheet.Row(1);
            for (int i = 0; i < propertyInfos.Count; i++)
            {
                row1.Cell(i + 1).Value = propertyInfos[i].Name;
            }
            
            // Populate data
            for (int i = 0; i < readOutput.results.Count; i++)
            {
                var record = (IDictionary<string, object?>)readOutput.results[i];
                var row = worksheet.Row(i + 2);
                for (int j = 0; j < propertyInfos.Count; j++)
                {
                    var propertyInfo = propertyInfos[j];
                    var value = record[propertyInfo.Name];
                    var cell = row.Cell(j + 1); 
                    if (value != null)
                    {
                        if (value is string)
                        {
                            cell.Value = (string)value;
                        }
                        else if (value is Guid)
                        {
                            cell.Value = value.ToString();
                        }
                        else if (value is DateTime)
                        {
                            cell.Value = value.ToString();
                        }
                        else if (value is bool)
                        {
                            cell.Value = (bool)value;    
                        }
                        else if (value is byte)
                        {
                            cell.Value = (byte)value;
                        } 
                        else if (value is sbyte)
                        {
                            cell.Value = (sbyte)value;
                        }
                        else if (value is short)
                        {
                            cell.Value = (short)value;
                        }
                        else if (value is ushort)
                        {
                            cell.Value = (ushort)value;
                        }
                        else if (value is int)
                        {
                            cell.Value = (int)value;
                        }
                        else if (value is uint)
                        {
                            cell.Value = (uint)value;
                        }
                        else if (value is nint)
                        {
                            cell.Value = (nint)value;
                        }
                        else if (value is nuint)
                        {
                            cell.Value = (nuint)value;
                        }
                        else if (value is long)
                        {
                            cell.Value = (long)value;
                        }
                        else if (value is ulong)
                        {
                            cell.Value = (ulong)value;
                        }
                        else if (value is float)
                        {
                            float fValue = (float)value;
                            if (float.IsInfinity(fValue) || float.IsNaN(fValue))
                            {
                                cell.Value = value.ToString();
                            }
                            else
                            {
                                cell.Value = (float)value;   
                            }
                        }
                        else if (value is double)
                        {
                            double dValue = (double)value;
                            if (double.IsInfinity(dValue) || double.IsNaN(dValue))
                            {
                                cell.Value = value.ToString();
                            }
                            else
                            {
                                cell.Value = (double)value;   
                            }
                        }
                        else if (value is decimal)
                        {
                            cell.Value = (decimal)value;
                        }
                        else if (value is char)
                        {
                            cell.Value = (char)value;
                        }
                        
                    }
                }
            }
            return TABLE_ImportExport.WorkbookResult(workbook, tableName);
        }
    }
}