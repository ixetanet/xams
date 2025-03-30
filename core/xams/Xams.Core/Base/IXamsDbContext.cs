using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xams.Core.Entities;

namespace Xams.Core.Base;


public interface IXamsDbContext : IDisposable
{
    internal IQueryable<User> UsersBase { get; }
    internal IQueryable<Role> RolesBase { get; }
    internal IQueryable<Team> TeamsBase { get; }
    internal IQueryable<Setting> SettingsBase { get; }
    internal IQueryable<Permission> PermissionsBase { get; }
    internal IQueryable<TeamUser<Team, User>> TeamUsersBase { get; } 
    internal IQueryable<TeamRole<Team, Role>> TeamRolesBase { get; }
    internal IQueryable<RolePermission<Role>> RolePermissionsBase { get; }
    internal IQueryable<UserRole<User, Role>> UserRolesBase { get; } 
    internal IQueryable<Option> OptionsBase { get; }
    internal IQueryable<Xams.Core.Entities.System> SystemsBase { get; }
    internal IQueryable<Server> ServersBase { get; }
    internal IQueryable<Job> JobsBase { get; }
    internal IQueryable<JobHistory> JobHistoriesBase { get; }
    internal IQueryable<Audit> AuditsBase { get; }
    internal IQueryable<AuditField> AuditFieldsBase { get; }
    internal IQueryable<AuditHistory> AuditHistoriesBase { get; }
    internal IQueryable<AuditHistoryDetail> AuditHistoryDetailsBase { get; }

    public bool SaveChangesCalledWithPendingChanges();
    DatabaseFacade Database { get; }
    ChangeTracker ChangeTracker { get; }
    IModel Model { get; }
    DbContextId ContextId { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken);
    int SaveChanges();
    int SaveChanges(bool acceptAllChangesOnSuccess);

    /// <summary>
    /// Returns the current database provider.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    DbProvider GetDbProvider();

    /// <summary>
    /// Takes in raw SQL for Postgres, translates it to the current database provider, and executes it.
    /// </summary>
    /// <param name="postgresSql"></param>
    /// <param name="parameters"></param>
    /// <exception cref="Exception"></exception>
    Task ExecuteRawSql(string postgresSql, Dictionary<string, object?>? parameters = null);

    bool Equals(object? obj);
    int GetHashCode();
    string? ToString();
    void Dispose();
    ValueTask DisposeAsync();
    EntityEntry Entry(object entity);
    EntityEntry Add(object entity);
    ValueTask<EntityEntry> AddAsync(object entity, CancellationToken cancellationToken);
    EntityEntry Attach(object entity);
    EntityEntry Update(object entity);
    EntityEntry Remove(object entity);
    void AddRange(params object[] entities);
    void AddRange(IEnumerable<object> entities);
    Task AddRangeAsync(params object[] entities);
    Task AddRangeAsync(IEnumerable<object> entities, CancellationToken cancellationToken);
    void AttachRange(params object[] entities);
    void AttachRange(IEnumerable<object> entities);
    void UpdateRange(params object[] entities);
    void UpdateRange(IEnumerable<object> entities);
    void RemoveRange(params object[] entities);
    void RemoveRange(IEnumerable<object> entities);
    object? Find(Type entityType, params object?[]? keyValues);
    ValueTask<object?> FindAsync(Type entityType, params object?[]? keyValues);
    ValueTask<object?> FindAsync(Type entityType, object?[]? keyValues, CancellationToken cancellationToken);
    event EventHandler<SavingChangesEventArgs>? SavingChanges;
    event EventHandler<SavedChangesEventArgs>? SavedChanges;
    event EventHandler<SaveChangesFailedEventArgs>? SaveChangesFailed;

    internal bool IsUserCustom();
    internal bool IsTeamCustom();
    internal bool IsRoleCustom();
    internal bool IsSettingCustom();
}