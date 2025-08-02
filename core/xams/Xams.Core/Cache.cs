using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;
using Xams.Core.Startup;
using Xams.Core.Utils;
// ReSharper disable UnusedAutoPropertyAccessor.Global

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

        internal static async Task Initialize(IDataService dataService)
        {
            var dbContext = dataService.GetDataRepository().CreateNewDbContext();
            var dbContextType = dbContext.GetType();
            
            Random rnd = new Random(DateTime.Now.Millisecond);
            var serverName = Environment.GetEnvironmentVariable("SERVER_NAME") ??
                             ServerNames[rnd.Next(0, ServerNames.Length)] + rnd.Next(0, 9999);
            Console.WriteLine($"Initializing Cache for server {serverName}");
            Cache cache = new Cache();
            Instance = cache;

            // Cache entity metadata
            var props = dbContextType.GetProperties();
            var nullabilityInfoContext = new NullabilityInfoContext();
            foreach (var prop in props)
            {
                var propType = prop.PropertyType;
                // DbSets only
                if (prop.PropertyType.Name != "DbSet`1")
                {
                    continue;
                }
                if (propType.GenericTypeArguments.Length > 0)
                {
                    var entityType = propType.GetGenericArguments()[0];
                    Instance.ValidateMetadata(dbContext, entityType);
                    
                    string tableName = dbContext.Model.FindEntityType(entityType)?.GetTableName() ?? 
                                       throw new Exception($"Failed to get table name. Entity type {entityType} not found");

                    var discriminatorPropertyName = dbContext.Model.FindEntityType(entityType)?.GetDiscriminatorPropertyName();
                    
                    if (!TryFindPrimaryKeyProperties(dbContext, entityType, out var primaryKeyProperty))
                    {
                        continue;
                    }

                    if (primaryKeyProperty == null)
                    {
                        continue;
                    }

                    // Find the primary key property
                    var primaryKeyType = primaryKeyProperty.First().PropertyType;
                    var primaryKeyName = primaryKeyProperty.First().Name;

                    var tableMetadata = new MetadataInfo()
                    {
                        Type = entityType,
                        DisplayNameAttribute =
                            entityType.GetCustomAttribute<UIDisplayNameAttribute>()
                            ??
                            new UIDisplayNameAttribute(tableName,
                                EntityUtil.IsSystemEntity(entityType) ? "System" : ""),
                        TableName = tableName,
                        PrimaryKey = primaryKeyName,
                        PrimaryKeyProperty = primaryKeyProperty.First(),
                        PrimaryKeyType = primaryKeyType,
                        NameProperty = entityType
                                           .GetProperties()
                                           .FirstOrDefault(x => x.GetCustomAttribute<UINameAttribute>() != null)
                                       ?? entityType.GetProperty("Name"),
                        IsProxy = entityType.GetCustomAttribute<UIProxyAttribute>() != null,
                        HasOwningUserField = entityType.GetProperty("OwningUserId") != null,
                        HasOwningTeamField = entityType.GetProperty("OwningTeamId") != null
                    };
                    cache.TableMetadata.Add(tableName, tableMetadata);
                    cache.TableTypeMetadata.Add(entityType, tableMetadata);
                    if (entityType.BaseType != null)
                    {
                        cache.TableTypeMetadata.TryAdd(entityType.BaseType, tableMetadata);
                    }
                    
                    // Table Required fields
                    var tableRequiredFields = entityType.GetCustomAttribute<UIRequiredAttribute>()?.Fields ?? [];
                    var tableReadOnlyFields = entityType.GetCustomAttribute<UIReadOnlyAttribute>()?.Fields ?? [];
                    var tableRecommendedFields = entityType.GetCustomAttribute<UIRecommendedAttribute>()?.Fields ?? [];

                    var owningUserDisplayName = entityType.GetCustomAttribute<UIDisplayNameOwningUserAttribute>();
                    var owningTeamDisplayName = entityType.GetCustomAttribute<UIDisplayNameOwningTeamAttribute>();
                    var createdDateDisplayName = entityType.GetCustomAttribute<UIDisplayNameCreatedDateAttribute>();
                    var updatedDateDisplayName = entityType.GetCustomAttribute<UIDisplayNameUpdatedDateAttribute>();
                    var isActiveDisplayName = entityType.GetCustomAttribute<UIDisplayNameIsActiveAttribute>();
                    var createdByDisplayName = entityType.GetCustomAttribute<UIDisplayNameCreatedByAttribute>();
                    var updatedByDisplayName = entityType.GetCustomAttribute<UIDisplayNameUpdatedByAttribute>();

                    // Get Metadata Output for the metadata endpoint
                    var properties = tableMetadata.Type.GetProperties();
                    List<MetadataField> fields = new List<MetadataField>();
                    foreach (var property in properties)
                    {
                        // Skip ICollection
                        if (!property.IsValidEntityProperty())
                            continue;

                        // Ignore the hidden fields
                        if (Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null)
                            continue;

                        // Ignore the foreign key field
                        if (!property.IsPrimitive() && properties.Any(x => x.Name == $"{property.Name}Id"))
                            continue;
                        
                        // Ignore discriminator column
                        if (!string.IsNullOrEmpty(discriminatorPropertyName) && property.Name == discriminatorPropertyName)
                            continue;

                        bool isLookup = tableMetadata.Type.IsLookupField(property.Name);

                        string? lookupTableNameField = string.Empty;
                        string? lookuptableDescriptionField = string.Empty;
                        string lookupTable = string.Empty;
                        bool lookupTablHasActiveField = false;
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
                            
                            lookupTablHasActiveField = lookupProperty?.PropertyType.GetProperty("IsActive") != null;
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

                        string displayName = displayNameAttribute.Name;

                        if (property.Name == "OwningUserId" && owningUserDisplayName != null)
                        {
                            displayName = owningUserDisplayName.DisplayName;
                        }
                        if (property.Name == "OwningTeamId" && owningTeamDisplayName != null)
                        {
                            displayName = owningTeamDisplayName.DisplayName;
                        }
                        if (property.Name == "CreatedDate" && createdDateDisplayName != null)
                        {
                            displayName = createdDateDisplayName.DisplayName;
                        }
                        if (property.Name == "UpdatedDate" && updatedDateDisplayName != null)
                        {
                            displayName = updatedDateDisplayName.DisplayName;
                        }
                        if (property.Name == "IsActive" && isActiveDisplayName != null)
                        {
                            displayName = isActiveDisplayName.DisplayName;
                        }
                        if (property.Name == "CreatedById" && createdByDisplayName != null)
                        {
                            displayName = createdByDisplayName.DisplayName;
                        }
                        if (property.Name == "UpdatedById" && updatedByDisplayName != null)
                        {
                            displayName = updatedByDisplayName.DisplayName;
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
                            displayName = displayName,
                            type = fieldType,
                            characterLimit = characterLimitAttribute?.Limit,
                            order = layoutAttribute.Order,
                            lookupName = isLookup ? property.Name.Substring(0, property.Name.Length - 2) : null,
                            lookupTable = lookupTable,
                            lookupTableNameField = lookupTableNameField,
                            lookupTableDescriptionField = lookuptableDescriptionField,
                            lookupTableHasActiveField = lookupTablHasActiveField,
                            dateFormat = dateFormatAttribute?.DateFormat,
                            isNullable = isNullable,
                            isRequired = requiredAttribute != null || tableRequiredFields.Contains(property.Name),
                            isRecommended = recommendedAttribute != null || tableRecommendedFields.Contains(property.Name),
                            isReadOnly = readOnlyAttribute != null || tableReadOnlyFields.Contains(property.Name),
                            option = optionAttribute?.Name ?? "",
                            numberRange = numberRangeAttribute != null
                                ? $"{numberRangeAttribute.Min}-{numberRangeAttribute.Max}"
                                : null
                        });
                    }

                    fields = fields.OrderBy(x => x.order).ToList();
                    
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
                        field.lookupTablePrimaryKeyField = cache.GetTableMetadata(field.lookupTable).PrimaryKey;
                    }
                }
            }

            Instance.ValidateSystemEntities();
            
            // Repair default values for Sqlite
            await SqliteUtil.Repair(dbContext);
            // Ensure all referenced assemblies are loaded first
            cache.LoadAllReferencedAssemblies();
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
                    JobInitialStateAttribute? jobStateAttribute = type.GetCustomAttribute<JobInitialStateAttribute>();

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
                        State = jobStateAttribute?.State ?? JobState.Active,
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
            using (var db = dataService.GetDataRepository().CreateNewDbContext())
            {
                db.Add(serverEntity);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Finds the primary key properties of an entity type.
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="entityType">The entity type to find the primary key for.</param>
        /// <param name="primaryKeyProperties"></param>
        /// <returns>The PropertyInfo of the primary key property.</returns>
        private static bool TryFindPrimaryKeyProperties(IXamsDbContext dbContext, Type entityType, out PropertyInfo[]? primaryKeyProperties)
        {
            var primaryKey = dbContext.Model.FindEntityType(entityType)?.FindPrimaryKey();
            if (primaryKey == null)
            {
                primaryKeyProperties = null;
                return false;
            }
            
            var properties = primaryKey.Properties;
            if (properties.Count == 0)
            {
                primaryKeyProperties = null;
                return false;
            }
            primaryKeyProperties = properties
                .Where(x => x.PropertyInfo != null)
                .Select(x =>
                {
                    if (x.PropertyInfo == null)
                    {
                        throw new Exception($"Property not found");
                    }
                    return x.PropertyInfo;
                }).ToArray();
            return true;
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
            if (!TableTypeMetadata.TryGetValue(entityType, out var metadata))
            {
                throw new Exception($"Table {entityType.Name} not found in metadata");
            }
            return metadata;
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

                    // if (TableMetadata[entity.Name].Type.GetProperty(entityProperty.Name)!.PropertyType.Name !=
                    //     entityProperty.Type)
                    // {
                    //     throw new Exception($"Property {entityProperty.Name} type mismatch in {entity.Name}");
                    // }
                }
            }
        }

        private void ValidateMetadata(IXamsDbContext dbContext, Type tableType)
        {
            List<string> warnings = new();

            var tableAttribute = tableType.GetCustomAttribute<TableAttribute>();
            if (tableAttribute == null)
            {
                warnings.Add($"TableAttribute not found for {tableType.Name}");
            }
            else
            {
                if (string.IsNullOrEmpty(tableAttribute.Name))
                {
                    warnings.Add($"TableAttribute Name not found for {tableType.Name}");
                }

                if (TableMetadata.ContainsKey(tableAttribute.Name))
                {
                    warnings.Add($"Duplicate TableAttribute Name: {tableAttribute.Name}");
                }
                
            }

            // Check for primary key
            if (!TryFindPrimaryKeyProperties(dbContext, tableType, out _))
            {
                warnings.Add($"Unable to find primary key property for {tableAttribute?.Name ?? tableType.Name}");
            }

            var nameAttrProp = tableType.GetProperties()
                .FirstOrDefault(x => x.GetCustomAttribute<UINameAttribute>() != null);

            if (!EntityUtil.IsSystemEntity(tableType))
            {
                if (nameAttrProp != null)
                {
                    if (nameAttrProp.PropertyType != typeof(string))
                    {
                        warnings.Add($"Name Property {nameAttrProp.Name} must be of type string for {tableType.Name}");
                    }
                }
                else
                {
                    var nameProperty = tableType.GetProperty("Name");
                    if (nameProperty == null)
                    {
                        warnings.Add($"Name Property not found for {tableType.Name}");
                    }
                    else
                    {
                        if (nameProperty.PropertyType != typeof(string))
                        {
                            warnings.Add($"Name Property must be of type string for {tableType.Name}");
                        }
                    }
                }
            }


            if (warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var error in warnings)
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
        
        /// <summary>
        /// Pre-Load referenced assemblies so all service logic, jobs, actions, etc. are loaded at time of caching
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void LoadAllReferencedAssemblies()
        {
            var loaded = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Where(a => !string.IsNullOrEmpty(a.FullName))
                .Select(a => a.FullName ?? ""));

            var toLoad = new Queue<Assembly>();
            var entryAssembly = Assembly.GetEntryAssembly();

            if (entryAssembly == null)
            {
                throw new Exception($"Unable to find entry assembly {entryAssembly?.FullName}");
            }
            
            toLoad.Enqueue(entryAssembly);

            while (toLoad.Count > 0)
            {
                var asm = toLoad.Dequeue();

                foreach (var reference in asm.GetReferencedAssemblies())
                {
                    if (loaded.Contains(reference.FullName)) continue;

                    try
                    {
                        var loadedAsm = Assembly.Load(reference);
                        toLoad.Enqueue(loadedAsm);
                        loaded.Add(reference.FullName);
                    }
                    catch
                    {
                        Console.Error.WriteLine($"Failed to load referenced assembly: {reference.FullName}");
                        // swallow or log — some might not load
                    }
                }
            }
        }




        public class MetadataInfo
        {
            public Type Type { get; set; } = null!;
            public string TableName { get; set; } = null!;
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
            public required List<EntityProperty> Properties { get; set; } = null!;
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
            public JobState State { get; internal set; }
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
