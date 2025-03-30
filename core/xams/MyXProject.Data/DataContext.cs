using Microsoft.EntityFrameworkCore;
using Xams.Core.Base;

namespace MyXProject.Data;

public class DataContext : XamsDbContext
{
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