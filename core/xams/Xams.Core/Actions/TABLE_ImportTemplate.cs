using ClosedXML.Excel;
using Xams.Core.Actions.Shared;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Actions;

[ServiceAction(nameof(TABLE_ImportTemplate))]
public class TABLE_ImportTemplate : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (context.Parameters == null)
        {
            return ServiceResult.Error($"Data Import requires parameters");
        }
        
        if (!context.Parameters.ContainsKey("tableName") || string.IsNullOrEmpty(context.Parameters["tableName"].GetString()))
        {
            return ServiceResult.Error($"Data Import requires a table name");
        }
        
        string tableName = context.Parameters["tableName"].GetString()!;
        var db = context.DataRepository.GetDbContext<BaseDbContext>();
        var dbContextType = Cache.Instance.GetTableMetadata(tableName);

        if (dbContextType == null)
        {
            return ServiceResult.Error($"Could not find table {tableName}");
        }
        
        var propertyInfos = TABLE_ImportExport.GetTableProperties(dbContextType.Type);
        
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(tableName);
            var row1 = worksheet.Row(1);
            for (int i = 0; i < propertyInfos.Count; i++)
            {
                row1.Cell(i + 1).Value = propertyInfos[i].Name;
            }
            
            return TABLE_ImportExport.WorkbookResult(workbook, tableName);
        }
    }
}