using System.Text.Json;
using Microsoft.Extensions.Logging;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;
using Xams.Core.Repositories;

namespace Xams.Core.Base;

public class BaseServiceContext(PipelineContext pipelineContext)
{
    internal PipelineContext PipelineContext { get; private set; } = pipelineContext;
    internal IDataService DataService => PipelineContext.DataService;
    internal DataRepository DataRepository => PipelineContext.DataRepository;
    internal MetadataRepository MetadataRepository => PipelineContext.MetadataRepository;
    internal SecurityRepository SecurityRepository => PipelineContext.SecurityRepository;
    public Guid ExecutionId => PipelineContext.DataService.GetExecutionId();
    public Guid ExecutingUserId => PipelineContext.UserId;
    public Dictionary<string, JsonElement> Parameters => PipelineContext.InputParameters;
    public ILogger Logger => PipelineContext.DataService.GetLogger();

    /// <summary>
    /// Create entity record and execute service logic.
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <typeparam name="T">Entity class</typeparam>
    /// <returns></returns>
    public async Task<T> Create<T>(object entity, object? parameters = null)
    {
        return await Create<T>(ExecutingUserId, entity, parameters);
    }
    
    /// <summary>
    /// Create entity record as a specific user and execute service logic.
    /// </summary>
    /// <param name="executingUserId">User to execute as</param>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <typeparam name="T">Entity class</typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<T> Create<T>(Guid executingUserId, object entity, object? parameters = null)
    {
        var response = await PipelineContext.DataService.Create(executingUserId, entity, parameters, PipelineContext);
        if (!response.Succeeded)
        {
            throw new Exception(response.FriendlyMessage);
        }

        if (response.Data == null)
        {
            throw new Exception($"Failed to create {entity.GetType().Name}.");
        }

        return (T)response.Data;
    }
    
    /// <summary>
    /// Update entity record and execute service logic.
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <typeparam name="T">Entity class</typeparam>
    /// <returns></returns>
    public async Task<T> Update<T>(object entity, object? parameters = null)
    {
        return await Update<T>(ExecutingUserId, entity, parameters);
    }

    /// <summary>
    /// Update entity record as a specific user and execute service logic.
    /// </summary>
    /// <param name="executingUserId">User to execute as</param>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <typeparam name="T">Entity class</typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<T> Update<T>(Guid executingUserId, object entity, object? parameters = null)
    {
        var response = await DataService.Update(executingUserId, entity, parameters, PipelineContext);
        if (!response.Succeeded)
        {
            throw new Exception(response.FriendlyMessage);
        }

        if (response.Data == null)
        {
            throw new Exception($"Failed to update {entity.GetType().Name}.");
        }

        return (T)response.Data;
    }
    
    /// <summary>
    /// Update record if it exists, Create if it doesn't and execute service logic.
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <typeparam name="T">Entity class</typeparam>
    /// <returns></returns>
    public async Task<T> Upsert<T>(object entity, object? parameters = null)
    {
        return await Upsert<T>(ExecutingUserId, entity, parameters);
    }

    /// <summary>
    /// As a specific user, Update record if it exists, Create if it doesn't and execute service logic. 
    /// </summary>
    /// <param name="executingUserId">User to execute as</param>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <typeparam name="T">Entity class</typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<T> Upsert<T>(Guid executingUserId, object entity, object? parameters = null)
    {
        var response = await DataService.Upsert(executingUserId, entity, parameters, PipelineContext);
        if (!response.Succeeded)
        {
            throw new Exception(response.FriendlyMessage);
        }

        if (response.Data == null)
        {
            throw new Exception($"Failed to update {entity.GetType().Name}.");
        }

        return (T)response.Data;
    }
    
    /// <summary>
    /// Delete record and execute service logic.
    /// </summary>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    public async Task Delete(object entity, object? parameters = null)
    {
        await Delete(ExecutingUserId, entity, parameters);
    }

    /// <summary>
    /// Delete record as a specific user and execute service logic.
    /// </summary>
    /// <param name="executingUserId">User to execute as</param>
    /// <param name="entity">Entity</param>
    /// <param name="parameters">Any type (can be anonymous)</param>
    /// <exception cref="Exception"></exception>
    public async Task Delete(Guid executingUserId, object entity, object? parameters = null)
    {
        var response = await DataService.Delete(executingUserId, entity, parameters, PipelineContext);
        if (!response.Succeeded)
        {
            throw new Exception(response.FriendlyMessage);
        }
    }
    
    /// <summary>
    /// Get the DbContext for this transaction.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetDbContext<T>() where T : BaseDbContext
    {
        return DataRepository.GetDbContext<T>();
    }
    
    /// <summary>
    /// Create a new DbContext outside of transaction. This is useful for querying data outside of the current
    /// transaction. For example, querying records that may have been deleted in the current transaction but still
    /// exist outside of it.
    /// </summary>
    /// <typeparam name="T">DbContext Type</typeparam>
    /// <returns></returns>
    public T CreateNewDbContext<T>() where T : BaseDbContext
    {
        return DataRepository.CreateNewDbContext<T>();
    }
    
    /// <summary>
    /// Deserialize parameters to a specific type.
    /// </summary>
    /// <typeparam name="T">Any type</typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public T GetParameters<T>() where T : class
    {
        string json = JsonSerializer.Serialize(Parameters);
        return JsonSerializer.Deserialize<T>(json) ?? throw new Exception($"Parameters is null. Cannot cast to type {typeof(T).Name}.");
    }

    /// <summary>
    /// Return an array of matching permissions the user has.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="permissions"></param>
    /// <returns></returns>
    public async Task<string[]> Permissions(Guid userId, string[] permissions)
    {
        if (!permissions.Any())
        {
            return [];
        }

        return await PermissionCache.GetUserPermissions(userId, permissions);
    }
}