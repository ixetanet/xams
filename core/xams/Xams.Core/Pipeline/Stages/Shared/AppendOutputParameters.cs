using System.Dynamic;

namespace Xams.Core.Pipeline.Stages.Shared;

public static class AppendOutputParameters
{
    public static List<object> Set(PipelineContext context, List<object> data)
    {
        List<object> results = new();
        if (data.Count == 0)
        {
            return results;
        }

        var first = data.First();

        // If it's a list of expando objects
        if (first is ExpandoObject)
        {
            foreach (var entity in data)
            {
                ((dynamic)entity)._parameters_ = context.OutputParameters;
                results.Add(entity);
            }

            return results;
        }
        
        // If it's a list of c# classes
        foreach (var entity in data)
        {
            dynamic expandoObject = new ExpandoObject();
            IDictionary<string, object?> expandoDictionary = ((IDictionary<string, object?>)expandoObject);
            foreach (var property in entity.GetType().GetProperties())
            {
                if (property.Name == "Item")
                    continue;
                expandoDictionary[property.Name] = property.GetValue(entity);
            }
            expandoObject._parameters_ = context.OutputParameters;
            results.Add(expandoObject);
        }

        return results;
    }
}