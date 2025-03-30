using System.Data.Common;
using System.Dynamic;
using System.Linq.Dynamic.Core;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Entities;
using Xams.Core.Services;
using Xams.Core.Utils;

namespace Xams.Core.Repositories
{
    public class DataRepository : IDisposable
    {
        private readonly Type _dataContextType;
        private readonly IXamsDbContext? _dataContext;
        private IDbContextTransaction? _transaction;
        private readonly NullabilityInfoContext _nullabilityInfoContext;
        private readonly List<IXamsDbContext> _dbContexts = new();

        public DataRepository(Type dataContext)
        {
            _dataContextType = dataContext;
            _dataContext = (IXamsDbContext?)Activator.CreateInstance(_dataContextType) ??
                           throw new ArgumentNullException();
            _dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            _nullabilityInfoContext = new();
        }

        public TDbContext GetDbContext<TDbContext>() where TDbContext : IXamsDbContext
        {
            if (!(_dataContext is TDbContext))
            {
                throw new Exception($"Cannot GetDbContext, DbContext is not of type {typeof(TDbContext).Name}.");
            }

            return (TDbContext)_dataContext!;
        }

        public IXamsDbContext CreateNewDbContext()
        {
            return CreateNewDbContext<IXamsDbContext>();
        }

        public T CreateNewDbContext<T>() where T : IXamsDbContext
        {
            var dbContext = ((T)Activator.CreateInstance(_dataContextType)!);
            dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            _dbContexts.Add(dbContext);
            return dbContext;
        }

        internal XamsDbContext<User, Team, Role, Setting> GetInternalDbContext()
        {
            var optionsBuilder = _dataContext.GetDbOptionsBuilder();
            if (optionsBuilder == null)
            {
                throw new Exception($"Failed to get db options builder. Ensure that base.OnConfiguring(optionsBuilder) is in the OnConfiguring method of the DbContext.");
            }
            var baseDbContext = new XamsDbContext<User, Team, Role, Setting>(optionsBuilder.Options);
            _dbContexts.Add(baseDbContext);
            return baseDbContext;
        }

        public async Task<Response<ReadOutput>> Find(string tableName, object[] ids, bool newDataContext, string[]? fields = null, bool updateFieldPrefixes = false)
        {
            IXamsDbContext dataContext = GetDbContext<IXamsDbContext>();
        
            if (newDataContext)
            {
                dataContext = CreateNewDbContext();
            }

            List<dynamic> results = new List<dynamic>();
            
            // Batches of 500
            List<object> batchIds = new List<object>();
            batchIds.AddRange(ids);
            
            List<object> toProcess = new List<object>();
            while (batchIds.Count > 0)
            {
                toProcess.AddRange(batchIds.Take(500));
                batchIds.RemoveRange(0, Math.Min(500, batchIds.Count));
                var query = new Query(dataContext, fields ?? ["*"])
                    .From(tableName)
                    .Contains($"root_{tableName}Id", toProcess.ToArray());

                var batchResult = await query.ToDynamicListAsync();
                results.AddRange(batchResult);
                // string where = string.Join(" OR ", toProcess.Select(x => $"root_{tableName}Id == \"{x}\""));
                // results.AddRange(await new Query(dataContext, fields ?? ["*"])
                //     .From(tableName).Where(where)
                //     .ToDynamicListAsync());
                
                toProcess.Clear();
            }

            if (updateFieldPrefixes)
            {
                results = UpdateFieldPrefixes(results);
            }
            
            return new Response<ReadOutput>()
            {
                Succeeded = true,
                Data = new ReadOutput()
                {
                    results = results
                }
            };
        }
        public async Task<Response<object?>> Find(string tableName, object id, bool newDataContext)
        {
            IXamsDbContext dataContext = GetDbContext<IXamsDbContext>();
        
            if (newDataContext)
            {
                dataContext = CreateNewDbContext();
            }
            
            var entity = await dataContext.FindAsync(Cache.Instance.GetTableMetadata(tableName).Type, id);
            
            
            return new Response<object?>()
            {
                Succeeded = true,
                Data = entity,
            };
        }
        /// <summary>
        /// This will return a list of ExpandoObjects
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="readInput"></param>
        /// <param name="readOptions"></param>
        /// <returns></returns>
        public async Task<Response<ReadOutput>> Read(Guid userId, ReadInput readInput, ReadOptions readOptions)
        {
            try
            {
                IXamsDbContext dataContext = _dataContext!;

                if (readOptions.NewDataContext)
                {
                    dataContext = CreateNewDbContext();
                }

                if (readInput.page <= 0)
                {
                    readInput.page = 1;
                }

                if (readInput.maxResults == null)
                {
                    readInput.maxResults = 5;
                }

                string[] permissions = readOptions.Permissions;
                if (readOptions.BypassSecurity)
                {
                    permissions = [$"TABLE_{readInput.tableName}_READ_SYSTEM"];
                }
                
                IQueryable query = new QueryFactory(dataContext, new QueryFactory.QueryOptions()
                {
                    UserId = userId,
                    Permissions = permissions
                },readInput).Create().ToQueryable();
                
                
                int totalCount = query.Count();
                int totalPages = Convert.ToInt32(Math.Ceiling(totalCount / (float)readInput.maxResults));

                query = query
                    .Skip((readInput.page - 1) * (int)readInput.maxResults)
                    .Take((int)readInput.maxResults);

                var results = await query.ToDynamicListAsync();

                // If using SQL Server, dates come back as "Unspecified" by default, convert dates to UTC
                if (dataContext.GetDbProvider() != DbProvider.Postgres)
                {
                    DatesToUTC(results);
                }
                
                // Remove root_ prefix from fields and change alias prefixes to "alias."
                results = UpdateFieldPrefixes(results, readInput);
                
                // If denormalize is true, denormalize the results
                if (readInput.denormalize == true)
                {
                    results = Denormalize(results, readInput);
                }
                
                return new Response<ReadOutput>()
                {
                    Succeeded = true,
                    Data = new ReadOutput()
                    {
                        currentPage = readInput.page,
                        totalResults = totalCount,
                        maxResults = (int)readInput.maxResults,
                        tableName = readInput.tableName,
                        pages = totalPages,
                        orderBy = readInput.orderBy,
                        distinct = readInput.distinct,
                        denormalize = readInput.denormalize,
                        results = results
                    }
                };
            }
            catch (Exception e)
            {
                return new Response<ReadOutput>()
                {
                    Succeeded = false,
                    FriendlyMessage = e.Message,
                    LogMessage = e.StackTrace ?? e.InnerException?.Message
                };
            }
        }

        public async Task<Response<object?>> Create<T>(T? entity, bool preventSave)
        {
            var response = ValidateFields(entity);
            if (!response.Succeeded)
            {
                return response;
            }

            _dataContext!.Add(entity ?? throw new ArgumentNullException(nameof(entity)));
            if (!preventSave) await _dataContext.SaveChangesAsync();

            return new Response<object?>()
            {
                Succeeded = true
            };
        }

        public async Task<Response<object?>> Update<T>(T? entity, bool preventSave)
        {
            var response = ValidateFields(entity);
            if (!response.Succeeded)
            {
                return response;
            }

            _dataContext!.Update(entity ?? throw new ArgumentNullException(nameof(entity)));
            if (!preventSave) await _dataContext.SaveChangesAsync();

            return new Response<object?>()
            {
                Succeeded = true
            };
        }

        public async Task<Response<object?>> Delete<T>(T? entity, bool preventSave)
        {
            _dataContext!.Remove(entity ?? throw new ArgumentNullException(nameof(entity)));
            if (!preventSave) await _dataContext.SaveChangesAsync();

            return new Response<object?>()
            {
                Succeeded = true
            };
        }

        public async Task SaveChangesAsync()
        {
            if (_dataContext != null) await _dataContext.SaveChangesAsync();
        }

        /// <summary>
        /// If the User, Team, Role or Setting has been extended a new discriminator column
        /// has been added, we need to ensure that it has the correct value
        /// </summary>
        internal async Task FixDiscriminators()
        {
            var appDbContext = CreateNewDbContext();
            var db = GetInternalDbContext();
            if (appDbContext.IsUserCustom())
            {
                // If User is extended with a custom entity ensure that all the user
                // records are using the custom entities discriminator
                var user = await db.Users.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 0) // Non-Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Users.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 1));
                }
            }
            else
            {
                var user = await db.Users.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 1) // Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Users.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 0));
                }
            }
            
            if (appDbContext.IsTeamCustom())
            {
                // If User is extended with a custom entity ensure that all the user
                // records are using the custom entities discriminator
                var user = await db.Teams.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 0) // Non-Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Teams.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 1));
                }
            }
            else
            {
                var user = await db.Teams.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 1) // Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Teams.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 0));
                }
            }
            
            if (appDbContext.IsRoleCustom())
            {
                // If User is extended with a custom entity ensure that all the user
                // records are using the custom entities discriminator
                var user = await db.Roles.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 0) // Non-Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Roles.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 1));
                }
            }
            else
            {
                var user = await db.Roles.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 1) // Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Roles.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 0));
                }
            }
            
            if (appDbContext.IsSettingCustom())
            {
                // If User is extended with a custom entity ensure that all the user
                // records are using the custom entities discriminator
                var user = await db.Settings.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 0) // Non-Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Settings.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 1));
                }
            }
            else
            {
                var user = await db.Settings.IgnoreQueryFilters()
                    .Where(x => x.Discriminator == 1) // Custom
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    await db.Settings.ExecuteUpdateAsync(x 
                        => x.SetProperty(y => y.Discriminator, 0));
                }
            }
            
        }

        /// <summary>
        /// If there are joins, flatten the results 
        /// </summary>
        /// <param name="results"></param>
        /// <param name="readInput"></param>
        /// <returns></returns>
        private List<dynamic> UpdateFieldPrefixes(List<dynamic> results, ReadInput? readInput = null)
        {
            if (results.Count == 0)
            {
                return results;
            }
            PropertyInfo[] properties = results.First().GetType().GetProperties(); //result.GetType().GetProperties();
            List<dynamic> newResults = new();
            List<string?> aliases = readInput?.joins?.Select(x => x.alias).ToList() ?? new List<string?>();
            foreach (var result in results)
            {
                dynamic expando = new ExpandoObject();
                IDictionary<string, object?> expandoDictionary = ((IDictionary<string, object?>)expando);
                newResults.Add(expando);
                
                foreach (var property in properties)
                {
                    
                    if (property.Name == "Item")
                        continue;
                    
                    string fieldName = property.Name;
                    if (fieldName.StartsWith("root_"))
                    {
                        fieldName = fieldName.Substring(5);
                    }
                    // If any field names start with an alias, remove the alias
                    string? alias = aliases.FirstOrDefault(x => fieldName.StartsWith($"{x}_"));
                    if (!string.IsNullOrEmpty(alias))
                    {
                        fieldName = $"{alias}.{fieldName.Substring(alias.Length + 1)}" ;
                    }
                    
                    expandoDictionary[fieldName] = property.GetValue(result);
                }
            }

            return newResults;
        }

        private List<dynamic> Denormalize(List<dynamic> results, ReadInput readInput)
        {
            if (readInput.joins == null || readInput.joins.Length == 0)
            {
                return results;
            }

            // Get root records
            List<dynamic> rootRecords = new List<dynamic>();
            HashSet<Guid> rootUniqueIds = new HashSet<Guid>();
            foreach (IDictionary<string, object?> result in results.Cast<IDictionary<string, object?>>())
            {
                Guid id = (Guid)result[$"{readInput.tableName}Id"]!;
                if (!rootUniqueIds.Contains(id))
                {
                    rootUniqueIds.Add(id);
                    IDictionary<string, object?> newResult = new ExpandoObject();
                    rootRecords.Add(newResult);
                    // Add all fields for this record
                    foreach (string key in result.Keys)
                    {
                        if (!key.Contains('.'))
                        {
                            newResult[key] = result[key];
                        }
                    }
                }
            }

            // Get joined records
            Dictionary<string, List<dynamic>> joinedRecords = new Dictionary<string, List<dynamic>>();
            foreach (Join join in readInput.joins)
            {
                HashSet<Guid> joinUniqueIds = new HashSet<Guid>();
                List<dynamic> joinResults = new List<dynamic>();
                string alias = string.IsNullOrEmpty(join.alias) ? join.toTable : join.alias;
                joinedRecords.Add(alias, joinResults);
                foreach (IDictionary<string, object?> result in results.Cast<IDictionary<string, object?>>())
                {
                    if (result.ContainsKey($"{alias}.{join.toField}"))
                    {
                        if (result.ContainsKey($"{alias}.{join.toTable}Id") && 
                            result[$"{alias}.{join.toTable}Id"] != null &&
                            !joinUniqueIds.Contains((Guid)(result[$"{alias}.{join.toTable}Id"] ?? 
                                                           throw new InvalidOperationException($"Failed to get {alias}.{join.toTable}Id"))))
                        {
                            joinUniqueIds.Add((Guid)result[$"{alias}.{join.toTable}Id"]!);
                            IDictionary<string, object?> newResult = new ExpandoObject();
                            joinResults.Add(newResult);
                            // Add all fields for this record
                            foreach (string key in result.Keys)
                            {
                                string[] split = key.Split('.');
                                if (key.Contains('.') && split[0] == alias)
                                {
                                    newResult[split[1]] = result[key];
                                }
                            }
                        }
                    }
                }
            }

            rootRecords = DenormalizeJoins(readInput.tableName, rootRecords, joinedRecords, readInput);
            
            return rootRecords;
        }

        public List<dynamic> DenormalizeJoins(string fromTable, List<dynamic> results, Dictionary<string, List<dynamic>> joinedRecords, ReadInput readInput)
        {
            if (readInput.joins == null)
            {
                throw new Exception($"No joins found query.");
            }
            foreach (IDictionary<string, object?> rootRecord in results)
            {
                foreach (Join join in readInput.joins)
                {
                    // Root joins
                    if (join.fromTable == fromTable)
                    {
                        string alias = string.IsNullOrEmpty(join.alias) ? join.toTable : join.alias;
                        List<dynamic> aliasRecords = joinedRecords[alias];
                        List<dynamic> relatedRecords = new();
                        rootRecord[alias] = relatedRecords;
                        foreach (IDictionary<string, object?> aRecord in aliasRecords)
                        {
                            if ((Guid)aRecord[join.toField]! == (Guid)rootRecord[join.fromField]!)
                            {
                                relatedRecords.Add(aRecord);
                            }
                        }
                        
                        DenormalizeJoins(alias, relatedRecords, joinedRecords, readInput);
                    }
                }
            }
            return results;
        }

        private Response<object?> ValidateFields<T>(T? entity)
        {
            if (entity == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Entity is null."
                };
            }
            foreach (var property in entity.GetType().GetProperties())
            {
                bool isNullable = Nullable.GetUnderlyingType(property.PropertyType) != null;
                // If the property is not nullable and is a value (numbers, and not a string) type, check if it is null
                if (!isNullable && property.PropertyType.IsValueType)
                {
                    var value = property.GetValue(entity);
                    if (value == null)
                    {
                        return new Response<object?>()
                        {
                            Succeeded = false,
                            FriendlyMessage =
                                $"{entity.GetType().Name}.{property.Name} is not nullable and is set to null."
                        };
                    }
                }
                // If the property is not a value type (string), check if it is null
                else if (!property.PropertyType.IsValueType && property.PropertyType == typeof(string))
                {
                    var value = property.GetValue(entity);
                    if (!IsNullable(property) && value == null)
                    {
                        return new Response<object?>()
                        {
                            Succeeded = false,
                            FriendlyMessage =
                                $"{entity.GetType().Name}.{property.Name} is not nullable and is set to null."
                        };
                    }
                }
            }
            
            return new Response<object?>()
            {
                Succeeded = true
            };
        }

        private void DatesToUTC(List<dynamic> results)
        {
            PropertyInfo[] properties = [];
            foreach (var result in results)
            {
                if (properties!.Length == 0)
                {
                    properties = result.GetType().GetProperties();
                }
                properties.Where(x => x.PropertyType == typeof(DateTime) || x.PropertyType == typeof(DateTime?)).ToList().ForEach(x =>
                {
                    var date = x.GetValue(result);
                    if (date != null)
                    {
                        x.SetValue(result, DateTime.SpecifyKind(date, DateTimeKind.Utc));    
                    }
                });
            }
        }

        private bool IsNullable(PropertyInfo property)
        {
            var info = _nullabilityInfoContext.Create(property);
            if (info.WriteState == NullabilityState.Nullable)
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _dataContext?.Dispose();
            foreach (var dbContext in _dbContexts)
            {
                dbContext.Dispose();
            }
        }

        public async Task BeginTransaction()
        {
            _transaction ??= await _dataContext?.Database.BeginTransactionAsync()!;
        }

        public async Task CommitTransaction()
        {
            // Only allow commit if SaveChanges was called and there's something to commit
            if (_transaction != null && GetDbContext<IXamsDbContext>().SaveChangesCalledWithPendingChanges())
            {
                await _transaction.CommitAsync();
            }
        }

        public async Task RollbackTransaction()
        {
            // Only allow rollback if SaveChanges was called and there's something to roll back
            if (_transaction != null && GetDbContext<IXamsDbContext>().SaveChangesCalledWithPendingChanges())
            {
                await _transaction.RollbackAsync();
            }
        }
        

    }
    
    public class ReadOptions
    {
        public required string[] Permissions { get; init; }
        public bool BypassSecurity { get; init; }
        public bool NewDataContext { get; init; }
    }
}