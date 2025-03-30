using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xams.Core.Entities;
using Xams.Core.Utils;

namespace Xams.Core.Base
{
    
    public class XamsDbContext : XamsDbContext<User>
    {
    }

    public class XamsDbContext<TUser> : XamsDbContext<TUser, Team>
        where TUser : User,
        new()
    {
    }

    public class XamsDbContext<TUser, TTeam> : XamsDbContext<TUser, TTeam, Role>
        where TUser : User 
        where TTeam : Team,
        new()
    {
    }

    public class XamsDbContext<TUser, TTeam, TRole> : XamsDbContext<TUser, TTeam, TRole, Setting>
        where TUser : User
        where TTeam : Team 
        where TRole : Role,
        new()
    {
    }

    public class XamsDbContext<TUser, TTeam, TRole, TSetting> : DbContext, IXamsDbContext
        where TUser : User
        where TTeam : Team
        where TRole : Role
        where TSetting : Setting, new()
    {
        public DbSet<TUser> Users { get; set; } = null!;
        public DbSet<TRole> Roles { get; set; } = null!;
        public DbSet<Permission> Permissions { get; set; } = null!;
        public DbSet<TTeam> Teams { get; set; } = null!;
        public DbSet<TeamUser<TUser, TTeam>> TeamUsers { get; set; } = null!;
        public DbSet<TeamRole<TTeam, TRole>> TeamRoles { get; set; } = null!;
        public DbSet<RolePermission<TRole>> RolePermissions { get; set; } = null!;
        public DbSet<UserRole<TUser, TRole>> UserRoles { get; set; } = null!;
        public DbSet<Option> Options { get; set; } = null!;
        public DbSet<TSetting> Settings { get; set; } = null!;
        public DbSet<Xams.Core.Entities.System> Systems { get; set; } = null!;
        public DbSet<Server> Servers { get; set; } = null!;
        public DbSet<Job> Jobs { get; set; } = null!;
        public DbSet<JobHistory> JobHistories { get; set; } = null!;
        public DbSet<Audit> Audits { get; set; }
        public DbSet<AuditField> AuditFields { get; set; }
        public DbSet<AuditHistory> AuditHistories { get; set; }
        public DbSet<AuditHistoryDetail> AuditHistoryDetails { get; set; }

        internal bool SaveChangesCalledWithPendingChanges { get; private set; }
        internal DbContextOptionsBuilder? OptionsBuilder { get; set; } = null!;
        
        public XamsDbContext()
        {
        }

        public XamsDbContext(DbContextOptions options) : base(options)
        {
        }

        public enum EntityType
        {
            Default = 0,
            Extended = 1
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            OptionsBuilder = optionsBuilder;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            var userDiscriminator = modelBuilder.Entity<User>() 
                .HasDiscriminator(x => x.Discriminator)
                .HasValue<User>((int)EntityType.Default);
            if (typeof(TUser) != typeof(User))
            {
                userDiscriminator.HasValue<TUser>((int)EntityType.Extended);
            }
            
            modelBuilder.Entity<User>().HasIndex(x => x.Discriminator);
            
            var teamDiscriminator = modelBuilder.Entity<Team>()
                .HasDiscriminator(x => x.Discriminator)
                .HasValue<Team>((int)EntityType.Default);
            if (typeof(TTeam) != typeof(Team))
            {
                teamDiscriminator.HasValue<TTeam>((int)EntityType.Extended);
            }
            modelBuilder.Entity<Team>().HasIndex(x => x.Discriminator);

            var roleDiscriminator = modelBuilder.Entity<Role>()
                .HasDiscriminator(x => x.Discriminator)
                .HasValue<Role>((int)EntityType.Default);
            if (typeof(TRole) != typeof(Role))
            {
                roleDiscriminator.HasValue<TRole>((int)EntityType.Extended);
            }
            modelBuilder.Entity<Role>().HasIndex(x => x.Discriminator);
            
            var settingDiscriminator = modelBuilder.Entity<Setting>()
                .HasDiscriminator(x => x.Discriminator)
                .HasValue<Setting>((int)EntityType.Default);
            if (typeof(TSetting) != typeof(Setting))
            {
                settingDiscriminator.HasValue<TSetting>((int)EntityType.Extended);
            }
            modelBuilder.Entity<Setting>().HasIndex(x => x.Discriminator);
        }

        bool IXamsDbContext.SaveChangesCalledWithPendingChanges()
        {
            return SaveChangesCalledWithPendingChanges;
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!SaveChangesCalledWithPendingChanges)
            {
                SaveChangesCalledWithPendingChanges = ChangeTracker.Entries().Any(e =>
                    e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            if (!SaveChangesCalledWithPendingChanges)
            {
                SaveChangesCalledWithPendingChanges = ChangeTracker.Entries().Any(e =>
                    e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);
            }

            return base.SaveChanges();
        }


        /// <summary>
        /// Returns the current database provider.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public DbProvider GetDbProvider()
        {
            string providerName = Database.GetService<IDatabaseProvider>().Name;

            if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                return DbProvider.Postgres;
            }

            if (providerName == "Microsoft.EntityFrameworkCore.SqlServer")
            {
                return DbProvider.SQLServer;
            }

            if (providerName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                return DbProvider.SQLite;
            }

            if (providerName == "Pomelo.EntityFrameworkCore.MySql")
            {
                return DbProvider.MySQL;
            }

            if (providerName == "MySql.EntityFrameworkCore")
            {
                return DbProvider.MySQL;
            }


            throw new Exception($"Database provider {providerName} not supported.");
        }

        public DbContextOptionsBuilder? GetDbOptionsBuilder()
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            OnConfiguring(optionsBuilder);
            return OptionsBuilder;
        }

        /// <summary>
        /// Takes in raw SQL for Postgres, translates it to the current database provider, and executes it.
        /// </summary>
        /// <param name="postgresSql"></param>
        /// <param name="parameters"></param>
        /// <exception cref="Exception"></exception>
        public async Task ExecuteRawSql(string postgresSql, Dictionary<string, object?>? parameters = null)
        {
            DbProviderFactory? factory = DbProviderFactories.GetFactory(Database.GetDbConnection());

            if (factory == null)
            {
                throw new Exception("Failed to get database provider factory.");
            }

            List<DbParameter> dbParameters = new();
            if (parameters != null)
            {
                foreach (var kvp in parameters)
                {
                    var parameter = factory.CreateParameter();
                    if (parameter != null)
                    {
                        parameter.ParameterName = kvp.Key;
                        parameter.Value = kvp.Value ?? DBNull.Value;
                        dbParameters.Add(parameter);
                    }
                }
            }

            var sql = SqlTranslator.Translate(postgresSql, GetDbProvider());
            await base.Database.ExecuteSqlRawAsync(sql, dbParameters);
        }

        public bool IsUserCustom()
        {
            if (typeof(TUser) == typeof(User))
            {
                return false;
            }
            return true;
        }

        public bool IsTeamCustom()
        {
            if (typeof(TTeam) == typeof(Team))
            {
                return false;
            }
            return true;
        }

        public bool IsRoleCustom()
        {
            if (typeof(TRole) == typeof(Role))
            {
                return false;
            }
            return true;
        }

        public bool IsSettingCustom()
        {
            if (typeof(TSetting) == typeof(Setting))
            {
                return false;
            }
            return true;
        }
    }

    public enum DbProvider
    {
        MySQL,
        SQLServer,
        SQLite,
        Postgres
    }
}