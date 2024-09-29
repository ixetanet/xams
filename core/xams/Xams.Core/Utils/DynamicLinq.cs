using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;

namespace Xams.Core.Utils
{
    public class DynamicLinq<T> where T : DbContext
    {
        public string PrimaryKey { get; private set; } = null!;
        public IQueryable Query { get; private set; } = null!;
        public object DbSet { get; private set; } = null!;
        public Type TargetType { get; private set; }
    
        public DynamicLinq(T db, Type entity)
        {
            TargetType = entity;
            OnCreate(db);
        }
    
        private void OnCreate(T db)
        {
            var dbSetProperty = db.GetType()
                .GetProperties()
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                    p.PropertyType.GenericTypeArguments[0] == TargetType);

            if (dbSetProperty == null)
            {
                throw new InvalidOperationException($"DbSet for type {TargetType.Name} not found in the context.");
            }

            DbSet = dbSetProperty.GetValue(db)!;
            Query = (IQueryable)DbSet;
            
            PrimaryKey = TargetType.Name + "Id";
        }
        
        public static async Task<List<dynamic>> BatchRequest(T db, Type entity, List<Guid> ids, int batchSize = 25)
        {
            var dynamicLinq = new DynamicLinq<T>(db, entity);
            var query = dynamicLinq.Query;
            var idList = ids.ToList();
            var results = new List<dynamic>();
            while (idList.Any())
            {
                var batch = idList.Take(batchSize).ToList();
                idList = idList.Skip(batchSize).ToList();
                var orString = string.Join(" || ", batch.Select(x => $"{dynamicLinq.PrimaryKey} == \"{x}\""));
                query = query.Where(orString);
                results.AddRange(await query.ToDynamicListAsync());
            }
            return results;
        }
        
        public static async Task<dynamic?> Find(T db, Type entity, Guid id)
        {
            var dynamicLinq = new DynamicLinq<T>(db, entity);
            var query = dynamicLinq.Query; 
            query = query.Where($"{dynamicLinq.PrimaryKey} == @0", id);
            return (await query.ToDynamicListAsync()).FirstOrDefault();
        }
        
        public static async Task<List<dynamic>> FindAll(T db, Type entity)
        {
            var dynamicLinq = new DynamicLinq<T>(db, entity);
            return await dynamicLinq.Query.ToDynamicListAsync();
        }
    
    }
}