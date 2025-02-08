using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Dtos;

namespace Xams.Core.Utils
{
    public static class EntityUtil
    {
        public static readonly HashSet<Type> SimpleTypes = new()
            { typeof(bool), typeof(int), typeof(float), typeof(long), typeof(double), typeof(decimal) };

        /// <summary>
        /// Finds the value of the Name or UILookupNameAttribute property
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static string GetLookupNameValue(object entity)
        {
            string result = string.Empty;
            foreach (var property in entity.GetType().GetProperties())
            {
                if (string.IsNullOrEmpty(result) && property.Name == "Name")
                {
                    result = property.GetValue(entity)?.ToString();
                }

                // if record has a UILookname attribute, use that
                var attribute = property.GetCustomAttributes(typeof(UINameAttribute), true).FirstOrDefault();
                if (attribute != null)
                {
                    result = property.GetValue(entity)?.ToString();
                }
            }

            return result;
        }

        public enum FieldModifications
        {
            NoRelatedFields = 1,
            RelatedToNames = 2, // This will replace the related entity with the Name property
            AllFields = 4
        }

        public static string GetEntityFields(Type targetType, string[]? fields, string fieldPrefix, FieldModifications mods)
        {
            string prefix = string.IsNullOrEmpty(fieldPrefix) ? "" : $"{fieldPrefix}_";
            StringBuilder sbFields = new StringBuilder();
            targetType.GetProperties()
                .Where(x => fields == null || fields.Contains(x.Name))
                .ToList().ForEach(x =>
                {
                    Type? nullableType = Nullable.GetUnderlyingType(x.PropertyType);
                    Type propertyType = x.PropertyType;
                    if (nullableType != null)
                    {
                        propertyType = nullableType;
                    }

                    // If Property is primitive
                    if (propertyType.IsPrimitive
                        || propertyType == typeof(Guid)
                        || propertyType == typeof(string)
                        || propertyType == typeof(decimal)
                        || propertyType == typeof(DateTime)
                        || mods == FieldModifications.AllFields
                       )
                    {
                        sbFields.Append($"{x.Name} as {prefix}{x.Name}, ");
                    }
                    else
                    {
                        if (mods == FieldModifications.RelatedToNames)
                        {
                            // If Property is an entity reference
                            string propertyName = String.Empty;

                            var properties = x.PropertyType.GetProperties();

                            // Check for "Name" first
                            foreach (var property in properties)
                            {
                                if (property.PropertyType == typeof(string) && property.Name == "Name")
                                {
                                    propertyName = $"{x.Name}.{property.Name}";
                                }
                            }

                            foreach (var property in properties)
                            {
                                if (property.GetCustomAttributes()
                                        .FirstOrDefault(x => x.GetType() == typeof(UINameAttribute)) != null)
                                {
                                    propertyName = $"{x.Name}.{property.Name}";
                                }
                            }

                            if (!string.IsNullOrEmpty(propertyName))
                            {
                                sbFields.Append($"{propertyName} as {prefix}{x.Name}, ");
                            }

                            if (string.IsNullOrEmpty(propertyName))
                            {
                                throw new Exception(
                                    $"{x.PropertyType.Name} lookup could not be resolved. Please add a Name property or UILookupNameAttribute to {x.PropertyType.Name}");
                            }
                        }
                    }
                });
            return sbFields.ToString().Substring(0, sbFields.ToString().Length - 2);
        }

        public static object DictionaryToEntity(Type type, Dictionary<string, dynamic> fields)
        {
            object? entity = Activator.CreateInstance(type);
            if (entity == null)
            {
                throw new Exception($"Failed to create instance of {type.Name}");
            }

            foreach (var property in type.GetEntityProperties())
            {
                if (fields.ContainsKey(property.Name))
                {
                    property.SetValue(entity, fields[property.Name]);
                }
            }

            return entity;
        }
        
        public static void OverwriteFields(object entity, object overwriteEntity)
        {
            foreach (var property in entity.GetType().GetProperties())
            {
                var overwriteProperty = overwriteEntity.GetType().GetProperty(property.Name);
                if (overwriteProperty != null)
                {
                    property.SetValue(entity, overwriteProperty.GetValue(overwriteEntity));
                }
            }
        }
        
        public static Response<object?> GetEntity(string tableName, Dictionary<string, dynamic> fields,
            DataOperation dataOperation, out string errorMessage)
        {
            var tableMetadata = Cache.Instance.GetTableMetadata(tableName);

            // Remove corresponding related fields (only use Id fields)
            var relatedFields = from field in fields
                where field.Key.EndsWith("Id")
                select field.Key.Substring(0, field.Key.Length - 2);

            fields = fields.Where(x => !relatedFields.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            // Remove _ui_info_ field
            fields.Remove("_ui_info_");
            fields.Remove("_parameters_");

            MapEntityResult? mapEntityResults;
            if (dataOperation is DataOperation.Delete)
            {
                // Only map the Id, don't perform any additional validation
                mapEntityResults = ConvertToEntityId(tableMetadata.Type, fields);
            }
            else
            {
                mapEntityResults = ConvertToEntity(tableMetadata.Type, fields);
            }

            if (mapEntityResults.Error || mapEntityResults.Entity == null)
            {
                errorMessage = mapEntityResults.Message ?? "Error mapping entity.";
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = mapEntityResults.Message
                };
            }
            errorMessage = string.Empty;
            return new Response<object?>()
            {
                Succeeded = true,
                Data = mapEntityResults.Entity
            };
        }

        public static MapEntityResult ConvertToEntity(Type type, Dictionary<string, dynamic> fields)
        {
            try
            {
                object? entity = Activator.CreateInstance(type);

                if (entity == null)
                {
                    return new MapEntityResult()
                    {
                        Error = true,
                        Message = $"Failed to instantiate Type {type.Name}."
                    };
                }

                foreach (var field in fields)
                {
                    PropertyInfo? property = type.GetProperty(field.Key);
                    if (property == null)
                    {
                        return new MapEntityResult()
                        {
                            Error = true,
                            Message = $"Property {field.Key} not found."
                        };
                    }


                    object? convertedValue = null;

                    string value = string.Empty;
                    if (field.Value is JsonElement)
                    {
                        value = ((JsonElement)field.Value).ToString();
                    }
                    else if (field.Value is string)
                    {
                        value = field.Value;
                    }
                    else
                    {
                        continue;
                    }

                    //
                    // if (field.Value is not JsonElement && field.Value == null)
                    // {
                    //     continue;
                    // }
                    //
                    // string value = ((JsonElement)field.Value).ToString();
                    Type targetType = property.PropertyType;
                    Type? underlyingType = Nullable.GetUnderlyingType(targetType);

                    // If nullable and value is null, set to null
                    if ((value == "" || value == null) && underlyingType != null)
                    {
                        property.SetValue(entity, convertedValue);
                        continue;
                    }

                    if (underlyingType != null)
                    {
                        targetType = underlyingType;
                    }

                    if (targetType == typeof(DateTime))
                    {
                        convertedValue = DateTime.Parse(value, CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                    }
                    else if (targetType == typeof(Guid))
                    {
                        convertedValue = new Guid(value);
                    }
                    else
                    {
                        try
                        {
                            if (!(underlyingType != null && value == "" && SimpleTypes.Contains(underlyingType)))
                            {
                                if (targetType == typeof(float) && value is "-Infinity" or "Infinity" or "NaN")
                                {
                                    if (value is "-Infinity")
                                    {
                                        convertedValue = float.NegativeInfinity;
                                    }
                                    else if (value is "Infinity")
                                    {
                                        convertedValue = float.PositiveInfinity;
                                    }
                                    else if (value is "NaN")
                                    {
                                        convertedValue = float.NaN;
                                    }
                                }
                                else if (targetType == typeof(double) && value is "-Infinity" or "Infinity" or "NaN")
                                {
                                    if (value is "-Infinity")
                                    {
                                        convertedValue = double.NegativeInfinity;
                                    }
                                    else if (value is "Infinity")
                                    {
                                        convertedValue = double.PositiveInfinity;
                                    }
                                    else if (value is "NaN")
                                    {
                                        convertedValue = double.NaN;
                                    }
                                }
                                else
                                {
                                    convertedValue = Convert.ChangeType(value, targetType);
                                    if (targetType == typeof(string) && convertedValue != null)
                                    {
                                        convertedValue = ((string)convertedValue).Trim();
                                    }    
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            return new MapEntityResult()
                            {
                                Error = true,
                                Message = $"Error converting {field.Key}:{value} to {targetType.Name}."
                            };
                        }
                    }

                    property.SetValue(entity, convertedValue);
                }

                return new MapEntityResult()
                {
                    Entity = entity
                };
            }
            catch (Exception e)
            {
                return new MapEntityResult()
                {
                    Error = true,
                    Message = e.Message
                };
            }
        }

        /// <summary>
        /// Only convert the Primary Key to a Guid
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static MapEntityResult ConvertToEntityId(Type type, Dictionary<string, dynamic> fields)
        {
            try
            {
                object? entity = Activator.CreateInstance(type);

                if (entity == null)
                {
                    return new MapEntityResult()
                    {
                        Error = false,
                        Message = $"Failed to instantiate Type {type.Name}."
                    };
                }

                var value = fields[$"{type.Name}Id"];
                if (value is JsonElement)
                {
                    JsonElement jsonElement = (JsonElement)value;
                    type.GetProperty($"{type.Name}Id")?.SetValue(entity, new Guid(jsonElement.ToString()));    
                } 
                else if (value is Guid)
                {
                    type.GetProperty($"{type.Name}Id")?.SetValue(entity, value);
                }

                return new MapEntityResult()
                {
                    Entity = entity
                };
            }
            catch (Exception e)
            {
                return new MapEntityResult()
                {
                    Error = true,
                    Message = e.Message
                };
            }
        }

        /// <summary>
        /// Convert all fields to their respective types and update the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fields"></param>
        /// <returns></returns>
        public static MapEntityResult MergeFields(object? entity, Dictionary<string, dynamic> fields)
        {
            if (entity == null)
            {
                return new MapEntityResult()
                {
                    Error = true,
                    Message = "Entity is null"
                };
            }

            Type entityType = entity.GetType();
            MapEntityResult mapEntityResult = ConvertToEntity(entityType, fields);

            if (mapEntityResult.Error)
            {
                return mapEntityResult;
            }

            PropertyInfo[] properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                if (fields.ContainsKey(property.Name))
                {
                    property.SetValue(entity, property.GetValue(mapEntityResult.Entity));
                }
            }

            return new MapEntityResult()
            {
                Entity = entity
            };
        }

        public static Object PatchEntity(object entity, object preEntity, Dictionary<string, dynamic> fields)
        {
            foreach (PropertyInfo property in preEntity.GetType().GetProperties())
            {
                if (!fields.ContainsKey(property.Name))
                {
                    object? oldValue = preEntity.GetType().GetProperty(property.Name)
                        ?.GetValue(preEntity);
                    entity.GetType().GetProperty(property.Name)?.SetValue(entity, oldValue);
                }
            }

            return entity;
        }
        
        public static string GetEntityTableName<T>(T? entity)
        {
            Type? type = entity?.GetType();
            if (type != null &&
                type.GetCustomAttributes().FirstOrDefault(x => x.GetType() == typeof(TableAttribute)) is TableAttribute
                    tableAttribute)
            {
                return tableAttribute.Name;
            }

            return type?.Name + "s";
        }
        
        public static bool IsSystemEntity(string tableName)
        {
            if (new[]
                {
                    "Option", "Permission", "Role", "RolePermission", "Team", 
                    "TeamRole", "TeamUser", "User", "UserRole", "Setting", "Job", "JobHistory", "Audit", "AuditField",
                    "AuditHistory", "AuditHistoryDetail", "System", "Server"
                }.Contains(tableName))
            {
                return true;
            }

            return false;
        }

        public class MapEntityResult
        {
            public bool Error { get; init; }
            public string? Message { get; init; }
            public object? Entity { get; init; }
        }
    }
}