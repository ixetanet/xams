using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xams.Core.Contexts;
using Xams.Core.Interfaces;

namespace Xams.Core;

public static class SignalRConfiguration
{
    public static Func<HttpContext, Task<Guid>>? GetUserId { get; set; }
}

public class SignalRHub : Hub
{
    public static readonly ConcurrentDictionary<Guid, UserConnection> UserConnections = new();
    private ILogger<SignalRHub> Logger { get; }
    private IServiceProvider ServiceProvider { get; }
    public SignalRHub(ILogger<SignalRHub> logger, IServiceProvider serviceProvider)
    {
        Logger = logger;
        ServiceProvider = serviceProvider;
    }
    public async Task<object?> OnReceive(string hubName, string message)
    {
        var userId = await GetCurrentUserId();
        var permissions = await PermissionCache.GetUserPermissions(userId, [$"HUB_{hubName}"]);

        if (permissions.Length == 0)
        {
            Logger.LogInformation("User {UserId} has no permissions for hub {HubName}", userId, hubName);
            return null;
        }

        try
        {
            var hub = GetHub(hubName);
            using var scope = ServiceProvider.CreateScope();
            var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
            var response = await dataService.ExecuteTransaction(userId, async (pipelineContext) =>
            {
                var response = await hub.OnReceive(new HubContext(pipelineContext, message, this));
                return response;
            });
            
        
            if (!response.Succeeded)
            {
                Logger.LogError("User {UserId} failed to receive message {Message}, {ResponseLogMessage}", userId, message, response.LogMessage);
            }

            return response.Data;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error processing message {Message} for user {UserId} on hub {HubName}", message, userId, hubName);
        }

        return null;

    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        var userId = await GetCurrentUserId();
        if (!UserConnections.TryAdd(userId, new UserConnection { NumberOfConnections = 1 }))
        {
            UserConnections[userId].NumberOfConnections++;
        }
        
        lock (UserConnections[userId].LockObj)
        {
            UserConnections[userId].ConnectionIds.Add(Context.ConnectionId);
            UserConnections[userId].ConnectionContexts.Add(Context);
            UserConnections[userId].ConnectionHubs.Add(Context.ConnectionId, []);    
        }
        
        var permissions = await PermissionCache.GetUserPermissions(userId);

        foreach (var permission in permissions)
        {
            if (!permission.StartsWith("HUB_"))
            {
                continue;
            }
            
            try
            {
                var hubName = permission.Substring(4, permission.Length - 4);
                lock (UserConnections[userId].LockObj)
                {
                    UserConnections[userId].ConnectionHubs[Context.ConnectionId].Add(hubName);    
                }
                var hub = GetHub(hubName);
                using var scope = ServiceProvider.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                var response = await dataService.ExecuteTransaction(userId, async (pipelineContext) =>
                {
                    var response = await hub.OnConnected(new HubContext(pipelineContext, "", this));
                    return response;
                });
                
                if (!response.Succeeded)
                {
                    Logger.LogError("User {UserId} failed to connect to hub {Permission}, {ResponseLogMessage}", userId, permission, response.LogMessage);
                    continue;
                }
                
                Logger.LogInformation("User {UserId} connected to hub {Permission}", userId, permission);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error processing user {UserId} connected to hub {Permission}", userId, permission);
            }
            
        }
        Logger.LogInformation("User {UserId} connected with connection ID {ContextConnectionId}", userId, Context.ConnectionId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        var userId = await GetCurrentUserId();
        var userHubs = new List<string>();
        if (UserConnections.TryGetValue(userId, out var userConnection))
        {
            lock (UserConnections[userId].LockObj)
            {
                userHubs = userConnection.ConnectionHubs[Context.ConnectionId].ToList();
                userConnection.ConnectionIds.Remove(Context.ConnectionId);
                userConnection.ConnectionContexts.RemoveAll(ctx => ctx.ConnectionId == Context.ConnectionId);
                userConnection.ConnectionHubs.Remove(Context.ConnectionId);
                if (userConnection.NumberOfConnections <= 1)
                {
                    UserConnections.TryRemove(userId, out _);
                }
                else
                {
                    UserConnections[userId].NumberOfConnections--;
                }
            }
            
        }
        
        foreach (var hubName in userHubs)
        {
            try
            {
                var hub = GetHub(hubName);
                using var scope = ServiceProvider.CreateScope();
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                var response = await dataService.ExecuteTransaction(userId, async (pipelineContext) =>
                {
                    var response = await hub.OnDisconnected(new HubContext(pipelineContext, "", this));
                    return response;
                });
                
                if (!response.Succeeded)
                {
                    Logger.LogError("User {UserId} failed to disconnect from hub {HubName}, {ResponseLogMessage}", userId, hubName, response.LogMessage);
                    continue;
                }

                Logger.LogInformation("User {UserId} disconnected from hub {HubName}", userId, hubName);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error processing user {UserId} disconnected from hub {HubName}", userId, hubName);
            }
        }
        Logger.LogInformation("User {UserId} disconnected with connection ID {ContextConnectionId}", userId, Context.ConnectionId);
    }

    public IServiceHub GetHub(string hubName)
    {
        if (!Cache.Instance.ServiceHubs.TryGetValue(hubName, out var hubMetadata))
        {
            throw new Exception($"Service hub {hubName} not found");
        }
        var hub = Activator.CreateInstance(hubMetadata.Type);
        if (hub == null)
        {
            throw new Exception($"Hub {hubName} not found");
        }
        return (IServiceHub)hub;
    }
    

    private async Task<Guid> GetCurrentUserId()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext not available");
        }

        // Use the configured GetUserId function if available, otherwise fall back to default
        if (SignalRConfiguration.GetUserId != null)
        {
            return await SignalRConfiguration.GetUserId(httpContext);
        }

        // Fallback to default implementation (header-based)
        return await GetUserIdFromQueryString(httpContext);
    }

    private static Task<Guid> GetUserIdFromQueryString(HttpContext httpContext)
    {
        if (httpContext.Request.Query.ContainsKey("userid"))
        {
            string userId = httpContext.Request.Query["userid"].ToString();
            if (Guid.TryParse(userId, out Guid guid))
            {
                return Task.FromResult(guid);
            }
            throw new Exception("UserId in header is not a Guid");
        }
        
        throw new Exception("UserId not found in request headers");
    }

    public class UserConnection
    {
        public int NumberOfConnections { get; set; }
        // public HashSet<string> Hubs { get; set; } = new();
        public HashSet<string> ConnectionIds { get; set; } = new();
        public Dictionary<string, List<string>> ConnectionHubs { get; set; } = new();
        public List<HubCallerContext> ConnectionContexts { get; set; } = new();
        public object LockObj = new();
    }

    public static Task ForceDisconnectUser(Guid userId, string reason)
    {
        if (UserConnections.TryGetValue(userId, out var userConnection))
        {
            var contexts = userConnection.ConnectionContexts.ToList();
            foreach (var context in contexts)
            {
                try
                {
                    // Send a notification before disconnecting (more graceful)
                    // Note: We can't use Clients here since this is a static method
                    // So we'll still use Abort() but the client now has better reconnection logic
                    context.Abort();
                }
                catch (Exception ex)
                {
                    // Logger.LogError($"Failed to abort connection {context.ConnectionId}: {ex.Message}");
                }
            }
        }

        return Task.CompletedTask;
    }
}