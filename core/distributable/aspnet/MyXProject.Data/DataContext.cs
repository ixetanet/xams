using Microsoft.EntityFrameworkCore;
using MyXProject.Common.Entities;
using Xams.Core.Base;

namespace MyXProject.Data;

public class DataContext : BaseDbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Team> Teams { get; set; }
    public DbSet<TeamUser> TeamUsers { get; set; }
    public DbSet<TeamRole> TeamRoles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Option> Options { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<MyXProject.Common.Entities.System> Systems { get; set; }
    public DbSet<Server> Servers { get; set; }
    public DbSet<Job> Jobs { get; set; }
    public DbSet<JobHistory> JobHistories { get; set; }
    public DbSet<Audit> Audits { get; set; }
    public DbSet<AuditField> AuditFields { get; set; }
    public DbSet<AuditHistory> AuditHistories { get; set; }
    public DbSet<AuditHistoryDetail> AuditHistoryDetails { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var dbpath = Path.Join(path, "MyXProject.db");

        optionsBuilder.UseSqlite($"Data Source={dbpath}");
        
        // Postgres
        // string connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? "";
        // optionsBuilder.UseNpgsql(connectionString, builder =>
        // {
        //
        // });
    }
    
    // Uncomment if using SQL Server
    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     base.OnModelCreating(modelBuilder);
    //     
    //     foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
    //     {
    //         relationship.DeleteBehavior = DeleteBehavior.Restrict;
    //     }
    // }
}