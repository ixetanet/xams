# Xams.Core API Reference

Auto-generated API documentation for Xams.Core library.
**Generated:** 2025-10-12 17:52:00 | **Version:** 1.0.10.0

## Xams.Core

### Class: AddXamsApiOptions
**Constructors:**
• `AddXamsApiOptions()`
**Properties:**
• `FirebaseConfig`: `FirebaseConfig?` { get; set }
• `RequireAuthorization`: `Boolean` { get; set }
• `UrlPath`: `String` { get; set }
• `UseDashboard`: `Boolean` { get; set }

### Class: AuditInfo
**Constructors:**
• `AuditInfo()`
**Properties:**
• `FieldAuditInfos`: `Dictionary<String, FieldAuditInfo>` { get }
• `IsCreateAuditEnabled`: `Boolean` { get; set }
• `IsDeleteAuditEnabled`: `Boolean` { get; set }
• `IsReadAuditEnabled`: `Boolean` { get; set }
• `IsUpdateAuditEnabled`: `Boolean` { get; set }

### Class: Cache
**Constructors:**
• `Cache()`
**Properties:**
• `AuditRefreshTime`: `DateTime` { get; set }
• `BulkServiceLogics`: `List<ServiceBulkInfo>` { get; set }
• `Instance`: `Cache` { get; set }
• `IsLogsEnabled`: `Boolean` { get; set }
• `ServerId`: `Guid` { get; set }
• `ServerName`: `String?` { get; set }
• `ServiceSecurityInfos`: `List<ServiceSecurityInfo>` { get; set }
• `ServiceStartupInfos`: `List<ServiceStartupInfo>` { get; set }
**Methods:**
• `GetTableMetadata(String tableName)`: `MetadataInfo`
• `GetTableMetadata(Type entityType)`: `MetadataInfo`

### Class: Entity
**Constructors:**
• `Entity()`
**Properties:**
• `Name`: `String` { get; set }
• `Properties`: `List<EntityProperty>` { get; set }

### Class: EntityProperty
**Constructors:**
• `EntityProperty()`
**Properties:**
• `Name`: `String` { get; set }
• `Type`: `String` { get; set }

### Class: FieldAuditInfo
**Constructors:**
• `FieldAuditInfo()`
**Properties:**
• `IsCreateAuditEnabled`: `Boolean` { get; set }
• `IsDeleteAuditEnabled`: `Boolean` { get; set }
• `IsUpdateAuditEnabled`: `Boolean` { get; set }

### Class: MetadataInfo
**Constructors:**
• `MetadataInfo()`
**Properties:**
• `DisplayNameAttribute`: `UIDisplayNameAttribute?` { get; set }
• `HasDeleteServiceLogic`: `Boolean` { get; set }
• `HasOwningTeamField`: `Boolean` { get; set }
• `HasOwningUserField`: `Boolean` { get; set }
• `HasPostOpServiceLogic`: `Boolean` { get; set }
• `HasPreOpServiceLogic`: `Boolean` { get; set }
• `IsProxy`: `Boolean` { get; set }
• `MetadataOutput`: `MetadataOutput` { get; set }
• `NameProperty`: `PropertyInfo?` { get; set }
• `PrimaryKey`: `String` { get; set }
• `PrimaryKeyProperty`: `PropertyInfo` { get; set }
• `PrimaryKeyType`: `Type` { get; set }
• `TableName`: `String` { get; set }
• `Type`: `Type` { get; set }

### Class: ServiceActionInfo
**Constructors:**
• `ServiceActionInfo()`
**Properties:**
• `ServiceActionAttribute`: `ServiceActionAttribute` { get; set }
• `Type`: `Type` { get; set }

### Class: ServiceBulkInfo
**Constructors:**
• `ServiceBulkInfo()`
**Properties:**
• `BulkServiceAttribute`: `BulkServiceAttribute` { get; set }
• `Type`: `Type` { get; set }

### Class: ServiceHubInfo
**Constructors:**
• `ServiceHubInfo()`
**Properties:**
• `ServiceHubAttribute`: `ServiceHubAttribute` { get; set }
• `Type`: `Type` { get; set }

### Class: ServiceJobInfo
**Constructors:**
• `ServiceJobInfo()`
**Properties:**
• `ExecuteJobOn`: `ExecuteJobOn` { get; set }
• `ServerName`: `String` { get; set }
• `ServiceJobAttribute`: `ServiceJobAttribute` { get; set }
• `State`: `JobState` { get; set }
• `TimeZone`: `String` { get; set }
• `Type`: `Type` { get; set }

### Class: ServiceLogicInfo
**Constructors:**
• `ServiceLogicInfo()`
**Properties:**
• `ServiceLogicAttribute`: `ServiceLogicAttribute` { get; set }
• `Type`: `Type` { get; set }

### Class: ServicePermissionInfo
**Constructors:**
• `ServicePermissionInfo()`
**Properties:**
• `Type`: `Type` { get; set }

### Class: ServiceSecurityInfo
**Constructors:**
• `ServiceSecurityInfo()`
**Properties:**
• `ServiceSecurityAttribute`: `ServiceSecurityAttribute` { get; set }
• `Type`: `Type` { get; set }

### Class: ServiceStartupInfo
**Constructors:**
• `ServiceStartupInfo()`
**Properties:**
• `ServiceStartupAttribute`: `ServiceStartupAttribute` { get; set }
• `Type`: `Type` { get; set }

### Class: TablePermission
**Constructors:**
• `TablePermission()`
**Properties:**
• `Permissions`: `List<String>` { get; set }
• `Table`: `String` { get; set }

### Class: User
**Constructors:**
• `User()`
**Properties:**
• `CreatedDate`: `DateTime` { get; set }

### Class: UserConnection
**Constructors:**
• `UserConnection()`
**Properties:**
• `ConnectionHubs`: `Dictionary<String, List`1>` { get; set }
• `ConnectionIds`: `HashSet<String>` { get; set }
• `NumberOfConnections`: `Int32` { get; set }

### Enum: PermissionLevel
**Inherits:** `Enum`
**Values:** 
`System`, `Team`, `User`

### Static Class: PermissionCache
**Properties:**
• `RolePermissions`: `ReadOnlyDictionary<Guid, HashSet`1>` { get } - RoleId, Permission Names
• `ServiceProvider`: `IServiceProvider` { get; set }
• `TeamRoles`: `ReadOnlyDictionary<Guid, List`1>` { get } - TeamId, RoleIds
• `UserRoles`: `ReadOnlyDictionary<Guid, List`1>` { get } - UserId, RoleIds
• `Users`: `ReadOnlyDictionary<Guid, User>` { get } - Hashset of all the known users in the system.
• `UserTeams`: `ReadOnlyDictionary<Guid, List`1>` { get } - UserId, TeamIds
**Methods:**
• `CacheRolePermissions(IXamsDbContext db, Guid? roleId = null)`: `Task`
• `CacheTeamRoles(IXamsDbContext db, Guid? teamId = null)`: `Task`
• `CacheUserRoles(IXamsDbContext db, Guid? userId = null)`: `Task`
• `CacheUsers(IXamsDbContext db, Guid? userId = null)`: `Task`
• `CacheUserTeams(IXamsDbContext db, Guid? userId = null)`: `Task`
• `GetUserPermissions(Guid userId, String[]? permissionNames = null)`: `Task<String[]>`
• `RemovePermission(String permissionName)`: `void`
• `RemoveRole(Guid roleId)`: `void`
• `RemoveTeam(Guid teamId)`: `void`
• `RemoveUser(Guid userId)`: `void`
• `UpdatePermission(String oldName, String newName)`: `void`

### Static Class: Permissions
**Methods:**
• `GetHighestPermission(String[]? permissions, String tableName = "")`: `PermissionLevel?`
• `GetUserTablePermissions(Guid userId, String tableName, String[] operations)`: `Task<String[]>`
• `GetUserTablePermissions(Guid userId, String[] tableNames, String operation)`: `Task<String[]>`

### Static Class: ProgramStart

### Static Class: SignalRConfiguration

## Xams.Core.Actions

### Class: ADMIN_ExportData
**Implements:** `IServiceAction`
**Constructors:**
• `ADMIN_ExportData()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`
• `TopologicalSort(DependencyInfo[] tables)`: `List<String>`

### Class: ADMIN_ExportDependencies
**Implements:** `IServiceAction`
**Constructors:**
• `ADMIN_ExportDependencies()`
**Methods:**
• `BuildDependencyGraph(IXamsDbContext context)`: `List<DependencyInfo>`
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: ADMIN_GetTypes
Return tables from metadata as typescript types

**Implements:** `IServiceAction`
**Constructors:**
• `ADMIN_GetTypes()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: ADMIN_ImportData
**Implements:** `IServiceAction`
**Constructors:**
• `ADMIN_ImportData()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: ADMIN_TriggerJob
**Implements:** `IServiceAction`
**Constructors:**
• `ADMIN_TriggerJob()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: DependencyInfo
**Constructors:**
• `DependencyInfo()`
**Properties:**
• `dependencies`: `DependentTable[]` { get; set }
• `name`: `String` { get; set }

### Class: DependencyInfo
**Constructors:**
• `DependencyInfo()`
**Properties:**
• `Dependencies`: `List<Dependent>` { get; set }
• `Name`: `String` { get; set }

### Class: Dependent
**Constructors:**
• `Dependent()`
**Properties:**
• `Name`: `String` { get; set }
• `Optional`: `Boolean` { get; set }

### Class: DependentTable
**Constructors:**
• `DependentTable()`
**Properties:**
• `name`: `String` { get; set }

### Class: RecordImport
**Constructors:**
• `RecordImport()`
**Properties:**
• `ImportType`: `ImportType` { get; set }
• `Record`: `Object` { get; set }
• `RowNumber`: `Int32` { get; set }

### Class: TABLE_ExportData
**Implements:** `IServiceAction`
**Constructors:**
• `TABLE_ExportData()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: TABLE_ImportData
**Implements:** `IServiceAction`
**Constructors:**
• `TABLE_ImportData()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: TABLE_ImportTemplate
**Implements:** `IServiceAction`
**Constructors:**
• `TABLE_ImportTemplate()`
**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Class: TableExport
**Constructors:**
• `TableExport()`
**Properties:**
• `data`: `Object` { get; set }
• `tableName`: `String` { get; set }

### Enum: ImportType
**Inherits:** `Enum`
**Values:** 
`Create`, `Update`

## Xams.Core.Actions.Shared

### Static Class: TABLE_ImportExport
**Methods:**
• `GetTableProperties(Type type)`: `List<PropertyInfo>`

## Xams.Core.Attributes

### Attribute: BulkServiceAttribute
**Inherits:** `Attribute`
**Constructors:**
• `BulkServiceAttribute(BulkStage bulkStage, Int32 order = 0)`
**Properties:**
• `Order`: `Int32` { get; set }
• `Stage`: `BulkStage` { get; set }

### Attribute: CascadeDeleteAttribute
**Inherits:** `Attribute`
**Constructors:**
• `CascadeDeleteAttribute()`

### Attribute: JobInitialStateAttribute
**Inherits:** `Attribute`
**Constructors:**
• `JobInitialStateAttribute(JobState state)`
**Properties:**
• `State`: `JobState` { get }

### Attribute: JobServerAttribute
**Inherits:** `Attribute`
**Constructors:**
• `JobServerAttribute(ExecuteJobOn executeJobOn, String serverName = null)`
**Properties:**
• `ExecuteJobOn`: `ExecuteJobOn` { get }
• `ServerName`: `String` { get }

### Attribute: JobTimeZone
**Inherits:** `Attribute`
**Constructors:**
• `JobTimeZone(String timeZone)`
**Properties:**
• `TimeZone`: `String` { get; set }

### Attribute: ModuleDbContextAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ModuleDbContextAttribute()`

### Attribute: ServiceActionAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServiceActionAttribute(String name)`
**Properties:**
• `Name`: `String` { get; set }

### Attribute: ServiceHubAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServiceHubAttribute(String name)`
**Properties:**
• `Name`: `String` { get; set }

### Attribute: ServiceJobAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServiceJobAttribute(String name, String queue, String timeSpan, JobSchedule jobSchedule = 1, DaysOfWeek daysOfWeek = 127, String tag = "")`
  If JobSchedule is TimeOfDay, then TimeSpan is the time of day to run the job in UTC, ie: "18:13:59" for 6:13:59 PM UTC,
            otherwise specify Interval with hours:minutes:seconds
  - `name`: 
  - `queue`: 
  - `timeSpan`: 
  - `jobSchedule`: 
  - `daysOfWeek`: 
  - `tag`: 
**Properties:**
• `DaysOfWeek`: `DaysOfWeek` { get }
• `JobSchedule`: `JobSchedule` { get }
• `Name`: `String` { get }
• `Queue`: `String` { get }
• `Tag`: `String` { get }
• `TimeSpan`: `TimeSpan` { get }

### Attribute: ServiceLogicAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServiceLogicAttribute(String tableName, DataOperation dataOperation, LogicStage logicStage, Int32 order = 0)`
**Properties:**
• `DataOperation`: `DataOperation` { get; set }
• `LogicStage`: `LogicStage` { get; set }
• `Order`: `Int32` { get; set }
• `TableName`: `String` { get; set }

### Attribute: ServicePermissionAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServicePermissionAttribute()`

### Attribute: ServiceSecurityAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServiceSecurityAttribute(Int32 order = 100)`
**Properties:**
• `Order`: `Int32` { get; set }

### Attribute: ServiceStartupAttribute
**Inherits:** `Attribute`
**Constructors:**
• `ServiceStartupAttribute(StartupOperation startupOperation, Int32 order = 0)`
**Properties:**
• `Order`: `Int32` { get; set }
• `StartupOperation`: `StartupOperation` { get; set }

### Attribute: UICharacterLimitAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UICharacterLimitAttribute(Int32 limit)`
**Properties:**
• `Limit`: `Int32` { get; set }

### Attribute: UIDateFormatAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDateFormatAttribute(String dateFormat)`
**Properties:**
• `DateFormat`: `String` { get; set }
**Methods:**
• `HasTimePart()`: `Boolean`

### Attribute: UIDescriptionAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDescriptionAttribute()`

### Attribute: UIDisplayNameAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameAttribute(Field[] fields)`
• `UIDisplayNameAttribute(String name, String tag = "")`
**Properties:**
• `Name`: `String` { get; set }
• `Tag`: `String` { get; set }

### Attribute: UIDisplayNameCreatedByAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameCreatedByAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIDisplayNameCreatedDateAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameCreatedDateAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIDisplayNameIsActiveAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameIsActiveAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIDisplayNameOwningTeamAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameOwningTeamAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIDisplayNameOwningUserAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameOwningUserAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIDisplayNameUpdatedByAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameUpdatedByAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIDisplayNameUpdatedDateAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIDisplayNameUpdatedDateAttribute(String displayName)`
**Properties:**
• `DisplayName`: `String` { get; set }

### Attribute: UIHideAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIHideAttribute(Boolean queryable = false)`
  Field cannot be read from the UI.
  - `queryable`: If true, this field can still be used as a filter from the UI.
**Properties:**
• `Queryable`: `Boolean` { get; set }

### Attribute: UINameAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UINameAttribute()`

### Attribute: UINumberRangeAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UINumberRangeAttribute(Single min, Single max)`
**Properties:**
• `Max`: `Single` { get; set }
• `Min`: `Single` { get; set }

### Attribute: UIOptionAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIOptionAttribute(String name)`
**Properties:**
• `Name`: `String` { get; set }

### Attribute: UIOrderAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIOrderAttribute(Int32 order)`
**Properties:**
• `Order`: `Int32` { get; set }

### Attribute: UIProxyAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIProxyAttribute()`

### Attribute: UIReadOnlyAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIReadOnlyAttribute()`
• `UIReadOnlyAttribute(String[] fields)`
**Properties:**
• `Fields`: `String[]` { get; set }

### Attribute: UIRecommendedAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIRecommendedAttribute()`
• `UIRecommendedAttribute(String[] fields)`
**Properties:**
• `Fields`: `String[]` { get; set }

### Attribute: UIRequiredAttribute
**Inherits:** `Attribute`
**Constructors:**
• `UIRequiredAttribute()`
• `UIRequiredAttribute(String[] fields)`
**Properties:**
• `Fields`: `String[]` { get; set }

### Attribute: UISetFieldFromLookupAttribute
Sets a string field on the entity to the lookupname value of a related entity

**Inherits:** `Attribute`
**Constructors:**
• `UISetFieldFromLookupAttribute(String lookupIdProperty)`
**Properties:**
• `LookupIdProperty`: `String` { get; set }

### Enum: BulkStage
**Inherits:** `Enum`
**Values:** 
`Pre`, `Post`

### Enum: DataOperation
**Inherits:** `Enum`
**Values:** 
`Create`, `Read`, `Update`, `Delete`, `Action`

### Enum: DaysOfWeek
**Inherits:** `Enum`
**Values:** 
`None`, `Monday`, `Tuesday`, `Wednesday`, `Thursday`, `Friday`, `Saturday`, `Sunday`, `Weekdays`, `Weekend`, `All`

### Enum: ExecuteJobOn
**Inherits:** `Enum`
**Values:** 
`All`, `One`

### Enum: JobSchedule
**Inherits:** `Enum`
**Values:** 
`TimeOfDay`, `Interval`

### Enum: JobState
**Inherits:** `Enum`
**Values:** 
`Active`, `Inactive`

### Enum: LogicStage
**Inherits:** `Enum`
**Values:** 
`PreValidation`, `PreOperation`, `PostOperation`

### Enum: StartupOperation
**Inherits:** `Enum`
**Values:** 
`Pre`, `Post`

### Struct: Field
**Constructors:**
• `Field()`
**Properties:**
• `DisplayName`: `String` { get; set }
• `Name`: `String` { get; set }

## Xams.Core.Base

### Class: BaseEntity
**Constructors:**
• `BaseEntity()`
**Properties:**
• `CreatedBy`: `User` { get; set }
• `CreatedById`: `Guid` { get; set }
• `CreatedDate`: `DateTime` { get; set }
• `IsActive`: `Boolean` { get; set }
• `OwningTeam`: `Team` { get; set }
• `OwningTeamId`: `Guid?` { get; set }
• `OwningUser`: `User` { get; set }
• `OwningUserId`: `Guid?` { get; set }
• `UpdatedBy`: `User` { get; set }
• `UpdatedById`: `Guid` { get; set }
• `UpdatedDate`: `DateTime` { get; set }

### Class: BasePipelineStage
**Implements:** `IPipelineStage`
**Constructors:**
• `BasePipelineStage()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`
• `SetNext(IPipelineStage nextItem)`: `void`

### Class: BaseServiceContext
**Constructors:**
• `BaseServiceContext(PipelineContext pipelineContext)`
**Properties:**
• `ExecutingUserId`: `Guid` { get }
• `ExecutionId`: `Guid` { get }
• `Parameters`: `Dictionary<String, JsonElement>` { get }
**Methods:**
• `Create(Object entity, Object? parameters = null)`: `Task<T>`
• `Create(Guid executingUserId, Object entity, Object? parameters = null)`: `Task<T>`
• `CreateNewDbContext()`: `T` where T : IXamsDbContext
• `Delete(Object entity, Object? parameters = null)`: `Task`
  Delete record and execute service logic.
  - `entity`: Entity
  - `parameters`: Any type (can be anonymous)

• `Delete(Guid executingUserId, Object entity, Object? parameters = null)`: `Task`
  Delete record as a specific user and execute service logic.
  - `executingUserId`: User to execute as
  - `entity`: Entity
  - `parameters`: Any type (can be anonymous)

• `ExecuteJob(JobOptions jobOptions)`: `Task<Guid>`
  Execute Job. Does not wait for the job to complete.

**Returns:** JobHistoryId
  - `jobOptions`: 

• `GetDbContext()`: `T` where T : IXamsDbContext
• `GetParameters()`: `T`
• `HubSend(Object message)`: `Task<Response`1>` where T : class, IServiceHub
• `Permissions(Guid userId, String[] permissions)`: `Task<String[]>`
  Return an array of matching permissions the user has.
  - `userId`: 
  - `permissions`: 

• `Update(Object entity, Object? parameters = null)`: `Task<T>`
• `Update(Guid executingUserId, Object entity, Object? parameters = null)`: `Task<T>`
• `Upsert(Object entity, Object? parameters = null)`: `Task<T>`
• `Upsert(Guid executingUserId, Object entity, Object? parameters = null)`: `Task<T>`

### Enum: DbProvider
**Inherits:** `Enum`
**Values:** 
`MySQL`, `SQLServer`, `SQLite`, `Postgres`

### Enum: EntityType<TUser, TTeam, TRole, TSetting>
**Inherits:** `Enum`
**Values:** 
`Default`, `Extended`

### Interface: IXamsDbContext
**Implements:** `IDisposable`
**Methods:**
• `AddRange(Object[] entities)`: `void`
• `AddRange(IEnumerable<Object> entities)`: `void`
• `AddRangeAsync(Object[] entities)`: `Task`
• `AddRangeAsync(IEnumerable<Object> entities, CancellationToken cancellationToken)`: `Task`
• `AttachRange(Object[] entities)`: `void`
• `AttachRange(IEnumerable<Object> entities)`: `void`
• `Dispose()`: `void`
• `DisposeAsync()`: `ValueTask`
• `ExecuteRawSql(String postgresSql, Dictionary<String, Object>? parameters = null)`: `Task`
• `Find(Type entityType, Object[] keyValues)`: `Object`
• `FindAsync(Type entityType, Object[]? keyValues)`: `ValueTask<Object>`
• `FindAsync(Type entityType, Object[]? keyValues, CancellationToken cancellationToken)`: `ValueTask<Object>`
• `GetDbProvider()`: `DbProvider`
  Returns the current database provider.

• `RemoveRange(Object[] entities)`: `void`
• `RemoveRange(IEnumerable<Object> entities)`: `void`
• `SaveChanges()`: `Int32`
• `SaveChanges(Boolean acceptAllChangesOnSuccess)`: `Int32`
• `SaveChangesAsync(CancellationToken cancellationToken = null)`: `Task<Int32>`
• `SaveChangesAsync(Boolean acceptAllChangesOnSuccess, CancellationToken cancellationToken)`: `Task<Int32>`
• `SaveChangesCalledWithPendingChanges()`: `Boolean`
• `UpdateRange(Object[] entities)`: `void`
• `UpdateRange(IEnumerable<Object> entities)`: `void`

## Xams.Core.Config

### Class: FirebaseConfig
**Constructors:**
• `FirebaseConfig()`
**Properties:**
• `apiKey`: `String` { get; set }
• `appId`: `String` { get; set }
• `authDomain`: `String` { get; set }
• `enableSmsMfa`: `Boolean` { get; set }
• `measurementId`: `String` { get; set }
• `messagingSenderId`: `String` { get; set }
• `projectId`: `String` { get; set }
• `providers`: `String[]` { get; set }
• `storageBucket`: `String` { get; set }

## Xams.Core.Contexts

### Class: ActionServiceContext
**Inherits:** `BaseServiceContext`
**Constructors:**
• `ActionServiceContext(PipelineContext pipelineContext)`
**Properties:**
• `DataOperation`: `DataOperation` { get }

### Class: BulkServiceContext
**Inherits:** `BaseServiceContext`
**Constructors:**
• `BulkServiceContext(PipelineContext pipelineContext, List<ServiceContext> allServiceContexts)`
**Properties:**
• `AllServiceContexts`: `List<ServiceContext>` { get; set }

### Class: HubContext
**Inherits:** `BaseServiceContext`
**Constructors:**
• `HubContext(PipelineContext pipelineContext, String message, SignalRHub signalRHub)`
**Properties:**
• `Message`: `String` { get }
**Methods:**
• `GetMessage()`: `T`

### Class: HubSendContext
**Inherits:** `BaseServiceContext`
### Class: JobServiceContext
**Inherits:** `BaseServiceContext`
**Constructors:**
• `JobServiceContext(PipelineContext pipelineContext)`

### Class: PermissionContext
**Constructors:**
• `PermissionContext(IDataService dataService)`
**Methods:**
• `CreateNewDbContext()`: `T` where T : IXamsDbContext
• `GetDbContext()`: `T` where T : IXamsDbContext

### Class: ServiceContext
**Inherits:** `BaseServiceContext`
**Constructors:**
• `ServiceContext(PipelineContext pipelineContext, LogicStage logicStage, DataOperation dataOperation, List<Object>? entities, ReadOutput? readOutput)`
**Properties:**
• `DataOperation`: `DataOperation` { get; set }
• `Depth`: `Int32` { get }
• `Entities`: `List<Object>` { get; set }
• `Entity`: `Object?` { get }
• `LogicStage`: `LogicStage` { get; set }
• `Parent`: `ServiceContext?` { get }
• `PreEntity`: `Object?` { get }
• `ReadInput`: `ReadInput?` { get; set }
• `ReadOutput`: `ReadOutput?` { get; set }
• `TableName`: `String` { get }
• `TopParent`: `ServiceContext` { get }
**Methods:**
• `GetEntity()`: `T`
• `GetId()`: `Object`
• `GetPreEntity()`: `T`
• `ValueChanged(String fieldName)`: `Boolean`

### Class: StartupContext
**Constructors:**
• `StartupContext(IServiceProvider serviceProvider, IDataService dataService)`
**Properties:**
• `DataService`: `IDataService` { get }
• `ServiceProvider`: `IServiceProvider` { get }

## Xams.Core.Dtos

### Class: ApiResponse
**Constructors:**
• `ApiResponse()`
**Properties:**
• `data`: `Object` { get; set }
• `friendlyMessage`: `String` { get; set }
• `logMessage`: `String` { get; set }
• `succeeded`: `Boolean` { get; set }

### Class: FileData
**Constructors:**
• `FileData()`
**Properties:**
• `ContentType`: `String` { get; set }
• `FileName`: `String` { get; set }
• `Stream`: `Stream` { get; set }

### Class: Response<T>
**Implements:** `IResponse`1`
**Constructors:**
• `Response`1()`
**Properties:**
• `Data`: `T` { get; set }
• `FriendlyMessage`: `String` { get; set }
• `LogMessage`: `String` { get; set }
• `ResponseType`: `ResponseType` { get; set }
• `Succeeded`: `Boolean` { get; set }

### Enum: ResponseType
**Inherits:** `Enum`
**Values:** 
`Json`, `File`

## Xams.Core.Dtos.Data

### Class: ActionInput
**Constructors:**
• `ActionInput()`
**Properties:**
• `name`: `String` { get; set }
• `parameters`: `Dictionary<String, JsonElement>?` { get; set }

### Class: BatchInput
**Inherits:** `Input`
**Constructors:**
• `BatchInput()`
**Properties:**
• `entities`: `Input[]?` { get; set }

### Class: BulkInput
**Constructors:**
• `BulkInput()`
**Properties:**
• `Creates`: `Input[]?` { get; set }
• `Deletes`: `Input[]?` { get; set }
• `Updates`: `Input[]?` { get; set }
• `Upserts`: `Input[]?` { get; set }

### Class: Exclude
**Constructors:**
• `Exclude()`
**Properties:**
• `fromField`: `String` { get; set }
• `query`: `ReadInput` { get; set }

### Class: FileInput
**Inherits:** `ActionInput`
**Constructors:**
• `FileInput()`

### Class: Filter
**Constructors:**
• `Filter()`
**Properties:**
• `field`: `String` { get; set }
• `filters`: `Filter[]?` { get; set }
• `logicalOperator`: `String` { get; set }
• `operator`: `String` { get; set }
• `value`: `String` { get; set }

### Class: Input
**Constructors:**
• `Input()`
**Properties:**
• `fields`: `Dictionary<String, Object>?` { get; set }
• `id`: `Object` { get; set }
• `parameters`: `Dictionary<String, JsonElement>?` { get; set }
• `tableName`: `String` { get; set }

### Class: Join
**Constructors:**
• `Join()`
**Properties:**
• `alias`: `String?` { get; set }
• `fields`: `String[]` { get; set }
• `filters`: `Filter[]?` { get; set }
• `fromField`: `String` { get; set }
• `fromTable`: `String` { get; set }
• `toField`: `String` { get; set }
• `toTable`: `String` { get; set }
• `type`: `String?` { get; set }

### Class: MetadataField
**Constructors:**
• `MetadataField()`
**Properties:**
• `characterLimit`: `Int32?` { get; set }
• `dateFormat`: `String` { get; set }
• `displayName`: `String` { get; set }
• `isNullable`: `Boolean` { get; set }
• `isReadOnly`: `Boolean` { get; set }
• `isRecommended`: `Boolean` { get; set }
• `isRequired`: `Boolean` { get; set }
• `lookupName`: `String` { get; set }
• `lookupTable`: `String` { get; set }
• `lookupTableDescriptionField`: `String` { get; set }
• `lookupTableHasActiveField`: `Boolean` { get; set }
• `lookupTableNameField`: `String` { get; set }
• `lookupTablePrimaryKeyField`: `String` { get; set }
• `name`: `String` { get; set }
• `numberRange`: `String` { get; set }
• `option`: `String` { get; set }
• `order`: `Int32` { get; set }
• `type`: `String` { get; set }

### Class: MetadataInput
**Constructors:**
• `MetadataInput()`
**Properties:**
• `method`: `String` { get; set }
• `parameters`: `Dictionary<String, JsonElement>?` { get; set }

### Class: MetadataOutput
**Constructors:**
• `MetadataOutput()`
**Properties:**
• `displayName`: `String` { get; set }
• `fields`: `List<MetadataField>` { get; set }
• `primaryKey`: `String` { get; set }
• `tableName`: `String` { get; set }

### Class: OrderBy
**Constructors:**
• `OrderBy()`
**Properties:**
• `field`: `String` { get; set }
• `order`: `String?` { get; set }

### Class: PermissionsInput
**Constructors:**
• `PermissionsInput()`
**Properties:**
• `method`: `String` { get; set }
• `parameters`: `Dictionary<String, JsonElement>?` { get; set }

### Class: ReadInput
**Constructors:**
• `ReadInput()`
**Properties:**
• `denormalize`: `Boolean?` { get; set }
• `distinct`: `Boolean?` { get; set }
• `except`: `Exclude[]?` { get; set }
• `fields`: `String[]` { get; set }
• `filters`: `Filter[]?` { get; set }
• `id`: `Object?` { get; set }
• `joins`: `Join[]?` { get; set }
• `maxResults`: `Int32?` { get; set }
• `orderBy`: `OrderBy[]?` { get; set }
• `page`: `Int32` { get; set }
• `parameters`: `Dictionary<String, JsonElement>?` { get; set }
• `tableName`: `String` { get; set }
**Methods:**
• `GetId()`: `Object`

### Class: ReadOutput
**Constructors:**
• `ReadOutput()`
**Properties:**
• `currentPage`: `Int32` { get; set }
• `denormalize`: `Boolean?` { get; set }
• `distinct`: `Boolean?` { get; set }
• `maxResults`: `Int32` { get; set }
• `orderBy`: `OrderBy[]?` { get; set }
• `pages`: `Int32` { get; set }
• `parameters`: `Object` { get; set }
• `results`: `List<Object>` { get; set }
• `tableName`: `String` { get; set }
• `totalResults`: `Int32` { get; set }

### Class: TablesOutput
**Constructors:**
• `TablesOutput()`
**Properties:**
• `displayName`: `String` { get; set }
• `tableName`: `String` { get; set }
• `tag`: `String` { get; set }

## Xams.Core.Entities

### Class: Audit
**Constructors:**
• `Audit()`
**Properties:**
• `AuditFields`: `ICollection<AuditField>` { get; set }
• `AuditId`: `Guid` { get; set }
• `IsCreate`: `Boolean` { get; set }
• `IsDelete`: `Boolean` { get; set }
• `IsRead`: `Boolean` { get; set }
• `IsTable`: `Boolean` { get; set }
• `IsUpdate`: `Boolean` { get; set }
• `Name`: `String?` { get; set }

### Class: AuditField
**Constructors:**
• `AuditField()`
**Properties:**
• `Audit`: `Audit` { get; set }
• `AuditFieldId`: `Guid` { get; set }
• `AuditId`: `Guid` { get; set }
• `IsCreate`: `Boolean` { get; set }
• `IsDelete`: `Boolean` { get; set }
• `IsUpdate`: `Boolean` { get; set }
• `Name`: `String` { get; set }

### Class: AuditHistory
**Constructors:**
• `AuditHistory()`
**Properties:**
• `AuditHistoryId`: `Guid` { get; set }
• `CreatedDate`: `DateTime` { get; set }
• `EntityId`: `String` { get; set }
• `Name`: `String` { get; set }
• `Operation`: `String` { get; set }
• `Query`: `String` { get; set }
• `Results`: `String` { get; set }
• `TableName`: `String` { get; set }
• `User`: `User` { get; set }
• `UserId`: `Guid?` { get; set }

### Class: AuditHistoryDetail
**Constructors:**
• `AuditHistoryDetail()`
**Properties:**
• `AuditHistory`: `AuditHistory` { get; set }
• `AuditHistoryDetailId`: `Guid` { get; set }
• `AuditHistoryId`: `Guid` { get; set }
• `FieldName`: `String` { get; set }
• `FieldType`: `String` { get; set }
• `NewValue`: `String` { get; set }
• `NewValueId`: `Guid?` { get; set }
• `OldValue`: `String` { get; set }
• `OldValueId`: `Guid?` { get; set }
• `TableName`: `String` { get; set }

### Class: Job
**Constructors:**
• `Job()`
**Properties:**
• `IsActive`: `Boolean` { get; set }
• `JobHistories`: `ICollection<JobHistory>` { get; set }
• `JobId`: `Guid` { get; set }
• `LastExecution`: `DateTime` { get; set }
• `Name`: `String` { get; set }
• `Queue`: `String` { get; set }
• `Tag`: `String` { get; set }

### Class: JobHistory
**Constructors:**
• `JobHistory()`
**Properties:**
• `CompletedDate`: `DateTime?` { get; set }
• `CreatedDate`: `DateTime` { get; set }
• `Job`: `Job?` { get; set }
• `JobHistoryId`: `Guid` { get; set }
• `JobId`: `Guid` { get; set }
• `Message`: `String` { get; set }
• `Name`: `String` { get; set }
• `Ping`: `DateTime` { get; set }
• `ServerName`: `String` { get; set }
• `Status`: `String` { get; set }

### Class: Log
**Constructors:**
• `Log()`
**Properties:**
• `ApplicationName`: `String` { get; set }
• `ClientIp`: `String` { get; set }
• `CorrelationId`: `Guid?` { get; set }
• `ElapsedMs`: `Double?` { get; set }
• `Environment`: `String` { get; set }
• `Exception`: `String` { get; set }
• `ExceptionMessage`: `String` { get; set }
• `ExceptionType`: `String` { get; set }
• `JobHistory`: `JobHistory` { get; set }
• `JobHistoryId`: `Guid?` { get; set }
• `Level`: `String` { get; set }
• `LogId`: `Guid` { get; set }
• `MachineName`: `String` { get; set }
• `Message`: `String` { get; set }
• `MessageTemplate`: `String` { get; set }
• `Properties`: `String` { get; set }
• `RequestId`: `String` { get; set }
• `RequestMethod`: `String` { get; set }
• `RequestPath`: `String` { get; set }
• `SourceContext`: `String` { get; set }
• `StatusCode`: `Int32?` { get; set }
• `ThreadId`: `Int32?` { get; set }
• `Timestamp`: `DateTime` { get; set }
• `User`: `User` { get; set }
• `UserAgent`: `String` { get; set }
• `UserId`: `Guid?` { get; set }
• `UserName`: `String` { get; set }
• `Version`: `String` { get; set }

### Class: Option
**Constructors:**
• `Option()`
**Properties:**
• `Label`: `String` { get; set }
• `Name`: `String` { get; set }
• `OptionId`: `Guid` { get; set }
• `Order`: `Int32?` { get; set }
• `Tag`: `String` { get; set }
• `Value`: `String` { get; set }

### Class: Permission
**Constructors:**
• `Permission()`
**Properties:**
• `Name`: `String` { get; set }
• `PermissionId`: `Guid` { get; set }
• `Tag`: `String` { get; set }

### Class: Role
**Constructors:**
• `Role()`
**Properties:**
• `Discriminator`: `Int32` { get; set }
• `Name`: `String` { get; set }
• `RoleId`: `Guid` { get; set }

### Class: RolePermission<TRole>
**Constructors:**
• `RolePermission`1()`
**Properties:**
• `Permission`: `Permission` { get; set }
• `PermissionId`: `Guid` { get; set }
• `Role`: `TRole` { get; set }
• `RoleId`: `Guid` { get; set }
• `RolePermissionId`: `Guid` { get; set }

### Class: Server
**Constructors:**
• `Server()`
**Properties:**
• `LastPing`: `DateTime` { get; set }
• `Name`: `String` { get; set }
• `ServerId`: `Guid` { get; set }

### Class: Setting
**Constructors:**
• `Setting()`
**Properties:**
• `Discriminator`: `Int32` { get; set }
• `Name`: `String` { get; set }
• `SettingId`: `Guid` { get; set }
• `Value`: `String` { get; set }

### Class: System
**Constructors:**
• `System()`
**Properties:**
• `DateTime`: `DateTime?` { get; set }
• `Name`: `String` { get; set }
• `SystemId`: `Guid` { get; set }
• `Value`: `String` { get; set }

### Class: Team
**Constructors:**
• `Team()`
**Properties:**
• `Discriminator`: `Int32` { get; set }
• `Name`: `String` { get; set }
• `TeamId`: `Guid` { get; set }

### Class: TeamRole<TTeam, TRole>
**Constructors:**
• `TeamRole`2()`
**Properties:**
• `Role`: `TRole` { get; set }
• `RoleId`: `Guid` { get; set }
• `Team`: `TTeam` { get; set }
• `TeamId`: `Guid` { get; set }
• `TeamRoleId`: `Guid` { get; set }

### Class: TeamUser<TTeam, TUser>
**Constructors:**
• `TeamUser`2()`
**Properties:**
• `Team`: `TTeam` { get; set }
• `TeamId`: `Guid` { get; set }
• `TeamUserId`: `Guid` { get; set }
• `User`: `TUser` { get; set }
• `UserId`: `Guid` { get; set }

### Class: User
**Constructors:**
• `User()`
**Properties:**
• `CreatedDate`: `DateTime` { get; set }
• `Discriminator`: `Int32` { get; set }
• `Name`: `String` { get; set }
• `UserId`: `Guid` { get; set }

### Class: UserRole<TUser, TRole>
**Constructors:**
• `UserRole`2()`
**Properties:**
• `Role`: `TRole` { get; set }
• `RoleId`: `Guid` { get; set }
• `User`: `TUser` { get; set }
• `UserId`: `Guid` { get; set }
• `UserRoleId`: `Guid` { get; set }

## Xams.Core.Extensions

### Static Class: ExtensionMethods
**Methods:**
• `GetTables(ReadInput? readInput)`: `String[]`

## Xams.Core.Hubs

### Class: LoggingHub
**Implements:** `IServiceHub`
**Constructors:**
• `LoggingHub()`
**Methods:**
• `OnConnected(HubContext context)`: `Task<Response`1>`
• `OnDisconnected(HubContext context)`: `Task<Response`1>`
• `OnReceive(HubContext context)`: `Task<Response`1>`
• `Send(HubSendContext context)`: `Task<Response`1>`

### Class: ReceiveMessage
**Constructors:**
• `ReceiveMessage()`
**Properties:**
• `EndDate`: `DateTime?` { get; set }
• `JobHistoryId`: `String` { get; set }
• `Levels`: `List<String>?` { get; set }
• `Message`: `String` { get; set }
• `Search`: `String` { get; set }
• `StartDate`: `DateTime?` { get; set }
• `Type`: `String` { get; set }
• `UserId`: `String` { get; set }

### Class: SendMessage
**Constructors:**
• `SendMessage()`
**Properties:**
• `Exception`: `String?` { get; set }
• `JobHistoryId`: `Guid?` { get; set }
• `Level`: `String` { get; set }
• `LogId`: `Guid` { get; set }
• `Message`: `String` { get; set }
• `Properties`: `Dictionary<String, Object>` { get; set }
• `SourceContext`: `String?` { get; set }
• `Timestamp`: `DateTime` { get; set }
• `UserId`: `Guid?` { get; set }

## Xams.Core.Interfaces

### Interface: IBulkService
**Methods:**
• `Execute(BulkServiceContext context)`: `Task<Response`1>`

### Interface: IDataService
**Methods:**
• `Bulk(Guid userId, BulkInput input)`: `Task<Response`1>`
• `Create(Guid userId, BatchInput createInput)`: `Task<Response`1>`
• `Create(Guid userId, T entity, Object parameters = null, PipelineContext parent = null)`: `Task<Response`1>`
• `Delete(Guid userId, BatchInput input)`: `Task<Response`1>`
• `Delete(Guid userId, T entity, Object parameters = null, PipelineContext parent = null)`: `Task<Response`1>`
• `GetDataRepository()`: `DataRepository`
• `GetDbContext()`: `T` where T : IXamsDbContext
• `GetExecutionId()`: `Guid`
• `GetExecutionUserId()`: `Guid`
• `GetMetadataRepository()`: `MetadataRepository`
• `GetSecurityRepository()`: `SecurityRepository`
• `Metadata(MetadataInput metadataInput, Guid userId)`: `Task<Response`1>`
• `Permissions(PermissionsInput permissionsInput, Guid userId)`: `Task<Response`1>`
• `Read(Guid userId, ReadInput input, PipelineContext? parent = null)`: `Task<Response`1>`
• `Update(Guid userId, BatchInput input)`: `Task<Response`1>`
• `Update(Guid userId, T entity, Object parameters = null, PipelineContext parent = null)`: `Task<Response`1>`
• `Upsert(Guid userId, BatchInput input)`: `Task<Response`1>`
• `Upsert(Guid userId, T entity, Object parameters = null, PipelineContext parent = null)`: `Task<Response`1>`
• `WhoAmI(Guid userId)`: `Task<Response`1>`

### Interface: IPipelineStage
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`
• `SetNext(IPipelineStage nextItem)`: `void`

### Interface: IResponse<T>
**Properties:**
• `Data`: `T` { get; set }
• `FriendlyMessage`: `String` { get; set }
• `LogMessage`: `String` { get; set }
• `Succeeded`: `Boolean` { get; set }

### Interface: IServiceAction
Execute a service action.

**Methods:**
• `Execute(ActionServiceContext context)`: `Task<Response`1>`

### Interface: IServiceHub
**Methods:**
• `OnConnected(HubContext context)`: `Task<Response`1>`
  Client connected to the hub and has permission to use it.
            Can use this method to add the user to a group, initialize state, etc.
            Also called if a client is connected, and they've recently been given permission to use the hub.
  - `context`: 

• `OnDisconnected(HubContext context)`: `Task<Response`1>`
  Client disconnected from the hub.
            Can use this method to clean up state, remove the user from a group, etc.
            Also called if the client recently lost permission to use the hub,
  - `context`: 

• `OnReceive(HubContext context)`: `Task<Response`1>`
  On Receive method is called when a message is received from the client.
  - `context`: 

• `Send(HubSendContext context)`: `Task<Response`1>`
  Send method is called to send a message to the client.
            This will only be called from server-side code
  - `context`: 


### Interface: IServiceJob
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

### Interface: IServiceLogic
Execute a service logic on create, read, update, or delete.

**Methods:**
• `Execute(ServiceContext context)`: `Task<Response`1>`

### Interface: IServicePermission
Service Permissions return a list of permissions that should exist in the system.
            If the permissions do not exist, the system will create them, and the permission
            existed prior but no longer does, the system will remove it.

**Methods:**
• `Execute(PermissionContext permissionContext)`: `Task<Response`1>`

### Interface: IServiceStartup
Executes logic on startup.

**Methods:**
• `Execute(StartupContext startupContext)`: `Task<Response`1>`

## Xams.Core.Jobs

### Class: Job
**Constructors:**
• `Job(Object jobObject, Dictionary<String, JsonElement> parameters, Guid? jobHistoryId = null)`
**Properties:**
• `Entity`: `Object` { get }
• `IsActive`: `Boolean` { get; set }
• `JobId`: `Guid` { get; set }
• `Name`: `String` { get; set }
• `Queue`: `String?` { get; set }
• `ServiceJobInfo`: `ServiceJobInfo` { get; set }
• `Tag`: `String?` { get; set }
**Methods:**
• `ConvertDayOfWeek(DayOfWeek dayOfWeek)`: `DaysOfWeek`

### Class: JobHistoryRetentionJob
**Implements:** `IServiceJob`
**Constructors:**
• `JobHistoryRetentionJob()`
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

### Class: JobOptions
**Constructors:**
• `JobOptions()`
**Properties:**
• `JobHistoryId`: `Guid?` { get; set } - Force set the JobHistoryId. Useful for monitoring a job status.
• `JobName`: `String` { get; set }
• `JobServer`: `String` { get; set } - Optionally specify Job Server
• `Parameters`: `Object` { get; set } - Any class or anonymous type

### Class: JobPing
**Constructors:**
• `JobPing(IXamsDbContext dbContext, Object jobHistory)`
**Methods:**
• `End()`: `Task`
• `Start()`: `void`

### Class: JobQueue
**Constructors:**
• `JobQueue(IServiceProvider serviceProvider)`
**Properties:**
• `Name`: `String?` { get; set }
**Methods:**
• `Execute(List<Job> jobs)`: `Task`

### Class: JobService
**Constructors:**
• `JobService(IServiceProvider serviceProvider)`
**Properties:**
• `DefaultServerName`: `String?` { get; set }
• `PingInterval`: `Int32` { get; set }
• `Singleton`: `JobService?` { get; set }

### Class: JobStartupService
**Implements:** `IServiceStartup`
**Constructors:**
• `JobStartupService()`
**Methods:**
• `CreateJobs()`: `Task`
• `Execute(StartupContext startupContext)`: `Task<Response`1>`

### Class: LogHistoryRetentionJob
**Implements:** `IServiceJob`
**Constructors:**
• `LogHistoryRetentionJob()`
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

### Class: LogStartupService
**Implements:** `IServiceStartup`
**Constructors:**
• `LogStartupService()`
**Methods:**
• `Execute(StartupContext startupContext)`: `Task<Response`1>`

## Xams.Core.Pipeline

### Class: PipelineBuilder
**Constructors:**
• `PipelineBuilder()`
**Methods:**
• `Add(IPipelineStage pipelineStage)`: `PipelineBuilder`
• `Execute(PipelineContext pipelineContext)`: `Task<Response`1>`

### Class: PipelineContext
**Constructors:**
• `PipelineContext()`
**Properties:**
• `DataOperation`: `DataOperation` { get; set }
• `DataRepository`: `DataRepository` { get; set }
• `DataService`: `IDataService` { get; set }
• `Depth`: `Int32` { get; set }
• `Entities`: `List<Object>?` { get; set }
• `Entity`: `Object?` { get; set }
• `Fields`: `Dictionary<String, Object>?` { get; set }
• `InputParameters`: `Dictionary<String, JsonElement>` { get; set }
• `IsProxy`: `Boolean` { get; set }
• `MetadataRepository`: `MetadataRepository` { get; set }
• `OutputParameters`: `Dictionary<String, JsonElement>` { get; set }
• `Parent`: `PipelineContext?` { get; set }
• `Permissions`: `String[]` { get; set }
• `PreEntity`: `Object?` { get; set }
• `ReadInput`: `ReadInput?` { get; set }
• `ReadOutput`: `ReadOutput?` { get; set }
• `SecurityRepository`: `SecurityRepository` { get; set }
• `ServiceContext`: `ServiceContext` { get; set }
• `SystemParameters`: `SystemParameters` { get; set }
• `TableName`: `String` { get; set }
• `UserId`: `Guid` { get; set }
**Methods:**
• `CreateServiceContext()`: `ServiceContext`

### Static Class: Pipelines
**Fields:**
• `Create`: `PipelineBuilder`
• `CreateProxy`: `PipelineBuilder`
• `Delete`: `PipelineBuilder`
• `DeleteProxy`: `PipelineBuilder`
• `Read`: `PipelineBuilder`
• `ReadProxy`: `PipelineBuilder`
• `SecurityPipeline`: `PipelineBuilder`
• `Update`: `PipelineBuilder`
• `UpdateProxy`: `PipelineBuilder`

### Struct: SystemParameters
**Properties:**
• `NoPostOrderTraversalDelete`: `Boolean` { get; set }
• `PreventSave`: `Boolean` { get; set }
• `ReturnEmpty`: `Boolean` { get; set }
• `ReturnEntity`: `Boolean` { get; set }

## Xams.Core.Pipeline.Stages

### Class: PipeAddEntityToEntities
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeAddEntityToEntities()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeEntityCreate
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeEntityCreate()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeEntityDelete
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeEntityDelete()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeEntityRead
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeEntityRead()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeEntityUpdate
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeEntityUpdate()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeExecuteServiceLogic
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeExecuteServiceLogic(LogicStage logicStage)`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeGetPreEntity
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeGetPreEntity()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipelineDependency
**Constructors:**
• `PipelineDependency()`
**Properties:**
• `Dependency`: `Dependency` { get; set }
• `PipelineContext`: `PipelineContext` { get; set }

### Class: PipePatchEntity
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipePatchEntity()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipePermissionRules
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipePermissionRules()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipePermissions
Determines if the user can perform the Create\Read\Update\Delete operation

**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipePermissions()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipePreValidation
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipePreValidation()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeProtectSystemRecords
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeProtectSystemRecords()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeResultEmpty
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeResultEmpty()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeResultEntity
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeResultEntity()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeResultReadOutput
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeResultReadOutput()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeSetDefaultFields
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeSetDefaultFields()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeUIServices
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeUIServices()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

### Class: PipeValidateNonNullableProperties
**Inherits:** `BasePipelineStage`
**Implements:** `IPipelineStage`
**Constructors:**
• `PipeValidateNonNullableProperties()`
**Methods:**
• `Execute(PipelineContext context)`: `Task<Response`1>`

## Xams.Core.Pipeline.Stages.Shared

### Static Class: AppendOutputParameters
**Methods:**
• `Set(PipelineContext context, List<Object> data)`: `List<Object>`

### Static Class: AppendUIInfo
**Methods:**
• `Set(PipelineContext context, ReadOutput readOutput)`: `Task<List`1>`

### Static Class: PipelineUtil
**Methods:**
• `SetExistingEntity(PipelineContext context, Guid id)`: `Task<Response`1>`

## Xams.Core.Repositories

### Class: DataRepository
**Implements:** `IDisposable`
**Constructors:**
• `DataRepository(Type dataContextType, IDataService dataService)`
**Methods:**
• `BeginTransaction()`: `Task`
• `CommitTransaction()`: `Task`
• `Create(T entity, Boolean preventSave)`: `Task<Response`1>`
• `CreateNewDbContext()`: `IXamsDbContext`
• `CreateNewDbContext()`: `T` where T : IXamsDbContext
• `Delete(T entity, Boolean preventSave)`: `Task<Response`1>`
• `DenormalizeJoins(String fromTable, List<Object> results, Dictionary<String, List`1> joinedRecords, ReadInput readInput)`: `List<Object>`
• `Dispose()`: `void`
• `Find(String tableName, Object[] ids, Boolean newDataContext, String[]? fields = null, Boolean updateFieldPrefixes = false)`: `Task<Response`1>`
• `Find(String tableName, Object id, Boolean newDataContext)`: `Task<Response`1>`
• `GetDbContext()`: `TDbContext` where TDbContext : IXamsDbContext
• `Read(Guid userId, ReadInput readInput, ReadOptions readOptions)`: `Task<Response`1>`
  This will return a list of ExpandoObjects
  - `userId`: 
  - `readInput`: 
  - `readOptions`: 

• `RollbackTransaction()`: `Task`
• `SaveChangesAsync()`: `Task`
• `Update(T entity, Boolean preventSave)`: `Task<Response`1>`

### Class: MetadataRepository
**Constructors:**
• `MetadataRepository(Type dataContext)`
**Methods:**
• `GetDataContext()`: `IXamsDbContext`
• `GetTableMetadata(MetadataInput metadataInput)`: `Response<Object>`
• `Metadata(MetadataInput metadataInput, Guid userId)`: `Task<Response`1>`
• `Tables(MetadataInput metadataInput, Guid userId)`: `Task<Response`1>`

### Class: ReadOptions
**Constructors:**
• `ReadOptions()`
**Properties:**
• `BypassSecurity`: `Boolean` { get; set }
• `NewDataContext`: `Boolean` { get; set }
• `Permissions`: `String[]` { get; set }

### Class: SecurityRepository
**Constructors:**
• `SecurityRepository()`
**Methods:**
• `Get(PermissionsInput permissionsInput, Guid userId)`: `Task<Response`1>`
• `UserPermissions(PermissionsInput permissionsInput, Guid userId)`: `Task<Response`1>`
• `UsersExistInSameTeam(Guid user1Id, Guid user2Id)`: `Boolean`
  Returns true if 2 users are reachable through the same team.
  - `user1Id`: 
  - `user2Id`: 


## Xams.Core.Services

### Class: DataService<TDbContext>
**Inherits:** `DataService`2`
**Implements:** `IDataService`, `IDisposable`
### Class: DataService<TDbContext, TUser>
**Inherits:** `DataService`3`
**Implements:** `IDataService`, `IDisposable`
### Class: DataService<TDbContext, TUser, TTeam>
**Inherits:** `DataService`4`
**Implements:** `IDataService`, `IDisposable`
### Class: DataService<TDbContext, TUser, TTeam, TRole>
**Inherits:** `DataService`5`
**Implements:** `IDataService`, `IDisposable`
### Class: DataService<TDbContext, TUser, TTeam, TRole, TSetting>
**Implements:** `IDataService`, `IDisposable`
### Class: OperationInfo
**Constructors:**
• `OperationInfo()`
**Properties:**
• `DataOperation`: `DataOperation` { get; set }
• `Entity`: `Object` { get; set }
• `Input`: `Input` { get; set }
• `TableName`: `String` { get; set }

### Class: QueryFactory
**Constructors:**
• `QueryFactory(IXamsDbContext dbContext, QueryOptions queryOptions, ReadInput readInput)`
**Methods:**
• `Base(ReadInput readInput, String fieldPrefix = "root")`: `Query`
• `Create()`: `Query`
• `ValidateFilters(ReadInput readInput, Filter[]? filters)`: `void`

### Class: QueryOptions
**Constructors:**
• `QueryOptions()`
**Properties:**
• `Permissions`: `String[]` { get; set }
• `UserId`: `Guid` { get; set }

### Class: TableOperationGroup
**Constructors:**
• `TableOperationGroup()`
**Properties:**
• `PipelineContexts`: `List<PipelineContext>` { get; set }
• `TableName`: `String` { get; set }

## Xams.Core.Services.Auditing

### Class: AuditBulkService
**Implements:** `IBulkService`
**Constructors:**
• `AuditBulkService()`
**Methods:**
• `Execute(BulkServiceContext context)`: `Task<Response`1>`

### Class: AuditCacheRefreshJob
**Implements:** `IServiceJob`
**Constructors:**
• `AuditCacheRefreshJob()`
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

### Class: AuditFieldService
**Implements:** `IServiceLogic`
**Constructors:**
• `AuditFieldService()`
**Methods:**
• `Execute(ServiceContext context)`: `Task<Response`1>`

### Class: AuditHistoryDetailService
**Implements:** `IServiceLogic`
**Constructors:**
• `AuditHistoryDetailService()`
**Methods:**
• `Execute(ServiceContext context)`: `Task<Response`1>`

### Class: AuditHistoryRecordOptions
**Constructors:**
• `AuditHistoryRecordOptions()`
**Properties:**
• `DataOperation`: `DataOperation` { get; set }
• `Entity`: `Object` { get; set }
• `ExecutingUserId`: `Guid` { get; set }
• `NewDb`: `IXamsDbContext` { get; set }
• `PreEntity`: `Object` { get; set }
• `ReadInput`: `ReadInput` { get; set }
• `ReadOutput`: `ReadOutput` { get; set }
• `TableName`: `String` { get; set }
• `TransactionDb`: `IXamsDbContext` { get; set }

### Class: AuditHistoryRetentionJob
**Implements:** `IServiceJob`
**Constructors:**
• `AuditHistoryRetentionJob()`
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

### Class: AuditNameService
If the Name property is changed, this service will update all Audit History records to reflect the new name.

**Implements:** `IServiceLogic`
**Constructors:**
• `AuditNameService()`
**Methods:**
• `Execute(ServiceContext context)`: `Task<Response`1>`

### Class: AuditOperation
**Constructors:**
• `AuditOperation()`
**Properties:**
• `DataOperation`: `DataOperation` { get; set }
• `Entity`: `Object` { get; set }
• `PreEntity`: `Object?` { get; set }

### Class: AuditStartupService
**Implements:** `IServiceStartup`
**Constructors:**
• `AuditStartupService()`
**Methods:**
• `CacheAuditRecords(IXamsDbContext db)`: `Task<Response`1>`
• `Execute(StartupContext startupContext)`: `Task<Response`1>`
• `GetAuditSettings(StartupContext context)`: `Task`
**Fields:**
• `AuditRetentionSetting`: `String`

### Class: Lookup
**Constructors:**
• `Lookup()`
**Properties:**
• `AuditId`: `Guid` { get; set }
• `Entity`: `Object` { get; set }
• `FieldName`: `String` { get; set }
• `TableName`: `String` { get; set }

### Class: NewAudit
**Constructors:**
• `NewAudit()`
**Properties:**
• `Entity`: `Object` { get; set }
• `TableName`: `String` { get; set }

### Static Class: AuditLogic
**Methods:**
• `AddAuditHistoryRecord(AuditHistoryRecordOptions options)`: `Object`
• `AddCreateUpdateDeleteDetails(AuditHistoryRecordOptions options, Object auditHistory)`: `Task`
• `Audit(IXamsDbContext db, CancellationToken cancellationToken = null)`: `Task`
• `FormatValue(Object value)`: `String`

## Xams.Core.Services.Jobs

### Class: ImportDataJob
This job is triggered upon config data import

**Implements:** `IServiceJob`
**Constructors:**
• `ImportDataJob()`
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

## Xams.Core.Services.Logging

### Class: LogItem
**Constructors:**
• `LogItem()`
**Properties:**
• `LogId`: `Guid` { get; set }

## Xams.Core.Services.Logic

### Class: SettingService
**Implements:** `IServiceLogic`
**Constructors:**
• `SettingService()`
**Methods:**
• `Execute(ServiceContext context)`: `Task<Response`1>`

### Class: SystemService
**Implements:** `IServiceLogic`
**Constructors:**
• `SystemService()`
**Methods:**
• `Execute(ServiceContext context)`: `Task<Response`1>`

## Xams.Core.Services.Permission

### Class: CreatePermissions
**Implements:** `IServicePermission`
**Constructors:**
• `CreatePermissions()`
**Methods:**
• `Execute(PermissionContext permissionContext)`: `Task<Response`1>`

### Class: PermissionCacheJob
**Implements:** `IServiceJob`
### Class: PermissionCacheService
Whenever the permissions are updated, this service will invalidate the cache so all running instances will refresh their cache.

**Implements:** `IBulkService`
**Constructors:**
• `PermissionCacheService()`
**Methods:**
• `Execute(BulkServiceContext context)`: `Task<Response`1>`

## Xams.Core.Services.Servers

### Class: ServerJob
Ping the database every 5 seconds

**Implements:** `IServiceJob`
**Constructors:**
• `ServerJob()`
**Methods:**
• `Execute(JobServiceContext context)`: `Task<Response`1>`

## Xams.Core.Startup

### Class: Item
**Constructors:**
• `Item(String name, String value)`
**Properties:**
• `Name`: `String` { get; set }
• `Value`: `String` { get; set }

### Class: SystemEntities
**Constructors:**
• `SystemEntities()`

### Class: SystemRecords
**Constructors:**
• `SystemRecords(IDataService dataService)`
**Methods:**
• `CreateRolePermissions()`: `Task`
• `CreateSettingAndSystemRecords()`: `Task`
• `CreateSystemPermissions()`: `Task`
• `CreateSystemRoles()`: `Task`
• `CreateSystemTeams()`: `Task`
• `CreateSystemUser()`: `Task`
• `CreateTeamRoles()`: `Task`
• `CreateTeamUsers()`: `Task`
• `CreateUserRoles()`: `Task`
**Fields:**
• `SystemAdministratorRoleId`: `Guid`
• `SystemAdministratorsTeamRoleId`: `Guid`
• `SystemAdministratorTeamId`: `Guid`
• `SystemTeamUserId`: `Guid`
• `SystemUserId`: `Guid`
• `SystemUserRoleId`: `Guid`

## Xams.Core.Utils

### Class: Dependency
**Constructors:**
• `Dependency()`
**Properties:**
• `Dependencies`: `List<Dependency>?` { get; set }
• `Depth`: `Int32` { get; set }
• `IsCascadeDelete`: `Boolean` { get; set }
• `IsNullable`: `Boolean` { get; set }
• `Parent`: `Dependency?` { get; set }
• `PropertyName`: `String` { get; set }
• `Type`: `Type` { get; set }

### Class: DependencyFinder
**Constructors:**
• `DependencyFinder()`
**Methods:**
• `GetDependencies(Type targetType, IXamsDbContext dbContext, Int32 depth = 0, Dependency? parent = null)`: `List<Dependency>`
• `GetMaxDepth(List<Dependency> dependencies)`: `Int32`
• `GetPostOrderTraversal(PostOrderTraversalSettings settings, PostOrderTraversalContext? context = null)`: `Task<ConcurrentDictionary`2>`
• `LogDependencyTree(List<Dependency> dependencies, Int32 depth = 0)`: `void`

### Class: DynamicLinq
**Constructors:**
• `DynamicLinq(IXamsDbContext db, Type entity)`
**Properties:**
• `DbSet`: `Object` { get; set }
• `PrimaryKey`: `String` { get; set }
• `Query`: `IQueryable` { get; set }
• `TargetType`: `Type` { get; set }
**Methods:**
• `BatchRequest(IXamsDbContext db, Type entity, List<Object> ids, Int32 batchSize = 500)`: `Task<List`1>`
• `BatchRequestThreaded(Func<IXamsDbContext> dbFactory, Type entity, List<Object> ids, Int32 batchSize = 500)`: `Task<ConcurrentDictionary`2>`
• `Find(IXamsDbContext db, Type entity, Object id)`: `Task<Object>`
• `FindAll(IXamsDbContext db, Type entity)`: `Task<List`1>`

### Class: EntityInfo
**Constructors:**
• `EntityInfo()`
**Properties:**
• `Entity`: `Object` { get; set }
• `Id`: `Object` { get; set }

### Class: GuidUtil
**Constructors:**
• `GuidUtil()`
**Methods:**
• `FromString(String input)`: `Guid`

### Class: MapEntityResult
**Constructors:**
• `MapEntityResult()`
**Properties:**
• `Entity`: `Object` { get; set }
• `Error`: `Boolean` { get; set }
• `Message`: `String` { get; set }

### Class: PostOrderTraversalContext
**Constructors:**
• `PostOrderTraversalContext()`
**Properties:**
• `Dependencies`: `List<Dependency>` { get; set }
• `Depth`: `Int32` { get; set }
• `Id`: `Object` { get; set }
• `RecordInfoDict`: `ConcurrentDictionary<Object, RecordInfo>` { get; set }

### Class: PostOrderTraversalSettings
**Constructors:**
• `PostOrderTraversalSettings()`
**Properties:**
• `DbContextFactory`: `Func<IXamsDbContext>` { get; set }
• `Dependencies`: `List<Dependency>` { get; set }
• `Id`: `Object` { get; set }
• `ReturnEntity`: `Boolean` { get; set }

### Class: Query
**Constructors:**
• `Query(IXamsDbContext dbContext, String[] fields, String rootAlias = "root")`
**Properties:**
• `RootAlias`: `String` { get }
• `TableName`: `String` { get; set }
**Methods:**
• `Contains(String contains, Object[] args)`: `Query`
• `Distinct()`: `Query`
• `Except(String fromField, Query excludeQuery)`: `Query`
• `From(String tableName)`: `Query`
• `Join(String from, String to, String alias, String[] fields)`: `Query`
• `Join(String from, String to, Query joinQuery)`: `Query`
• `LeftJoin(String from, String to, String alias, String[] fields)`: `Query`
• `LeftJoin(String from, String to, Query joinQuery)`: `Query`
• `OrderBy(String field, String order)`: `Query`
• `ToDynamicListAsync()`: `Task<List`1>`
• `Top(Int32 value)`: `Query`
• `ToQueryable()`: `IQueryable`
• `ToQueryableRaw()`: `IQueryable`
• `Union(Query query)`: `Query`
• `Where(String where, Object[]? args)`: `Query`

### Class: RecordInfo
**Constructors:**
• `RecordInfo()`
**Properties:**
• `Count`: `Int32` { get; set }
• `Dependencies`: `List<Dependency>` { get; set }
• `Depth`: `Int32` { get; set }
• `Entity`: `Object` { get; set }

### Class: SqliteUtil
**Constructors:**
• `SqliteUtil()`
**Methods:**
• `Repair(IXamsDbContext dbContext)`: `Task`
  Upon creating a non-nullable field, Sqlite defaults the value to null
            This creates an issue with the entity framework upon querying because it's expecting a non-nullable value
            This updates the database with the default values


### Class: SqlTranslator
**Constructors:**
• `SqlTranslator()`
**Methods:**
• `Translate(String postgresqlQuery, DbProvider targetDb)`: `String`

### Class: UiSetFieldFromLookupInfo
**Constructors:**
• `UiSetFieldFromLookupInfo()`
**Properties:**
• `Id`: `Guid` { get; set }
• `LookupType`: `Type` { get; set }
• `Property`: `PropertyInfo` { get; set }

### Class: XamsDashboardOptions
**Constructors:**
• `XamsDashboardOptions()`
**Properties:**
• `RequireAuthorization`: `Boolean` { get; set }

### Enum: FieldModifications
**Inherits:** `Enum`
**Values:** 
`NoRelatedFields`, `RelatedToNames`, `AllFields`

### Static Class: CopyUtil
**Methods:**
• `DeepCopy(IXamsDbContext dbContext, Type entityType, Guid entityId, String[] excludedEntities)`: `Task<Object>`
  Deep copy an entity
  - `dbContext`: 
  - `entityType`: 
  - `entityId`: 
  - `excludedEntities`: Which entities should not be copied and maintain their original relationship


### Static Class: EmbeddedResourceUtil
**Properties:**
• `Assembly`: `Assembly` { get }
**Methods:**
• `GetNamespace()`: `String`
• `GetResourceNames()`: `String[]`
  Lists all embedded resources in the assembly


### Static Class: EmbeddedRoutingUtil

### Static Class: EntityUtil
**Methods:**
• `ConvertToEntity(Type type, Dictionary<String, Object> fields)`: `MapEntityResult`
• `ConvertToEntityId(Type type, Input input)`: `MapEntityResult`
  Convert the Primary Key to the appropriate type

**Returns:** MapEntityResult containing the entity or error information
  - `type`: The entity type
  - `fields`: The fields dictionary

• `DictionaryToEntity(Type type, Dictionary<String, Object> fields)`: `Object`
• `GetEntity(Input input, DataOperation dataOperation, String& errorMessage)`: `Response<Object>`
• `GetEntityFields(Type targetType, String[]? fields, String fieldPrefix, FieldModifications mods)`: `String`
• `GetLookupNameValue(Object entity)`: `String`
  Finds the value of the Name or UILookupNameAttribute property
  - `entity`: 

• `IsSystemEntity(Type entityType)`: `Boolean`
• `MergeFields(Object? entity, Dictionary<String, Object> fields)`: `MapEntityResult`
• `ValueChanged(Object ent, Object preEntity, String fieldName)`: `Boolean`
**Fields:**
• `SimpleTypes`: `HashSet<Type>`

### Static Class: Queries
**Methods:**
• `GetCreateSetting(IXamsDbContext db, String name, String defaultValue)`: `Task<String>`
• `UpdateSystemRecord(IXamsDbContext db, String name, String value)`: `Task`

### Static Class: QueryUtil
**Methods:**
• `Contains(IQueryable query, Object[] values, String field)`: `IQueryable`
  Adds a "Contains" filter to an IQueryable based on an array of  values

**Returns:** Filtered IQueryable with Where clause applied
  - `query`: The IQueryable to filter
  - `values`: Array of values
  - `field`: Name of the field

• `GetTables(ReadInput? readInput)`: `String[]`

### Static Class: ServiceResult
**Methods:**
• `Error(Object data = null)`: `Response<Object>`
• `Error(String friendlyMessage, Object? data = null)`: `Response<Object>`
• `Error(String friendlyMessage, String logMessage, Object? data = null)`: `Response<Object>`
• `Success(Object data = null)`: `Response<Object>`
• `Success(List<String> data)`: `Response<List`1>`
• `Success(FileData fileData)`: `Response<Object>`

### Static Class: UIInfoUtil
**Methods:**
• `IsUIInfoSet(ReadOutput readOutput)`: `Boolean`
• `SetUIInfo(ReadOutput readOutput, Boolean canDelete, Boolean canUpdate, Boolean canAssign)`: `void`

