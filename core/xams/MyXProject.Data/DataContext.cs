using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xams.Core.Base;

namespace MyXProject.Data;

public class DataContext : XamsDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
        
        // var folder = Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath(folder);
        // var dbpath = Path.Join(path, "MyXProject.db");
        //
        // optionsBuilder.UseSqlite($"Data Source={dbpath}");
        
        string baseDirectory = AppContext.BaseDirectory;
        string projectDirectory = Path.GetFullPath(Path.Combine(baseDirectory, "../../../../MyXProject.Web"));
        
        optionsBuilder.UseSqlite($"DataSource={projectDirectory}/app.db;").ConfigureWarnings(warnings => warnings
            .Throw(RelationalEventId.QueryPossibleUnintendedUseOfEqualsWarning)
        );
        
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