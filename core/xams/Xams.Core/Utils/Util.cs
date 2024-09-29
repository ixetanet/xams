using System.Text.Json;

namespace Xams.Core.Utils;

internal static class Util
{
    public static Dictionary<string, JsonElement> ObjectToParameters(object? o)
    {
        if (o == null)
        {
            return new();
        }
        
        string jsonString = JsonSerializer.Serialize(o);
        using JsonDocument doc = JsonDocument.Parse(jsonString);
        var dictionary = new Dictionary<string, JsonElement>();
        
        if (doc.RootElement.ValueKind == JsonValueKind.Null)
        {
            return dictionary;
        }
        
        foreach (var property in doc.RootElement.EnumerateObject())
        {
            dictionary.Add(property.Name, property.Value.Clone());
        }

        return dictionary;
    }
    
    public static Dictionary<string, JsonElement> MergeParameters(Dictionary<string, JsonElement> source, Dictionary<string, JsonElement> target)
    {
        foreach (var (key, value) in source)
        {
            target[key] = value;
        }

        return target;
    }
}