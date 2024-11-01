using System.Linq.Dynamic.Core;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Actions
{
    [ServiceAction("ADMIN_ImportData")]
    public class ADMIN_ImportData : IServiceAction
    {
        public async Task<Response<object?>> Execute(ActionServiceContext context)
        {
            try
            {
                if (context.File == null)
                {
                    return ServiceResult.Error("Missing File");
                }
                
                using var reader = new StreamReader(context.File.OpenReadStream());
                var content = await reader.ReadToEndAsync();
                ADMIN_ExportData.TableExport[]? imports = JsonSerializer.Deserialize<ADMIN_ExportData.TableExport[]>(content);
                if (imports == null)
                {
                    return new Response<object?>()
                    {
                        Succeeded = false,
                        FriendlyMessage = "Failed to parse file"
                    };
                }
                var dataContextUpsert = context.DataRepository.GetDbContext<BaseDbContext>();
                foreach (var import in imports)
                {
                    var dataContext = context.DataRepository.GetDbContext<BaseDbContext>();
                    dataContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                    var dbContextType = Cache.Instance.GetTableMetadata(import.tableName);
                    
                    // Retrieve all the id's from the table
                    DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(dataContext, dbContextType.Type);
                    IQueryable query = dynamicLinq.Query;
                    
                    // If TeamRole, UserRole, or TeamUser match on the joining tables instead of the Primary Key
                    if ("RolePermission" == import.tableName)
                    {
                        // Do nothing
                    } 
                    else if ("TeamRole" == import.tableName)
                    {
                        query = query.Select("new(TeamId, RoleId)");
                    }
                    else if ("UserRole" == import.tableName)
                    {
                        query = query.Select("new(UserId, RoleId)");
                    }
                    else if ("TeamUser" == import.tableName)
                    {
                        query = query.Select("new(TeamId, UserId)");
                    }
                    else
                    {
                        query = query.Select(dbContextType.PrimaryKey);    
                    }
                    
                    if (!new[] { "RolePermission", "TeamRole", "UserRole", "TeamUser" }.Contains(import.tableName))
                    {
                        // Convert to a HashSet for faster lookup
                        List<dynamic> existing = await query.ToDynamicListAsync();
                        HashSet<Guid> existingIds = [..existing.Select(x => (Guid)x)];
                        foreach (var item in ((JsonElement)import.data).EnumerateArray())
                        {
                            object entity = JsonSerializer.Deserialize(item.GetRawText(), dbContextType.Type) ??
                                            throw new Exception("Failed to parse entity");
                            var primaryKeyValue = entity.GetValue<Guid>(dbContextType.PrimaryKey);
                            if (existingIds.Contains(primaryKeyValue))
                            {
                                dataContextUpsert.Update(entity);
                            }
                            else
                            {
                                dataContextUpsert.Add(entity);
                            }
                        }
                    }

                    if (import.tableName == "RolePermission")
                    {
                        List<object> existing = (await query.ToDynamicListAsync()).Select(x => (object)x).ToList();
                        var existingEntities = existing.Select(x => new
                        {
                            RoleId = x.GetValue<Guid>("RoleId"),
                            PermissionId = x.GetValue<Guid>("PermissionId")
                        }).ToList();

                        HashSet<Guid> updateIds = [];
                        foreach (var item in ((JsonElement)import.data).EnumerateArray())
                        {
                            object entity = JsonSerializer.Deserialize(item.GetRawText(), dbContextType.Type) ??
                                            throw new Exception("Failed to parse entity");
                            if (!existingEntities.Any(x => x.RoleId == entity.GetValue<Guid>("RoleId")
                                                          && x.PermissionId == entity.GetValue<Guid>("PermissionId")))
                            {
                                var updateEntity = existing
                                    .FirstOrDefault(x => x.GetValue<Guid>("RolePermissionId") == entity.GetValue<Guid>("RolePermissionId"));
                                if (updateEntity != null)
                                {
                                    updateIds.Add(updateEntity.GetValue<Guid>("RolePermissionId"));
                                    dataContextUpsert.Update(entity);
                                }
                                else
                                {
                                    dataContextUpsert.Add(entity);
                                    existing.Add(entity);
                                    existingEntities.Add(new
                                    {
                                        RoleId = entity.GetValue<Guid>("RoleId"),
                                        PermissionId = entity.GetValue<Guid>("PermissionId")
                                    });
                                }
                            }
                        }
                        
                        // Remove all RolePermissions that are not in the import
                        foreach (var existingEntity in existingEntities)
                        {
                            if (!((JsonElement)import.data).EnumerateArray().Any(x =>
                                x.GetProperty("RoleId").GetGuid() == existingEntity.RoleId &&
                                x.GetProperty("PermissionId").GetGuid() == existingEntity.PermissionId))
                            {
                                var entity = existing.First(x =>
                                    x.GetValue<Guid>("RoleId") == existingEntity.RoleId &&
                                    x.GetValue<Guid>("PermissionId") == existingEntity.PermissionId);
                                
                                // This might be an update because the primary key already exists in the database
                                if (updateIds.Contains(entity.GetValue<Guid>("RolePermissionId")))
                                {
                                    continue;
                                }
                                
                                dataContextUpsert.Remove(entity);
                            }
                        }
                    }
                    
                    if (import.tableName == "TeamRole")
                    {
                        List<object> existing = (await query.ToDynamicListAsync()).Select(x => (object)x).ToList();
                        var existingEntities = existing.Select(x => new
                        {
                            TeamId = x.GetValue<Guid>("TeamId"),
                            RoleId = x.GetValue<Guid>("RoleId")
                        }).ToList();
                        
                        foreach (var item in ((JsonElement)import.data).EnumerateArray())
                        {
                            object entity = JsonSerializer.Deserialize(item.GetRawText(), dbContextType.Type) ??
                                            throw new Exception("Failed to parse entity");
                            if (!existingEntities.Any(x => x.TeamId == entity.GetValue<Guid>("TeamId")
                                                          && x.RoleId == entity.GetValue<Guid>("RoleId")))
                            {
                                dataContextUpsert.Add(entity);
                            }
                        }
                    }
                    
                    if (import.tableName == "UserRole")
                    {
                        List<object> existing = (await query.ToDynamicListAsync()).Select(x => (object)x).ToList();
                        var existingEntities = existing.Select(x => new
                        {
                            UserId = x.GetValue<Guid>("UserId"),
                            RoleId = x.GetValue<Guid>("RoleId")
                        }).ToList();
                        
                        foreach (var item in ((JsonElement)import.data).EnumerateArray())
                        {
                            object entity = JsonSerializer.Deserialize(item.GetRawText(), dbContextType.Type) ??
                                            throw new Exception("Failed to parse entity");
                            if (!existingEntities.Any(x => x.UserId == entity.GetValue<Guid>("UserId")
                                                          && x.RoleId == entity.GetValue<Guid>("RoleId")))
                            {
                                dataContextUpsert.Add(entity);
                            }
                        }
                    }
                    
                    if (import.tableName == "TeamUser")
                    {
                        List<object> existing = (await query.ToDynamicListAsync()).Select(x => (object)x).ToList();
                        var existingEntities = existing.Select(x => new
                        {
                            UserId = x.GetValue<Guid>("UserId"),
                            TeamId = x.GetValue<Guid>("TeamId")
                        }).ToList();
                        
                        foreach (var item in ((JsonElement)import.data).EnumerateArray())
                        {
                            object entity = JsonSerializer.Deserialize(item.GetRawText(), dbContextType.Type) ??
                                            throw new Exception("Failed to parse entity");
                            if (!existingEntities.Any(x => x.UserId == entity.GetValue<Guid>("UserId")
                                                           && x.TeamId == entity.GetValue<Guid>("TeamId")))
                            {
                                dataContextUpsert.Add(entity);
                            }
                        }
                    }
                    
                }
                await dataContextUpsert.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return new Response<object?>()
                {   
                    Succeeded = false,
                    FriendlyMessage = $"Failed to import data: {e.Message}"
                };
            }
        
            return new Response<object?>()
            {
                Succeeded = true,
                Data = null
            };
        }
    }
}