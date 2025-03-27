using System.Linq.Dynamic.Core;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Utils;

public static class QueryUtil
{
    public static string[] GetTables(ReadInput? readInput)
    {
        List<string> results = new();
        
        if (readInput == null) return results.ToArray();
            
        results.Add(readInput.tableName);
            
        if (readInput.joins != null)
        {
            foreach (var join in readInput.joins)
            {
                results.Add(join.toTable);
            }
        }

        if (readInput.except != null)
        {
            foreach (var exclude in readInput.except)
            {
                results.AddRange(GetTables(exclude.query));
            }
        }

        return results.ToArray();
    }
    
    /// <summary>
    /// Adds a "Contains" filter to an IQueryable based on an array of primitive key values
    /// </summary>
    /// <typeparam name="T">Type of the entity</typeparam>
    /// <param name="query">The IQueryable to filter</param>
    /// <param name="ids">Array of primary key values</param>
    /// <param name="primaryKeyName">Name of the primary key property (defaults to "Id")</param>
    /// <returns>Filtered IQueryable with Where clause applied</returns>
    public static IQueryable WhereIdIn(this IQueryable query, object[] ids, string primaryKeyName)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (ids == null || ids.Length == 0) return query;
        
        // Get the non-null IDs for type determination
        var nonNullIds = ids.Where(id => id != null).ToArray();
        if (nonNullIds.Length == 0) return query;

        // Determine the type and apply the appropriate filter
        if (nonNullIds[0] is int || nonNullIds[0] is int?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<int>());
        }
        else if (nonNullIds[0] is long || nonNullIds[0] is long?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<long>());
        }
        else if (nonNullIds[0] is short || nonNullIds[0] is short?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<short>());
        }
        else if (nonNullIds[0] is byte || nonNullIds[0] is byte?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<byte>());
        }
        else if (nonNullIds[0] is uint || nonNullIds[0] is uint?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<uint>());
        }
        else if (nonNullIds[0] is ulong || nonNullIds[0] is ulong?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<ulong>());
        }
        else if (nonNullIds[0] is ushort || nonNullIds[0] is ushort?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<ushort>());
        }
        else if (nonNullIds[0] is sbyte || nonNullIds[0] is sbyte?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<sbyte>());
        }
        else if (nonNullIds[0] is decimal || nonNullIds[0] is decimal?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<decimal>());
        }
        else if (nonNullIds[0] is string)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<string>());
        }
        else if (nonNullIds[0] is Guid || nonNullIds[0] is Guid?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<Guid>());
        }
        else if (nonNullIds[0] is DateTime || nonNullIds[0] is DateTime?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<DateTime>());
        }
        else if (nonNullIds[0] is DateTimeOffset || nonNullIds[0] is DateTimeOffset?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<DateTimeOffset>());
        }
        else if (nonNullIds[0] is TimeSpan || nonNullIds[0] is TimeSpan?)
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<TimeSpan>());
        }
        else if (nonNullIds[0] is Enum)
        {
            // Use dynamic typing for enum values
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList(nonNullIds[0].GetType()));
        }
        else if (nonNullIds[0] is byte[])
        {
            return query.Where($"@0.Contains({primaryKeyName})", ids.ToDynamicList<byte[]>());
        }
        else
        {
            // Generic fallback - try to use the actual type of the first element
            var elementType = nonNullIds[0].GetType();
            // Use generic method with Type parameter - converting using reflection
            var method = typeof(DynamicQueryableExtensions).GetMethod("ToDynamicList");
            var genericMethod = method.MakeGenericMethod(elementType);
            var dynamicList = genericMethod.Invoke(null, new object[] { ids });
            
            return query.Where($"@0.Contains({primaryKeyName})", dynamicList);
        }
    }
    
}