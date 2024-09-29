using System.Reflection;
using ClosedXML.Excel;
using Xams.Core.Attributes;
using Xams.Core.Dtos;

namespace Xams.Core.Actions.Shared;

public static class TABLE_ImportExport
{
    public static List<PropertyInfo> GetTableProperties(Type type)
    {
        var properties = type.GetProperties();
        string[] ignoreFields = {"CreatedDate", "UpdatedDate", "CreatedById", "CreatedBy", "UpdatedById", "UpdatedBy"};
        List<PropertyInfo> propertyInfos = new List<PropertyInfo>();
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute(typeof(UIHideAttribute)) != null)
            {
                continue;
            }
            
            if (ignoreFields.Contains(property.Name))
            {
                continue;
            }
            
            propertyInfos.Add(property);
        }

        return propertyInfos;
    }
    
    public static Response<object?> WorkbookResult(XLWorkbook workbook, string tableName)
    {
        MemoryStream stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return new Response<object?>
        {
            Succeeded = true,
            Data = new FileData()
            {
                FileName = $"import_{tableName}.xlsx",
                Stream = stream,
                ContentType = "application/octet-stream"
            },
            ResponseType = ResponseType.File
        };
    }
}