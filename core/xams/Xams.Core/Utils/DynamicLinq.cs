using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Runtime.InteropServices.JavaScript;
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

            PrimaryKey = Cache.Instance.GetTableMetadata(EntityUtil.GetTableName(TargetType, db.GetType()).TableName).PrimaryKey;
        }
        
        public static async Task<List<dynamic>> BatchRequest(T db, Type entity, List<object> ids, int batchSize = 500)
        {
            var dynamicLinq = new DynamicLinq<T>(db, entity);
            var query = dynamicLinq.Query;
            var idList = ids.ToList();
            var results = new List<object>();
            while (idList.Any())
            {
                var batch = idList.Take(batchSize).ToArray();
                idList = idList.Skip(batchSize).ToList();
                query = query.WhereIdIn(batch, dynamicLinq.PrimaryKey);
                results.AddRange(await query.ToDynamicListAsync());
            }
            return results;
        }
        
        /// <summary>
        /// dbFactory needs to return a new data context. The key is the entity id
        /// </summary>
        /// <param name="dbFactory"></param>
        /// <param name="entity"></param>
        /// <param name="ids"></param>
        /// <param name="batchSize"></param>
        /// <returns></returns>
        public static async Task<ConcurrentDictionary<object, dynamic>> BatchRequestThreaded(Func<T> dbFactory, Type entity, List<object> ids, int batchSize = 500)
        {
            var results = new ConcurrentDictionary<object, dynamic>();
            var idList = ids.ToList();
            
            var batches = new List<object[]>();
            while (idList.Any())
            {
                var batch = idList.Take(batchSize).ToArray();
                idList = idList.Skip(batchSize).ToList();
                batches.Add(batch);
            }

            await Parallel.ForEachAsync(batches, async (batch, token) =>
            {
                var db = dbFactory();
                var dLinq = new DynamicLinq<T>(db, entity);
                var query = dLinq.Query.WhereIdIn(batch, dLinq.PrimaryKey);
                var entities = await query.ToDynamicListAsync(cancellationToken: token);
                foreach (var ent in entities)
                {
                    results.TryAdd(((object)ent).GetIdValue(entity), ent);
                }
            });

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