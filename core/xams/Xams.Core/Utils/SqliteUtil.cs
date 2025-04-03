using Microsoft.EntityFrameworkCore;
using Xams.Core.Base;

namespace Xams.Core.Utils;

public class SqliteUtil
{
    /// <summary>
    /// Upon creating a non-nullable field, Sqlite defaults the value to null
    /// This creates an issue with the entity framework upon querying because it's expecting a non-nullable value
    /// This updates the database with the default values
    /// </summary>
    public static async Task Repair(IXamsDbContext dbContext)
    {
        if (dbContext.Database.ProviderName?.ToLower().Contains("sqlite") == true)
        {
            Console.WriteLine("Updating SQLite non-nullable fields");
            foreach (var entityType in dbContext.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    var isNullable = Nullable.GetUnderlyingType(property.ClrType) != null;
                    if (!isNullable && (property.ClrType.IsPrimitive || property.ClrType == typeof(decimal)))
                    {
                        var primaryKey = entityType.ClrType.Metadata().PrimaryKey;
                        var tableName = entityType.GetTableName() ?? string.Empty;
                        if (string.IsNullOrEmpty(tableName))
                        {
                            continue;
                        }
                        
                        // Check to see if there are any values that need to be updated
                        string query = $"SELECT {primaryKey} FROM {tableName} WHERE {property.Name} IS NULL LIMIT 1";
                        var ids = await dbContext.Database
                            .SqlQueryRaw<Guid>(query)
                            .ToListAsync();
                        // Perform update
                        if (ids.Count > 0)
                        {
                            query = $"UPDATE {tableName} SET {property.Name} = 0 WHERE {property.Name} IS NULL";
                            await dbContext.Database.ExecuteSqlRawAsync(query);    
                        }
                    }
                }    
            }
        }
    }
}