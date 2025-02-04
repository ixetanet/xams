using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Dtos.Data;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core
{
    public class Cache
    {
        public static Cache Instance { get; private set; } = new();

        public readonly Dictionary<string, List<ServiceLogicInfo>> ServiceLogics = new();
        public readonly Dictionary<string, ServiceActionInfo> ServiceActions = new();
        public List<ServiceBulkInfo> BulkServiceLogics { get; private set; } = new();
        public List<ServiceStartupInfo> ServiceStartupInfos { get; private set; } = new();
        public List<ServiceSecurityInfo> ServiceSecurityInfos { get; private set; } = new();
        public readonly List<ServicePermissionInfo> ServicePermissionInfos = new();
        public readonly Dictionary<string, ServiceJobInfo> ServiceJobs = new();
        public readonly ConcurrentDictionary<string, AuditInfo> TableAuditInfo = new();
        public readonly Dictionary<string, MetadataInfo> TableMetadata = new();

        public bool IsAuditEnabled { get; set; }
        public DateTime AuditRefreshTime { get; set; } = DateTime.MinValue;
        public int JobHistoryRetentionDays { get; set; } = 30;
        public int AuditHistoryRetentionDays { get; set; } = 30;

        internal static void Initialize(Type dbContext)
        {
            Console.WriteLine("Initializing Cache");
            Cache cache = new Cache();
            Instance = cache;
            
            // Cache entity metadata
            var props = dbContext.GetProperties();
            var nullabilityInfoContext = new NullabilityInfoContext();
            foreach (var prop in props)
            {
                var propType = prop.PropertyType;
                if (propType.GenericTypeArguments.Length > 0)
                {
                    Instance.ValidateMetadata(propType.GenericTypeArguments[0]);

                    var tableAttribute = propType.GenericTypeArguments[0].GetCustomAttribute<TableAttribute>();
                    if (tableAttribute == null)
                    {
                        continue;
                    }

                    var tableMetadata = new MetadataInfo()
                    {
                        Type = propType.GenericTypeArguments[0],
                        DisplayNameAttribute =
                            propType.GenericTypeArguments[0].GetCustomAttribute<UIDisplayNameAttribute>()
                            ??
                            new UIDisplayNameAttribute(tableAttribute.Name,
                                EntityUtil.IsSystemEntity(tableAttribute.Name) ? "System" : ""),
                        TableAttribute = tableAttribute,
                        PrimaryKey = $"{tableAttribute.Name}Id",
                        NameProperty = propType.GenericTypeArguments[0]
                                           .GetProperties()
                                           .FirstOrDefault(x => x.GetCustomAttribute<UINameAttribute>() != null)
                                       ?? propType.GenericTypeArguments[0].GetProperty("Name"),
                        IsProxy = propType.GenericTypeArguments[0].GetCustomAttribute<UIProxyAttribute>() != null,
                        HasOwningUserField = propType.GenericTypeArguments[0].GetProperty("OwningUserId") != null,
                        HasOwningTeamField = propType.GenericTypeArguments[0].GetProperty("OwningTeamId") != null
                    };
                    cache.TableMetadata.Add(tableMetadata.TableAttribute.Name, tableMetadata);

                    // Get Metadata Output for the metadata endpoint
                    var properties = tableMetadata.Type.GetProperties();
                    List<MetadataField> fields = new List<MetadataField>();
                    foreach (var property in properties)
                    {
                        // Skip ICollection
                        if (!property.IsValidEntityProperty())
                            continue;
                        // Ignore the Primary Key field
                        if (property.Name == $"{tableAttribute.Name}Id")
                            continue;

                        // Ignore the hidden fields
                        if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null)
                            continue;

                        // Ignore the foreign key field
                        if (!property.IsPrimitive() && properties.Any(x => x.Name == $"{property.Name}Id"))
                            continue;

                        bool isLookup = tableMetadata.Type.IsLookupField(property.Name);

                        string? lookupTableNameField = string.Empty;
                        string? lookuptableDescriptionField = string.Empty;
                        string lookupTable = string.Empty;
                        if (isLookup)
                        {
                            var lookupProperty =
                                properties.FirstOrDefault(x =>
                                    x.Name == property.Name.Substring(0, property.Name.Length - 2));

                            if (lookupProperty?.PropertyType.GetCustomAttribute(typeof(TableAttribute)) is
                                TableAttribute
                                propTableAttribute)
                            {
                                lookupTable = propTableAttribute.Name;
                            }

                            if (string.IsNullOrEmpty(lookupTable))
                            {
                                lookupTable = lookupProperty?.PropertyType.Name + "s";
                            }

                            lookupTableNameField = lookupProperty?.PropertyType
                                .GetProperties()
                                .FirstOrDefault(x => x.GetCustomAttribute(typeof(UINameAttribute)) != null)
                                ?.Name;
                            if (string.IsNullOrEmpty(lookupTableNameField))
                            {
                                lookupTableNameField = lookupProperty?.PropertyType
                                    .GetProperties()
                                    .FirstOrDefault(x => x.Name == "Name")
                                    ?.Name;
                            }

                            lookuptableDescriptionField = lookupProperty?.PropertyType
                                .GetProperties()
                                .FirstOrDefault(x => x.GetCustomAttribute(typeof(UIDescriptionAttribute)) != null)
                                ?.Name;
                        }

                        UIOrderAttribute layoutAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIOrderAttribute)) as UIOrderAttribute ??
                            new UIOrderAttribute(99999);

                        UIReadOnlyAttribute? readOnlyAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIReadOnlyAttribute)) as UIReadOnlyAttribute;

                        UIDisplayNameAttribute? displayNameAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIDisplayNameAttribute)) as
                                UIDisplayNameAttribute;

                        UIOptionAttribute? optionAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIOptionAttribute)) as UIOptionAttribute;

                        UIRequiredAttribute? requiredAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIRequiredAttribute)) as UIRequiredAttribute;

                        UIDateFormatAttribute? dateFormatAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIDateFormatAttribute)) as
                                UIDateFormatAttribute;

                        UICharacterLimitAttribute? characterLimitAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UICharacterLimitAttribute)) as
                                UICharacterLimitAttribute;

                        UIRecommendedAttribute? recommendedAttribute =
                            Attribute.GetCustomAttribute(property, typeof(UIRecommendedAttribute)) as
                                UIRecommendedAttribute;
                        
                        UINumberRangeAttribute? numberRangeAttribute = 
                            Attribute.GetCustomAttribute(property, typeof(UINumberRangeAttribute)) as
                                UINumberRangeAttribute;

                        string fieldType = GetFieldType(isLookup, property);

                        if (displayNameAttribute == null)
                        {
                            if (fieldType == "Lookup")
                            {
                                displayNameAttribute =
                                    new UIDisplayNameAttribute(property.Name.Substring(0, property.Name.Length - 2));
                            }
                            else
                            {
                                displayNameAttribute = new UIDisplayNameAttribute(property.Name);
                            }
                        }

                        bool isNullable;
                        if (property.PropertyType != typeof(string))
                        {
                            isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
                        }
                        else
                        {
                            var info = nullabilityInfoContext.Create(property);
                            isNullable = info.WriteState == NullabilityState.Nullable;
                        }
                        
                        fields.Add(new MetadataField()
                        {
                            name = property.Name,
                            displayName = displayNameAttribute.Name,
                            type = fieldType,
                            characterLimit = characterLimitAttribute?.Limit,
                            order = layoutAttribute.Order,
                            lookupName = isLookup ? property.Name.Substring(0, property.Name.Length - 2) : null,
                            lookupTable = lookupTable,
                            lookupTableNameField = lookupTableNameField,
                            lookupTableDescriptionField = lookuptableDescriptionField,
                            dateFormat = dateFormatAttribute?.DateFormat,
                            isNullable = isNullable,
                            isRequired = requiredAttribute != null,
                            isRecommended = recommendedAttribute != null,
                            isReadOnly = readOnlyAttribute != null,
                            option = optionAttribute?.Name ?? "",
                            numberRange = numberRangeAttribute != null ? $"{numberRangeAttribute.Min}-{numberRangeAttribute.Max}" : null
                        });
                    }

                    fields = fields.OrderBy(x => x.order).ToList();

                    var tableName = tableMetadata.TableAttribute.Name;
                    var metadataOutput = new MetadataOutput()
                    {
                        displayName = tableMetadata.DisplayNameAttribute?.Name ?? tableName,
                        tableName = tableName,
                        primaryKey = tableName + "Id",
                        fields = fields
                    };
                    tableMetadata.MetadataOutput = metadataOutput;
                }
            }

            Instance.ValidateSystemEntities();

            var assemblies = AssemblyLoadContext.Default.Assemblies.ToList();

            foreach (var assembly in assemblies)
            {
                // Cache Service Logic
                foreach (var type in assembly.GetTypes())
                {
                    ServiceLogicAttribute? serviceLogicAttribute = type.GetCustomAttribute<ServiceLogicAttribute>();
                    if (serviceLogicAttribute == null)
                    {
                        continue;
                    }

                    ServiceLogicInfo serviceLogicInfo = new ServiceLogicInfo
                    {
                        ServiceLogicAttribute = serviceLogicAttribute,
                        Type = type
                    };

                    if (serviceLogicAttribute.TableName != "*" &&
                        !cache.TableMetadata.ContainsKey(serviceLogicAttribute.TableName))
                    {
                        throw new Exception($"Table {serviceLogicAttribute.TableName} not found in metadata");
                    }

                    if (serviceLogicAttribute.LogicStage.HasFlag(LogicStage.PostOperation))
                    {
                        if (cache.TableMetadata.TryGetValue(serviceLogicAttribute.TableName, out var metadataInfo))
                        {
                            metadataInfo.HasPostOpServiceLogic = true;
                        }
                        else if (serviceLogicAttribute.TableName.Trim() == "*")
                        {
                            foreach (var kvp in cache.TableMetadata)
                            {
                                kvp.Value.HasPostOpServiceLogic = true;
                            }
                        }
                    }

                    if (serviceLogicAttribute.LogicStage.HasFlag(LogicStage.PreOperation))
                    {
                        if (cache.TableMetadata.TryGetValue(serviceLogicAttribute.TableName, out var metadataInfo))
                        {
                            metadataInfo.HasPreOpServiceLogic = true;
                        }
                        else if (serviceLogicAttribute.TableName.Trim() == "*")
                        {
                            foreach (var kvp in cache.TableMetadata)
                            {
                                kvp.Value.HasPreOpServiceLogic = true;
                            }
                        }
                    }

                    if (serviceLogicAttribute.DataOperation.HasFlag(DataOperation.Delete))
                    {
                        if (cache.TableMetadata.TryGetValue(serviceLogicAttribute.TableName, out var metadataInfo))
                        {
                            metadataInfo.HasDeleteServiceLogic = true;
                        }
                        else if (serviceLogicAttribute.TableName.Trim() == "*")
                        {
                            foreach (var kvp in cache.TableMetadata)
                            {
                                kvp.Value.HasDeleteServiceLogic = true;
                            }
                        }
                    }

                    if (cache.ServiceLogics.ContainsKey(serviceLogicAttribute.TableName))
                    {
                        cache.ServiceLogics[serviceLogicAttribute.TableName].Add(serviceLogicInfo);
                    }
                    else
                    {
                        cache.ServiceLogics.Add(serviceLogicAttribute.TableName, new List<ServiceLogicInfo>
                        {
                            serviceLogicInfo,
                        });
                    }
                }

                // Order service logics
                foreach (var kvp in cache.ServiceLogics)
                {
                    cache.ServiceLogics[kvp.Key] = kvp.Value.OrderBy(x => x.ServiceLogicAttribute.Order).ToList();
                }
            }

            foreach (var assembly in assemblies)
            {
                // Cache Bulk Services
                foreach (var type in assembly.GetTypes())
                {
                    BulkServiceAttribute? serviceBulkAttribute = type.GetCustomAttribute<BulkServiceAttribute>();
                    if (serviceBulkAttribute == null)
                    {
                        continue;
                    }

                    ServiceBulkInfo serviceBulkInfo = new()
                    {
                        BulkServiceAttribute = serviceBulkAttribute,
                        Type = type
                    };


                    cache.BulkServiceLogics.Add(serviceBulkInfo);
                }

                cache.BulkServiceLogics = cache.BulkServiceLogics.OrderBy(x => x.BulkServiceAttribute.Order).ToList();
            }


            // Cache Actions
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ServiceActionAttribute? serviceActionAttribute = type.GetCustomAttribute<ServiceActionAttribute>();
                    if (serviceActionAttribute == null)
                    {
                        continue;
                    }

                    ServiceActionInfo serviceActionInfo = new()
                    {
                        ServiceActionAttribute = serviceActionAttribute,
                        Type = type
                    };

                    if (cache.ServiceActions.ContainsKey(serviceActionAttribute.Name))
                    {
                        throw new Exception($"Duplicate Actions with Name: {serviceActionAttribute.Name}");
                    }

                    cache.ServiceActions.Add(serviceActionAttribute.Name, serviceActionInfo);
                }
            }


            // Cache Startup Services
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ServiceStartupAttribute? serviceStartupAttribute =
                        type.GetCustomAttribute<ServiceStartupAttribute>();
                    if (serviceStartupAttribute == null)
                    {
                        continue;
                    }

                    ServiceStartupInfo serviceStartupInfo = new()
                    {
                        ServiceStartupAttribute = serviceStartupAttribute,
                        Type = type
                    };

                    cache.ServiceStartupInfos.Add(serviceStartupInfo);
                }

                cache.ServiceStartupInfos =
                    cache.ServiceStartupInfos.OrderBy(x => x.ServiceStartupAttribute.Order).ToList();
            }

            // Cache Security Services
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ServiceSecurityAttribute? serviceSecurityAttribute =
                        type.GetCustomAttribute<ServiceSecurityAttribute>();
                    if (serviceSecurityAttribute == null)
                    {
                        continue;
                    }

                    ServiceSecurityInfo serviceSecurityInfo = new()
                    {
                        ServiceSecurityAttribute = serviceSecurityAttribute,
                        Type = type
                    };

                    cache.ServiceSecurityInfos.Add(serviceSecurityInfo);
                }
            }

            cache.ServiceSecurityInfos =
                cache.ServiceSecurityInfos.OrderBy(x => x.ServiceSecurityAttribute.Order).ToList();


            // Cache Permission Services
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ServicePermissionAttribute? servicePermissionAttribute =
                        type.GetCustomAttribute<ServicePermissionAttribute>();
                    if (servicePermissionAttribute == null)
                    {
                        continue;
                    }

                    ServicePermissionInfo servicePermissionInfo = new()
                    {
                        Type = type
                    };

                    cache.ServicePermissionInfos.Add(servicePermissionInfo);
                }
            }

            // Cache Jobs
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ServiceJobAttribute? serviceJobAttribute = type.GetCustomAttribute<ServiceJobAttribute>();
                    if (serviceJobAttribute == null)
                    {
                        continue;
                    }

                    ServiceJobInfo serviceJobInfo = new()
                    {
                        ServiceJobAttribute = serviceJobAttribute,
                        Type = type
                    };

                    if (cache.ServiceJobs.ContainsKey(serviceJobAttribute.Name))
                    {
                        throw new Exception($"Duplicate Jobs with Name: {serviceJobAttribute.Name}");
                    }

                    cache.ServiceJobs.Add(serviceJobAttribute.Name, serviceJobInfo);
                }
            }
        }

        internal List<MetadataInfo> GetTableMetadata()
        {
            if (TableMetadata.Count > 0)
            {
                return TableMetadata.Values.ToList();
            }


            return TableMetadata.Values.ToList();
        }

        public MetadataInfo GetTableMetadata(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
            {
                throw new Exception("Table name cannot be null or empty");
            }

            if (TableMetadata.Count == 0)
            {
                GetTableMetadata();
            }

            return TableMetadata[tableName] ?? throw new Exception($"Table {tableName} not found in metadata");
        }

        private void ValidateSystemEntities()
        {
            List<Entity>? entities = JsonSerializer.Deserialize<List<Entity>>(SystemEntities.JsonValidationSchema);

            if (entities == null)
            {
                throw new Exception($"Failed to load system entities json metadata");
            }

            foreach (var entity in entities)
            {
                if (!TableMetadata.ContainsKey(entity.Name))
                {
                    throw new Exception($"System Entity {entity.Name} not found in metadata");
                }

                foreach (var entityProperty in entity.Properties)
                {
                    if (TableMetadata[entity.Name].Type.GetProperty(entityProperty.Name) == null)
                    {
                        throw new Exception($"Property {entityProperty.Name} not found in {entity.Name}");
                    }

                    if (TableMetadata[entity.Name].Type.GetProperty(entityProperty.Name)!.PropertyType.Name !=
                        entityProperty.Type)
                    {
                        throw new Exception($"Property {entityProperty.Name} type mismatch in {entity.Name}");
                    }
                }
            }
        }

        private void ValidateMetadata(Type tableType)
        {
            List<string> errors = new();

            var tableAttribute = tableType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute == null)
            {
                errors.Add($"TableAttribute not found for {tableType.Name}");
            }
            else
            {
                if (string.IsNullOrEmpty(tableAttribute.Name))
                {
                    errors.Add($"TableAttribute Name not found for {tableType.Name}");
                }

                if (TableMetadata.ContainsKey(tableAttribute.Name))
                {
                    errors.Add($"Duplicate TableAttribute Name: {tableAttribute.Name}");
                }

                if (tableAttribute.Name != tableType.Name)
                {
                    errors.Add($"Class Name and TableAttribute Name do not match for {tableType.Name}");
                }
            }

            if (tableType.GetProperty($"{tableType.Name}Id") == null)
            {
                errors.Add($"Primary Key {tableType.Name}Id not found for {tableType.Name}");
            }
            else if (tableType.GetProperty($"{tableType.Name}Id")?.PropertyType != typeof(Guid))
            {
                errors.Add($"Primary Key {tableType.Name}Id must be of type Guid for {tableType.Name}");
            }

            var nameAttrProp = tableType.GetProperties()
                .FirstOrDefault(x => x.GetCustomAttribute<UINameAttribute>() != null);

            if (!EntityUtil.IsSystemEntity(tableType.Name))
            {
                if (nameAttrProp != null)
                {
                    if (nameAttrProp.PropertyType != typeof(string))
                    {
                        errors.Add($"Name Property {nameAttrProp.Name} must be of type string for {tableType.Name}");
                    }
                }
                else
                {
                    var nameProperty = tableType.GetProperty("Name");
                    if (nameProperty == null)
                    {
                        errors.Add($"Name Property not found for {tableType.Name}");
                    }
                    else
                    {
                        if (nameProperty.PropertyType != typeof(string))
                        {
                            errors.Add($"Name Property must be of type string for {tableType.Name}");
                        }
                    }
                }
            }


            if (errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }

                Console.ResetColor();
            }
        }
        
        private static string GetFieldType(bool isLookup, PropertyInfo property)
        {
            if (isLookup)
            {
                return "Lookup";
            }

            Type? underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
            if (underlyingType != null)
            {
                return underlyingType.Name;
            }

            return property.PropertyType.Name;
        }


        public class MetadataInfo
        {
            public Type Type { get; set; } = null!;
            public TableAttribute TableAttribute { get; set; } = null!;
            public UIDisplayNameAttribute? DisplayNameAttribute { get; set; }
            public PropertyInfo? NameProperty { get; set; }
            public string PrimaryKey { get; set; } = null!;
            public bool HasPostOpServiceLogic { get; set; }
            public bool HasPreOpServiceLogic { get; set; }
            public bool HasDeleteServiceLogic { get; set; }
            public bool HasOwningUserField { get; set; }
            public bool HasOwningTeamField { get; set; }
            public bool IsProxy { get; set; }

            public MetadataOutput MetadataOutput { get; set; } = null!;
            
        }

        public class ServiceLogicInfo
        {
            public ServiceLogicAttribute ServiceLogicAttribute { get; internal set; } = null!;
            public Type Type { get; internal set; } = null!;
        }

        public class ServiceBulkInfo
        {
            public BulkServiceAttribute BulkServiceAttribute { get; internal set; } = null!;
            public Type Type { get; internal set; } = null!;
        }

        public class ServiceActionInfo
        {
            public ServiceActionAttribute ServiceActionAttribute { get; internal set; } = null!;
            public Type Type { get; internal set; } = null!;
        }

        public class ServiceStartupInfo
        {
            public ServiceStartupAttribute ServiceStartupAttribute { get; internal set; } = null!;
            public Type Type { get; internal set; } = null!;
        }

        public class ServiceSecurityInfo
        {
            public ServiceSecurityAttribute ServiceSecurityAttribute { get; internal set; } = null!;
            public Type Type { get; internal set; } = null!;
        }

        public class ServicePermissionInfo
        {
            public Type Type { get; internal set; } = null!;
        }

        public class Entity
        {
            public required string Name { get; set; }
            public required List<EntityProperty> Properties { get; set; }
        }

        public class EntityProperty
        {
            public required string Name { get; set; }
            public required string Type { get; set; }
        }

        public class ServiceJobInfo
        {
            public ServiceJobAttribute ServiceJobAttribute { get; internal set; } = null!;
            public Type? Type { get; internal set; }
        }

        public class AuditInfo
        {
            public bool IsCreateAuditEnabled { get; internal set; }
            public bool IsReadAuditEnabled { get; internal set; }
            public bool IsUpdateAuditEnabled { get; internal set; }
            public bool IsDeleteAuditEnabled { get; internal set; }
            public Dictionary<string, FieldAuditInfo> FieldAuditInfos { get; } = new();
        }

        public class FieldAuditInfo
        {
            public bool IsCreateAuditEnabled { get; internal set; }
            public bool IsUpdateAuditEnabled { get; internal set; }
            public bool IsDeleteAuditEnabled { get; internal set; }
        }
    }
}