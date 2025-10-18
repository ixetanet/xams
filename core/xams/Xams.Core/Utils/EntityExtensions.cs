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
            if (typeof(T) != typeof(object) && (propertyInfo == null || propertyInfo.PropertyType != typeof(T)))
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
    
    public static object GetId(this object obj)
    {
        // Get the metadata for this type to find the primary key
        var tableMetadata = obj.Metadata();
        var primaryKeyName = tableMetadata.PrimaryKey;
        
        if (obj is ExpandoObject)
        {
            if (obj is not IDictionary<string, object> dict)
            {
                throw new Exception($"Property {primaryKeyName} not found on {obj.GetType().Name}");
            }

            if (!dict.ContainsKey(primaryKeyName))
            {
                throw new Exception($"Property {primaryKeyName} not found on {obj.GetType().Name}");
            }

            var value = dict[primaryKeyName];
            if (value == null)
            {
                throw new Exception($"Property {primaryKeyName} is null on {obj.GetType().Name}");
            }

            return value;
        }
        
        PropertyInfo? property = obj.GetType().GetProperty(primaryKeyName);
        if (property == null)
        {
            throw new Exception($"Property {primaryKeyName} not found on {obj.GetType().Name}");
        }

        var propValue = property.GetValue(obj);

        if (propValue == null)
        {
            throw new Exception($"Property {primaryKeyName} is null on {obj.GetType().Name}");
        }

        return propValue;
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
    
    public static string? GetNameFieldValue(this object obj)
    {
        var nameProperty = obj.Metadata().NameProperty;

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
        
        // Check if the field ends with "Id"
        if (!fieldName.EndsWith("Id") || fieldName.Length <= 2)
        {
            return false;
        }

        // Check if there's a corresponding navigation property
        var navigationPropertyName = fieldName.Substring(0, fieldName.Length - 2);
        var propertyInfo = type.GetProperty(navigationPropertyName);

        if (propertyInfo == null)
        {
            return false;
        }

        // Check if the navigation property is a complex type (not a primitive)
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
        
        // Check if the field ends with "Id"
        if (!fieldName.EndsWith("Id") || fieldName.Length <= 2)
        {
            return null;
        }

        // Get the navigation property name by removing the "Id" suffix
        var navigationPropertyName = fieldName.Substring(0, fieldName.Length - 2);
        var propertyInfo = type.GetProperty(navigationPropertyName);

        if (propertyInfo == null)
        {
            return null;
        }

        // Check if the navigation property is a complex type (not a primitive)
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

        // If a core entity has been inherited get that entity, ie: User, Team, etc.
        var metadata = Cache.Instance.GetTableMetadata(propertyType);
        propertyType = metadata.Type;

        return propertyType;
    }
    
    public static bool IsICollection(this PropertyInfo propertyInfo)
    {
        Type propertyType = propertyInfo.PropertyType;
        if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            return true;
        }
        return false;
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
            
            if (property.IsICollection())
            {
                continue;
            }
            
            result.Add(property);
        }

        return result;
    }

    public static Cache.MetadataInfo Metadata(this object obj)
    {
        return Cache.Instance.GetTableMetadata(obj.GetType());
    }

    public static Cache.MetadataInfo Metadata(this Type obj)
    {
        return Cache.Instance.GetTableMetadata(obj);
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
                if (!lookupIdProperty.Name.EndsWith("Id") || lookupIdProperty.Name.Length <= 2)
                {
                    continue;
                }

                // Get the ID value (could be any type, not just Guid)
                var idValue = lookupIdProperty.GetValue(_obj);
                if (idValue == null)
                {
                    continue;
                }

                // For backward compatibility, we still use Guid for existing code
                // In the future, this should be updated to handle any primary key type
                if (idValue is not Guid guidValue)
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
                    Id = guidValue,
                    Property = property,
                    LookupType = tableNameUnderlyingType,
                });
            }
        }

        return lookupInfos;
    }

    public class UiSetFieldFromLookupInfo
    {
        public required Guid Id { get; set; }
        public required Type LookupType { get; set; }
        public required PropertyInfo Property { get; set; }
    }
}
