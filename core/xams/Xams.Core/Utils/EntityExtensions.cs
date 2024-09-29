using System.Collections.Concurrent;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using Xams.Core.Attributes;

namespace Xams.Core.Utils;

internal static class EntityExtensions
{
    public static ExtensionUtil EntityExt(this object obj)
    {
        return new ExtensionUtil(obj);
    }

    public static object? GetValue(this object obj, string fieldName)
    {
        PropertyInfo? property = obj.GetType().GetProperty(fieldName);
        if (property == null)
        {
            throw new Exception($"Property {fieldName} not found on {obj.GetType().Name}");
        }

        return property.GetValue(obj);
    }
    
    
    // Optimized for performance
    private static readonly ConcurrentDictionary<(Type, string), Delegate> PropertyCache = new();
    public static T GetValue<T>(this object obj, string fieldName)
    {
        if (obj is ExpandoObject)
        {
            var dict = (IDictionary<string, object>)obj;
            if (dict.TryGetValue(fieldName, out var value))
            {
                return (T)value;
            }

            throw new Exception($"Property {fieldName} not found on {obj.GetType().Name}");
        }
        
        var key = (obj.GetType(), fieldName);
        
        if (!PropertyCache.TryGetValue(key, out var accessor))
        {
            var propertyInfo = obj.GetType().GetProperty(fieldName);
            if (propertyInfo == null || propertyInfo.PropertyType != typeof(T))
            {
                throw new Exception($"Property {fieldName} not found or type mismatch on {obj.GetType().Name}");
            }

            var parameter = Expression.Parameter(typeof(object), "instance");
            var castParameter = Expression.Convert(parameter, obj.GetType());
            var propertyAccess = Expression.Property(castParameter, propertyInfo);
            var castResult = Expression.Convert(propertyAccess, typeof(object));
            var lambda = Expression.Lambda<Func<object, object>>(castResult, parameter);

            accessor = lambda.Compile();
            PropertyCache[key] = accessor;
        }

        return (T)((Func<object, object>)accessor)(obj);
    }
    
    public static bool HasField(this object obj, string fieldName)
    {
        if (obj is ExpandoObject)
        {
            var dict = (IDictionary<string, object>)obj;
            return dict.ContainsKey(fieldName);
        }
        
        return obj.GetType().GetProperty(fieldName) != null;
    }
    
    public static Guid GetIdValue(this object obj, Type type)
    {
        if (obj is ExpandoObject)
        {
            if (obj is not IDictionary<string, object> dict)
            {
                throw new Exception($"Property {obj.GetType().Name}Id not found on {obj.GetType().Name}");
            }

            if (dict[$"{type.Name}Id"] is not Guid expandoValue)
            {
                throw new Exception($"Property {obj.GetType().Name}Id is null on {obj.GetType().Name}");
            }

            return expandoValue;
        }
        
        PropertyInfo? property = obj.GetType().GetProperty($"{obj.GetType().Name}Id");
        if (property == null)
        {
            throw new Exception($"Property {obj.GetType().Name}Id not found on {obj.GetType().Name}");
        }

        var value = property.GetValue(obj);

        if (value == null)
        {
            throw new Exception($"Property {obj.GetType().Name}Id is null on {obj.GetType().Name}");
        }

        return (Guid)value;
    }

    public static void SetValue(this object obj, string fieldName, object? value)
    {
        PropertyInfo? property = obj.GetType().GetProperty(fieldName);
        if (property == null)
        {
            throw new Exception($"Property {fieldName} not found on {obj.GetType().Name}");
        }
        property.SetValue(obj, value);
    }
    
    public static string? GetNameFieldValue(this object obj, Type type)
    {
        var nameProperty = Cache.Instance.GetTableMetadata(type.Name).NameProperty;

        if (nameProperty == null)
        {
            return string.Empty;
        }
        
        if (obj is ExpandoObject)
        {
            if (obj is not IDictionary<string, object> dict)
            {
                return string.Empty;
            }

            if (dict[nameProperty.Name] is not string expandoValue)
            {
                return string.Empty;
            }

            return expandoValue;
        }
        
        var value = nameProperty.GetValue(obj);
        
        return value as string;
    }
    
    /// <summary>
    /// Take a fieldNameId field and returns true if it's a lookup field.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public static bool IsLookupField(this Type type, string fieldName)
    {
        var idProperty = type.GetProperty(fieldName);
        
        if (idProperty == null)
        {
            return false;
        }
        
        var idPropertyType = idProperty.PropertyType;
        idPropertyType = Nullable.GetUnderlyingType(idPropertyType) ?? idPropertyType;

        if (idPropertyType != typeof(Guid))
        {
            return false;
        }
        
        if (fieldName.Length <= 2)
        {
            return false;
        }

        var propertyInfo = type.GetProperty(fieldName.Substring(0, fieldName.Length - 2));

        if (propertyInfo == null)
        {
            return false;
        }

        Type propertyType = propertyInfo.PropertyType;
        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (propertyType.IsPrimitive
            || propertyType == typeof(Guid)
            || propertyType == typeof(string)
            || propertyType == typeof(decimal)
            || propertyType == typeof(DateTime))
        {
            return false;
        }

        return true;
    }

    public static bool IsPrimitive(this PropertyInfo propertyInfo)
    {
        Type propertyType = propertyInfo.PropertyType;
        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (propertyType.IsPrimitive
            || propertyType == typeof(Guid)
            || propertyType == typeof(string)
            || propertyType == typeof(decimal)
            || propertyType == typeof(DateTime))
        {
            return true;
        }

        return false;
    }
    
    /// <summary>
    /// Take a fieldNameId field and returns the type of the lookup entity.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    public static Type? GetLookupType(this Type type, string fieldName)
    {
        var idProperty = type.GetProperty(fieldName);
        if (idProperty == null)
        {
            return null;
        }
        
        var idPropertyType = idProperty.PropertyType;
        idPropertyType = Nullable.GetUnderlyingType(idPropertyType) ?? idPropertyType;

        if (idPropertyType != typeof(Guid))
        {
            return null;
        }
        
        if (fieldName.Length <= 2)
        {
            return null;
        }

        var propertyInfo = type.GetProperty(fieldName.Substring(0, fieldName.Length - 2));

        if (propertyInfo == null)
        {
            return null;
        }

        Type propertyType = propertyInfo.PropertyType;
        propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (propertyType.IsPrimitive
            || propertyType == typeof(Guid)
            || propertyType == typeof(string)
            || propertyType == typeof(decimal)
            || propertyType == typeof(DateTime))
        {
            return null;
        }

        return propertyType;
    }

    public static bool IsValidEntityProperty(this PropertyInfo propertyInfo)
    {
        Type propertyType = propertyInfo.PropertyType;
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            return false;
        }
        return true;
    }
    public static Type GetUnderlyingType(this PropertyInfo propertyInfo)
    {
        return Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
    }
    public static List<PropertyInfo> GetEntityProperties(this Type type)
    {
        List<PropertyInfo> result = new List<PropertyInfo>();
        foreach (var property in type.GetProperties())
        {
            if (property.Name == "Item")
                continue;
            
            if (!property.IsValidEntityProperty())
            {
                continue;
            }
            
            result.Add(property);
        }

        return result;
    }
}

internal class ExtensionUtil
{
    private object _obj;

    public ExtensionUtil(object obj)
    {
        _obj = obj;
    }

    public List<UiSetFieldFromLookupInfo> GetUISetFieldFromLookupInfo()
    {
        List<UiSetFieldFromLookupInfo> lookupInfos = new List<UiSetFieldFromLookupInfo>();
        var properties = _obj.GetType().GetProperties();
        foreach (var property in properties)
        {
            UISetFieldFromLookupAttribute? uiSetStringFieldAttribute =
                property.GetCustomAttribute<UISetFieldFromLookupAttribute>();

            if (uiSetStringFieldAttribute != null)
            {
                var lookupIdProperty = properties
                    .FirstOrDefault(x => x.Name == uiSetStringFieldAttribute.LookupIdProperty);

                if (lookupIdProperty == null)
                {
                    continue;
                }

                // If it's not a xxId field, skip
                if (lookupIdProperty.Name.Length <= 2)
                {
                    continue;
                }

                Guid? value = (Guid?)lookupIdProperty.GetValue(_obj);

                if (value == null)
                {
                    continue;
                }

                var lookupTableProperty = properties
                    .FirstOrDefault(x =>
                        x.Name == lookupIdProperty.Name.Substring(0, lookupIdProperty.Name.Length - 2));

                if (lookupTableProperty == null)
                {
                    continue;
                }

                Type tableNameType = lookupTableProperty.PropertyType;
                Type tableNameUnderlyingType = Nullable.GetUnderlyingType(tableNameType) ?? tableNameType;

                var lookupTableProperties = tableNameUnderlyingType.GetProperties();
                foreach (var lookupProperty in lookupTableProperties)
                {
                    if (lookupProperty.GetCustomAttribute<UINameAttribute>() != null)
                    {
                        break;
                    }
                }
                
                lookupInfos.Add(new UiSetFieldFromLookupInfo()
                {
                    Id = (Guid)value,
                    // FieldName = property.Name,
                    Property = property,
                    // LookupNameField = lookupFieldName,
                    LookupType = tableNameUnderlyingType,
                });
            }
        }

        return lookupInfos;
    }

    public class UiSetFieldFromLookupInfo
    {
        public required Guid Id { get; set; }
        // public required string FieldName { get; set; }
        public required Type LookupType { get; set; }
        // public required string LookupNameField { get; set; }
        public required PropertyInfo Property { get; set; }
    }
}