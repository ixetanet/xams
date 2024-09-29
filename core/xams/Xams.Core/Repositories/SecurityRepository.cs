using System.Linq.Dynamic.Core;
using System.Text.Json;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Repositories
{
    public class SecurityRepository 
    {
        private Type _dataContextType = typeof(BaseDbContext);
        private BaseDbContext? _dataContext;

        public SecurityRepository(Type dataContext)
        {
            _dataContextType = dataContext;
            _dataContext = (BaseDbContext?)Activator.CreateInstance(_dataContextType);
        }
    
        public BaseDbContext? GetDataContext()
        {
            return (BaseDbContext?)Activator.CreateInstance(_dataContextType);
        }

        public async Task<Response<object?>> Get(PermissionsInput permissionsInput, Guid userId)
        {
            if (permissionsInput.method == "has_permissions")
            {
                return await UserPermissions(permissionsInput, userId);
            }

            return new Response<object?>()
            {
                Succeeded = false,
                FriendlyMessage = "Invalid method"
            };
        }
        public async Task<Response<object?>> UserPermissions(PermissionsInput permissionsInput, Guid userId)
        {
            if (permissionsInput.parameters == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Missing parameters"
                };
            }
            
            if (!permissionsInput.parameters.ContainsKey("permissionNames"))
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Missing permissionNames parameter"
                };
            }
            string jsonString = permissionsInput.parameters["permissionNames"].ToString();
            string[]? permissionsNamesArray = JsonSerializer.Deserialize<string[]>(jsonString);
            
            using (var db = GetDataContext())
            {
                var permissions = await Permissions.GetUserPermissions(db, userId, permissionsNamesArray);
            
                return new Response<object?>()
                {
                    Succeeded = true,
                    Data = permissions
                };
            };
        }
        
        public async Task<Response<string[]>> UserPermissions(Guid userId, string? permissionNames = null)
        {
            using (var db = GetDataContext())
            {
                string[] permissionsArray = Array.Empty<string>();
                if (permissionNames != null)
                {
                    permissionsArray = permissionNames.Split(",").Select(x => x.Trim()).ToArray();
                }
            
                var permissions = await Permissions.GetUserPermissions(db, userId, permissionsArray);
            
                return new Response<string[]>()
                {
                    Succeeded = true,
                    Data = permissions
                };
            };
        }

        
        public async Task<string[]> UserPermissions(Guid userId, string[] permissions)
        {
            using (var db = GetDataContext())
            {
                // Process in batches of 500
                int batchSize = 500;
                int batchCount = (int)Math.Ceiling((double)permissions.Length / batchSize);
                List<string> userPermissions = new List<string>();
                for (int i = 0; i < batchCount; i++)
                {
                    string[] batchPermissions = permissions.Skip(i * batchSize).Take(batchSize).ToArray();
                    var userPermissionsBatch = await Permissions.GetUserPermissions(db, userId, batchPermissions);
                    userPermissions.AddRange(userPermissionsBatch);
                }
                return userPermissions.ToArray();
            };
        }
        

        public async Task<Response<List<Guid>?>> UserTeams(Guid userId, Guid[]? teamIds = null)
        {
            var teamUser = Cache.Instance.GetTableMetadata("TeamUser");
        
            DynamicLinq<BaseDbContext> userTeams = new DynamicLinq<BaseDbContext>(_dataContext, teamUser.Type);
            IQueryable query = userTeams.Query;
        
            query = query.Where($"UserId == @0", userId);
            if (teamIds is { Length: > 0 })
            {
                // Team id or filter
                string teamIdsOr = string.Join(" || ", teamIds.Select(x => $"TeamId == \"{x}\""));
                query = query.Where(teamIdsOr);
            }
        
            var results = query.ToDynamicList();
        
            return new Response<List<Guid>?>()
            {
                Succeeded = true,
                Data = results.Select(x => (Guid)x.TeamId).ToList()
            };
        }

        public Task<Response<object?>> Team(Guid teamId)
        {
            var team = Cache.Instance.GetTableMetadata("Team");
            DynamicLinq<BaseDbContext> dynamicLinq = new DynamicLinq<BaseDbContext>(_dataContext, team.Type);
            IQueryable query = dynamicLinq.Query;
            query = query.Where($"TeamId == @0", teamId);
            var results = query.ToDynamicList();
            return Task.FromResult(new Response<object?>()
            {
                Succeeded = true,
                Data = results.FirstOrDefault()
            });
        }

        /// <summary>
        /// Returns true if 2 users are reachable through the same team.
        /// </summary>
        /// <param name="user1Id"></param>
        /// <param name="user2Id"></param>
        /// <returns></returns>
        public async Task<Response<bool>> UsersExistInSameTeam(Guid user1Id, Guid user2Id)
        {
            var teamUser = Cache.Instance.GetTableMetadata("TeamUser");
        
            DynamicLinq<BaseDbContext> user1Teams = new DynamicLinq<BaseDbContext>(_dataContext, teamUser.Type);
            IQueryable user1Query = user1Teams.Query;
            user1Query = user1Query.Where($"UserId == @0", user1Id);
        
            DynamicLinq<BaseDbContext> user2Teams = new DynamicLinq<BaseDbContext>(_dataContext, teamUser.Type);
            IQueryable user2Query = user2Teams.Query;
            user2Query = user2Query.Where($"UserId == @0", user2Id);
        
            user1Query.Join(user2Query, "TeamId", "TeamId", "outer");
        
            bool userExistsInTeam = user1Query.Any();

            return new Response<bool>()
            {
                Succeeded = true,
                Data = userExistsInTeam
                // Data = await users.ToListAsync()
            };
        }
    }
}