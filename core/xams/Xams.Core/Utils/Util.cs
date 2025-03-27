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
    
    /// <summary>
    /// Converts an array of objects into their respective primitive key types as dynamic objects.
    /// Supports all common Entity Framework primary key types.
    /// </summary>
    /// <param name="values">Array of objects to convert to appropriate primary key types</param>
    /// <param name="targetType">Optional explicit type to convert to. If null, best type is inferred.</param>
    /// <returns>Array of dynamic objects converted to appropriate primary key types</returns>
    public static dynamic[] ConvertToPrimaryKeyTypes(object[] values, Type targetType)
    {
        var result = new dynamic[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            try
            {
                if (targetType == typeof(int) || targetType == typeof(Int32))
                    result[i] = Convert.ToInt32(values[i]);
                else if (targetType == typeof(long) || targetType == typeof(Int64))
                    result[i] = Convert.ToInt64(values[i]);
                else if (targetType == typeof(short) || targetType == typeof(Int16))
                    result[i] = Convert.ToInt16(values[i]);
                else if (targetType == typeof(byte))
                    result[i] = Convert.ToByte(values[i]);
                else if (targetType == typeof(uint) || targetType == typeof(UInt32))
                    result[i] = Convert.ToUInt32(values[i]);
                else if (targetType == typeof(ulong) || targetType == typeof(UInt64))
                    result[i] = Convert.ToUInt64(values[i]);
                else if (targetType == typeof(ushort) || targetType == typeof(UInt16))
                    result[i] = Convert.ToUInt16(values[i]);
                else if (targetType == typeof(sbyte))
                    result[i] = Convert.ToSByte(values[i]);
                else if (targetType == typeof(decimal))
                    result[i] = Convert.ToDecimal(values[i]);
                else if (targetType == typeof(string))
                    result[i] = values[i].ToString();
                else if (targetType == typeof(Guid))
                {
                    if (values[i] is string guidString)
                        result[i] = Guid.Parse(guidString);
                    else
                        result[i] = (Guid)values[i];
                }
                else if (targetType == typeof(DateTime))
                    result[i] = Convert.ToDateTime(values[i]);
                else if (targetType == typeof(DateTimeOffset))
                {
                    if (values[i] is DateTime dt)
                        result[i] = new DateTimeOffset(dt);
                    else
                        result[i] = (DateTimeOffset)values[i];
                }
                else if (targetType == typeof(TimeSpan))
                {
                    if (values[i] is string timeString)
                        result[i] = TimeSpan.Parse(timeString);
                    else
                        result[i] = (TimeSpan)values[i];
                }
                else if (targetType == typeof(byte[]))
                {
                    if (values[i] is string base64String)
                        result[i] = Convert.FromBase64String(base64String);
                    else
                        result[i] = (byte[])values[i];
                }
                else if (targetType.IsEnum)
                {
                    if (values[i] is string enumString)
                        result[i] = Enum.Parse(targetType, enumString);
                    else
                        result[i] = Enum.ToObject(targetType, values[i]);
                }
                else
                {
                    // Default case, try direct casting
                    result[i] = Convert.ChangeType(values[i], targetType);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Cannot convert value '{values[i]}' to type '{targetType.Name}'", ex);
            }
        }

        return result;
    }
}