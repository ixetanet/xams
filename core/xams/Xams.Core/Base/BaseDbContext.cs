using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xams.Core.Utils;

namespace Xams.Core.Base
{
    public class BaseDbContext : DbContext
    {
        internal bool SaveChangesCalledWithPendingChanges { get; private set; }
        
        public BaseDbContext()
        {
        }

        public BaseDbContext(DbContextOptions<DbContext> options) : base(options)
        {
        }
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (!SaveChangesCalledWithPendingChanges)
            {
                SaveChangesCalledWithPendingChanges = ChangeTracker.Entries().Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);    
            }
            
            return await base.SaveChangesAsync(cancellationToken);
        }
        
        public override int SaveChanges()
        {
            if (!SaveChangesCalledWithPendingChanges)
            {
                SaveChangesCalledWithPendingChanges = ChangeTracker.Entries().Any(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);    
            }
            return base.SaveChanges();
        }
        

        /// <summary>
        /// Returns the current database provider.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public DbProvider GetDBProvider()
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

            var sql = SqlTranslator.Translate(postgresSql, GetDBProvider());
            await base.Database.ExecuteSqlRawAsync(sql, dbParameters);
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