using System.Dynamic;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Utils;

public static class UIInfoUtil
{
    public static void SetUIInfo(this ReadOutput readOutput, bool canDelete, bool canUpdate, bool canAssign)
    {
        if (readOutput.results != null)
        {
            List<object> newResults = new();
            foreach (var entity in readOutput.results)
            {
                dynamic expandoObject = new ExpandoObject();  
                IDictionary<string, object?> expandoDictionary = ((IDictionary<string, object?>)expandoObject);
                foreach (var property in entity.GetType().GetProperties())
                {
                    if (property.Name == "Item")
                        continue;
                    expandoDictionary[property.Name] = property.GetValue(entity);
                }

                expandoObject._ui_info_ = new
                {
                    canDelete, canUpdate, canAssign
                };
                
                newResults.Add(expandoObject);
            }

            readOutput.results = newResults;
        }
    }

    public static bool IsUIInfoSet(this ReadOutput readOutput)
    {
        foreach (var entity in readOutput.results)
        {
            if (entity is ExpandoObject)
            {
                IDictionary<string, object?> expandoDictionary = ((IDictionary<string, object?>)entity);
                if (!expandoDictionary.ContainsKey("_ui_info_"))
                    return false;
                
                var uiInfo = expandoDictionary["_ui_info_"];
                if (uiInfo == null)
                    return false;
                
                var canDeleteProp = uiInfo.GetType().GetProperty("canDelete");
                var canUpdateProp = uiInfo.GetType().GetProperty("canUpdate");
                var canAssignProp = uiInfo.GetType().GetProperty("canAssign");
                
                if (canDeleteProp == null || canUpdateProp == null || canAssignProp == null)
                    return false;
                
                var canDelete = canDeleteProp.GetValue(uiInfo);
                var canUpdate = canUpdateProp.GetValue(uiInfo);
                var canAssign = canAssignProp.GetValue(uiInfo);
                
                if (canDelete == null || canUpdate == null || canAssign == null)
                    return false;
            }
            else
            {
                var uiInfoProperty = entity.GetType().GetProperty("_ui_info_");
                if (uiInfoProperty == null)
                    return false;
            
                var canDeleteProp = uiInfoProperty.PropertyType.GetProperty("canDelete");
                var canUpdateProp = uiInfoProperty.PropertyType.GetProperty("canUpdate");
                var canAssignProp = uiInfoProperty.PropertyType.GetProperty("canAssign");
            
                if (canDeleteProp == null || canUpdateProp == null || canAssignProp == null)
                    return false;
            
                var canDelete = canDeleteProp.GetValue(uiInfoProperty.GetValue(entity));
                var canUpdate = canUpdateProp.GetValue(uiInfoProperty.GetValue(entity));
                var canAssign = canAssignProp.GetValue(uiInfoProperty.GetValue(entity));
            
                if (canDelete == null || canUpdate == null || canAssign == null)
                    return false;
            }
        }

        return true;
    }
}