using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;
using Xams.Core.Startup;
using Xams.Core.Utils;

namespace Xams.Core
{
    public class Cache
    {
        public static Cache Instance { get; private set; } = new();

        public string? ServerName { get; set; }
        public Guid ServerId { get; set; }
        public readonly Dictionary<string, List<ServiceLogicInfo>> ServiceLogics = new();
        public readonly Dictionary<string, ServiceActionInfo> ServiceActions = new();
        public List<ServiceBulkInfo> BulkServiceLogics { get; private set; } = new();
        public List<ServiceStartupInfo> ServiceStartupInfos { get; private set; } = new();
        public List<ServiceSecurityInfo> ServiceSecurityInfos { get; private set; } = new();
        public readonly List<ServicePermissionInfo> ServicePermissionInfos = new();
        public readonly Dictionary<string, ServiceJobInfo> ServiceJobs = new();
        public readonly ConcurrentDictionary<string, AuditInfo> TableAuditInfo = new();
        public readonly Dictionary<string, MetadataInfo> TableMetadata = new();
        public readonly Dictionary<Type, MetadataInfo> TableTypeMetadata = new();

        public bool IsAuditEnabled { get; set; }
        public DateTime AuditRefreshTime { get; set; } = DateTime.MinValue;
        public int JobHistoryRetentionDays { get; set; } = 30;
        public int AuditHistoryRetentionDays { get; set; } = 30;

        internal static void Initialize(Type dbContext, IDataService dataService)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            var serverName = Environment.GetEnvironmentVariable("SERVER_NAME") ??
                             ServerNames[rnd.Next(0, ServerNames.Length)] + rnd.Next(0, 9999);
            Console.WriteLine($"Initializing Cache for server {serverName}");
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
                    var entityType = propType.GetGenericArguments()[0];
                    Instance.ValidateMetadata(entityType);

                    var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
                    if (tableAttribute == null)
                    {
                        tableAttribute = new TableAttribute(EntityUtil.GetTableName(entityType, dbContext).TableName);
                    }

                    // Find the primary key property
                    var primaryKeyProperty =
                        FindPrimaryKeyProperty(entityType, tableAttribute.Name);
                    var primaryKeyType = primaryKeyProperty.PropertyType;
                    var primaryKeyName = primaryKeyProperty.Name;

                    var tableMetadata = new MetadataInfo()
                    {
                        Type = entityType,
                        DisplayNameAttribute =
                            entityType.GetCustomAttribute<UIDisplayNameAttribute>()
                            ??
                            new UIDisplayNameAttribute(tableAttribute.Name,
                                EntityUtil.IsSystemEntity(tableAttribute.Name) ? "System" : ""),
                        TableName = tableAttribute.Name,
                        PrimaryKey = primaryKeyName,
                        PrimaryKeyProperty = primaryKeyProperty,
                        PrimaryKeyType = primaryKeyType,
                        NameProperty = entityType
                                           .GetProperties()
                                           .FirstOrDefault(x => x.GetCustomAttribute<UINameAttribute>() != null)
                                       ?? entityType.GetProperty("Name"),
                        IsProxy = entityType.GetCustomAttribute<UIProxyAttribute>() != null,
                        HasOwningUserField = entityType.GetProperty("OwningUserId") != null,
                        HasOwningTeamField = entityType.GetProperty("OwningTeamId") != null
                    };
                    cache.TableMetadata.Add(tableMetadata.TableName, tableMetadata);
                    cache.TableTypeMetadata.Add(entityType, tableMetadata);

                    // Get Metadata Output for the metadata endpoint
                    var properties = tableMetadata.Type.GetProperties();
                    List<MetadataField> fields = new List<MetadataField>();
                    foreach (var property in properties)
                    {
                        // Skip ICollection
                        if (!property.IsValidEntityProperty())
                            continue;
                        // Ignore the Primary Key field
                        if (property.Name == primaryKeyName)
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
                            numberRange = numberRangeAttribute != null
                                ? $"{numberRangeAttribute.Min}-{numberRangeAttribute.Max}"
                                : null
                        });
                    }

                    fields = fields.OrderBy(x => x.order).ToList();

                    var tableName = EntityUtil.GetTableName(entityType, dbContext).TableName;
                    var metadataOutput = new MetadataOutput()
                    {
                        displayName = tableMetadata.DisplayNameAttribute?.Name ?? tableName,
                        tableName = tableName,
                        primaryKey = primaryKeyName,
                        fields = fields
                    };
                    tableMetadata.MetadataOutput = metadataOutput;
                }
            }

            foreach (var metadataInfo in cache.TableMetadata.Values)
            {
                foreach (var field in metadataInfo.MetadataOutput.fields)
                {
                    if (!string.IsNullOrEmpty(field.lookupTable))
                    {
                        field.lookupPrimaryKeyField = cache.GetTableMetadata(field.lookupTable).PrimaryKey;
                    }
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

                    JobServerAttribute? jobServerAttribute = type.GetCustomAttribute<JobServerAttribute>();
                    JobTimeZone? jobDaylightSavingsAttribute = type.GetCustomAttribute<JobTimeZone>();

                    if (jobDaylightSavingsAttribute != null && !IsValidTimeZone(jobDaylightSavingsAttribute.TimeZone))
                    {
                        throw new Exception(
                            $"Invalid TimeZone '{jobDaylightSavingsAttribute.TimeZone}' for job {serviceJobAttribute.Name}");
                    }

                    ServiceJobInfo serviceJobInfo = new()
                    {
                        ServiceJobAttribute = serviceJobAttribute,
                        Type = type,
                        ExecuteJobOn = jobServerAttribute?.ExecuteJobOn ?? ExecuteJobOn.All,
                        ServerName = jobServerAttribute?.ServerName ?? string.Empty,
                        TimeZone = jobDaylightSavingsAttribute?.TimeZone ?? string.Empty,
                    };

                    if (cache.ServiceJobs.ContainsKey(serviceJobAttribute.Name))
                    {
                        throw new Exception($"Duplicate Jobs with Name: {serviceJobAttribute.Name}");
                    }

                    cache.ServiceJobs.Add(serviceJobAttribute.Name, serviceJobInfo);
                }
            }

            // Get Server Nam
            cache.ServerName = serverName;
            cache.ServerId = Guid.NewGuid();

            // Create new server record
            var serverRecord = new Dictionary<string, dynamic>();
            serverRecord["Name"] = cache.ServerName;
            serverRecord["ServerId"] = cache.ServerId;
            serverRecord["LastPing"] = DateTime.UtcNow;
            var serverEntity = EntityUtil.DictionaryToEntity(cache.GetTableMetadata("Server").Type, serverRecord);
            using (var db = dataService.GetDataRepository().CreateNewDbContext<BaseDbContext>())
            {
                db.Add(serverEntity);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Finds the primary key property of an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to find the primary key for.</param>
        /// <param name="tableName">The table name from the TableAttribute.</param>
        /// <returns>The PropertyInfo of the primary key property.</returns>
        private static PropertyInfo FindPrimaryKeyProperty(Type entityType, string tableName)
        {
            // First, look for properties with the [Key] attribute
            var keyProperties = entityType.GetProperties()
                .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                .ToList();

            if (keyProperties.Count == 1)
            {
                return keyProperties[0];
            }

            // If no [Key] attribute is found, look for properties named "Id" or "{TableName}Id"
            var idProperty = entityType.GetProperty("Id");
            if (idProperty != null)
            {
                return idProperty;
            }

            var tableIdProperty = entityType.GetProperty($"{tableName}Id");
            if (tableIdProperty != null)
            {
                return tableIdProperty;
            }

            // If no primary key is found, throw an exception
            throw new Exception($"No primary key found for entity type {entityType.Name}");
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

            if (!TableMetadata.ContainsKey(tableName))
            {
                throw new Exception($"Table {tableName} not found in metadata");
            }

            return TableMetadata[tableName];
        }

        public MetadataInfo GetTableMetadata(Type entityType)
        {
            return TableTypeMetadata[entityType];
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

            // Check for primary key
            try
            {
                var primaryKeyProperty = FindPrimaryKeyProperty(tableType, tableAttribute?.Name ?? tableType.Name);
                // No need to validate the type of the primary key anymore
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
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
                Console.ForegroundColor = ConsoleColor.Yellow;
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

        private static bool IsValidTimeZone(string timeZoneId)
        {
            if (string.IsNullOrEmpty(timeZoneId))
                return false;

            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
        }



        public class MetadataInfo
        {
            public Type Type { get; set; } = null!;
            public string TableName { get; set; }
            public UIDisplayNameAttribute? DisplayNameAttribute { get; set; }
            public PropertyInfo? NameProperty { get; set; }
            public string PrimaryKey { get; set; } = null!;
            public PropertyInfo PrimaryKeyProperty { get; set; } = null!;
            public Type PrimaryKeyType { get; set; } = null!;
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
            public ExecuteJobOn ExecuteJobOn { get; internal set; } = ExecuteJobOn.All;
            public string? ServerName { get; internal set; }

            public string? TimeZone { get; internal set; }
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

        public static string[] ServerNames =
        [
            "AlphaCore", "BetaNexus", "GammaVault", "DeltaHorizon", "EpsilonGrid",
            "ZetaSphere", "EtaMatrix", "ThetaPulse", "IotaLink", "KappaBridge",
            "LambdaNode", "MuCloud", "NuStream", "XiForge", "OmicronVault",
            "PiBeacon", "RhoSignal", "SigmaWave", "TauPulse", "UpsilonSync",
            "PhiCore", "ChiFrame", "PsiChannel", "OmegaNet", "NebulaHost",
            "QuantumGate", "HyperionLink", "OrionSphere", "AetherGrid", "CelestialNode",
            "EchoRelay", "PhoenixLayer", "ZenithStream", "ParagonNet", "SolsticeBeacon",
            "TitaniumMesh", "VelocityCore", "AuroraHost", "HorizonSync", "GenesisCloud",
            "EchelonBridge", "CipherVault", "NexusFlow", "ZenGrid", "NovaLink",
            "PinnacleStream", "MiragePulse", "EclipseRelay", "StratosphereMesh", "ChronosSync",
            "PulsarNode", "NebulaCore", "SentinelHost", "VelocitySync", "OdysseyVault",
            "StellarFrame", "InfinityGate", "TerraSphere", "CosmosNet", "HyperDriveRelay",
            "PrimeBeacon", "VertexGrid", "SummitSync", "EmpyreanHost", "CyberBridge",
            "MomentumNode", "EnclaveCore", "ElevateStream", "SingularityNet", "FusionWave",
            "EpochLayer", "VanguardMesh", "UtopiaRelay", "RadianceSync", "SpectralVault",
            "EquinoxNode", "PioneerGrid", "AstralBeacon", "BeaconSync", "EonLink",
            "HorizonCore", "LegacyFlow", "SynergyWave", "AscendNet", "SovereignCloud",
            "PrimeSphere", "EtherealBridge", "CatalystMesh", "GlacialNode", "SummitPulse",
            "ExodusVault", "MomentumFlow", "CelestialPulse", "ApexSync", "PulseGrid",
            "PinnacleHost", "SolaceRelay", "LuminousCore", "VertexMesh", "MetropolisNet",
            "CosmicVault", "InfinityNode", "EclipticSync", "SentientGrid", "StrataRelay",
            "DynastyCore", "ChronicleStream", "HorizonBeacon", "GenesisRelay", "TranquilNet",
            "HarmonicSync", "EchelonVault", "QuantumPulse", "VelocityBridge", "VerdantNode",
            "NebularMesh", "UpliftGrid", "TranscendHost", "ProphetCore", "OracleRelay",
            "PanoramaSync", "MajesticVault", "AllegiantNode", "MirageNet", "NimbusBridge",
            "TitanGrid", "TerraRelay", "SentinelSync", "PinnacleMesh", "EmpyreanVault",
            "OdysseyBeacon", "EpochNode", "VanguardGrid", "AuroraRelay", "CelestialFlow",
            "SummitHost", "EtherealSync", "EquinoxPulse", "MomentumRelay", "HorizonVault",
            "ParadoxCore", "GenesisSync", "SovereignBeacon", "PrimeRelay", "QuantumNode",
            "FusionSync", "SynergyCore", "GlacialVault", "SpectralGrid", "AscendRelay",
            "ElevateNode", "NovaBeacon", "StellarSync", "ZenithCore", "ApexHost",
            "TerraVault", "OdysseyGrid", "InfinityBeacon", "HyperionRelay", "MomentumPulse",
            "OrionNet", "SingularitySync", "EclipticVault", "StratosphereRelay", "TitaniumNode",
            "VelocityPulse", "CyberSync", "EnclaveVault", "HyperDriveGrid", "UtopiaBeacon",
            "ExodusSync", "LuminousVault", "EmpyreanRelay", "PanoramaGrid", "CatalystCore",
            "SolsticeSync", "ChronicleVault", "EchoRelay", "VerdantPulse", "AetherSync",
            "NebulaBridge", "CelestialVault", "EpochSync", "ProphetGrid", "QuantumSync",
            "DynastyPulse", "EtherealRelay", "ElevateBeacon", "NovaSync", "NimbusVault",
            "AllegiantCore", "TranscendGrid", "ParagonSync", "PioneerRelay", "MajesticHost",
            "HorizonNode", "SolaceGrid", "MomentumBeacon", "ZenithRelay", "GlacialSync",
            "LegacyVault", "VanguardCore", "PinnaclePulse", "OracleGrid", "EquinoxSync",
            "TranquilVault", "SpectralRelay", "HyperionSync", "CosmicPulse", "NebularVault"
        ];
    }
}
