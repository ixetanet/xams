using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;
using Xams.Core.Pipeline;
using Xams.Core.Repositories;
using Xams.Core.Utils;

namespace Xams.Core.Services
{
    public class DataService<TDbContext> : IDataService where TDbContext : BaseDbContext, new()
    {
        private readonly Guid ExecutionId = Guid.NewGuid();
        private readonly DataRepository _dataRepository = new(typeof(TDbContext));
        private readonly MetadataRepository _metadataRepository = new(typeof(TDbContext));
        private readonly SecurityRepository _securityRepository = new();
        private readonly Dictionary<string, HashSet<dynamic>> _deletes = new();
        private List<ServiceContext> ServiceContexts { get; set; } = new();
        internal ILogger Logger { get; private set; }

        
        public DataService(ILogger<DataService<TDbContext>> logger)
        {
            Logger = logger;
        }

        public ILogger GetLogger()
        {
            return Logger;
        }

        public Guid GetExecutionId()
        {
            return ExecutionId;
        }
        
        public DataRepository GetDataRepository()
        {
            return _dataRepository;
        }

        public MetadataRepository GetMetadataRepository()
        {
            return _metadataRepository;
        }

        public SecurityRepository GetSecurityRepository()
        {
            return _securityRepository;
        }
        
        /// <summary>
        /// Return false if the entity has already been tracked for deletion
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TrackDelete(string entity, object id)
        {
            if (!_deletes.ContainsKey(entity))
            {
                _deletes.Add(entity, new HashSet<dynamic>());
            }
            if (!_deletes[entity].Contains(id))
            {
                _deletes[entity].Add(id);
                return true;
            }

            return false;
        }

        public bool TrackingDelete(string entity, object id)
        {
            if (!(id is Guid guidId))
            {
                // For now, we only track Guid IDs
                return false;
            }
            
            return _deletes.ContainsKey(entity) && _deletes[entity].Contains(guidId);
        }

        public async Task<Response<ReadOutput>> Read(Guid userId, ReadInput readInput, PipelineContext? parent = null)
        {
            try
            {
                if (string.IsNullOrEmpty(readInput.tableName))
                {
                    return new Response<ReadOutput>()
                    {
                        Succeeded = false,
                        FriendlyMessage = "Table name is required."
                    };
                }

                var pipelineContext = new PipelineContext()
                {
                    Parent = parent,
                    UserId = userId,
                    TableName = readInput.tableName,
                    DataOperation = DataOperation.Read,
                    InputParameters = readInput.parameters ?? new Dictionary<string, JsonElement>(),
                    OutputParameters = new Dictionary<string, JsonElement>(),
                    SystemParameters = new SystemParameters(),
                    ReadInput = readInput,
                    DataService = this,
                    DataRepository = _dataRepository,
                    MetadataRepository = _metadataRepository,
                    SecurityRepository = _securityRepository
                };
                pipelineContext.CreateServiceContext();
                ServiceContexts.Add(pipelineContext.ServiceContext);

                var result = await ExecutePipeline(pipelineContext);

                await TryExecuteBulkServiceLogic(BulkStage.Post, userId);

                return new Response<ReadOutput>()
                {
                    Succeeded = result.Succeeded,
                    FriendlyMessage = result.FriendlyMessage,
                    LogMessage = result.LogMessage,
                    Data = result.Data as ReadOutput
                };
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Read Failed: {Message}", e.Message);
                return new Response<ReadOutput>()
                {
                    Succeeded = false,
                    FriendlyMessage = e.Message,
                    LogMessage = e.StackTrace
                };
            }
            finally
            {
                _dataRepository.Dispose();
            }
        }

        public async Task<Response<object?>> Create(Guid userId, BatchInput createInput)
        {
            return await BulkExecute(userId, new BulkInput()
            {
                Creates = createInput.entities ?? [createInput]
            }, createInput.entities != null);
        }

        public async Task<Response<object?>> Create<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null)
        {
            return await ServiceExecute(userId, entity, DataOperation.Create, parameters, parent);
        }

        public async Task<Response<object?>> Update(Guid userId, BatchInput updateInput)
        {
            return await BulkExecute(userId, new BulkInput()
            {
                Updates = updateInput.entities ?? [updateInput]
            }, updateInput.entities != null);
        }

        public async Task<Response<object?>> Update<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null)
        {
            return await ServiceExecute(userId, entity, DataOperation.Update, parameters, parent);
        }

        public async Task<Response<object?>> Delete(Guid userId, BatchInput deleteInput)
        {
            return await BulkExecute(userId, new BulkInput()
            {
                Deletes = deleteInput.entities ?? [deleteInput]
            }, deleteInput.entities != null);
        }

        public async Task<Response<object?>> Delete<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null)
        {
            return await ServiceExecute(userId, entity, DataOperation.Delete, parameters, parent);
        }

        public async Task<Response<object?>> Upsert(Guid userId, BatchInput input)
        {
            return await BulkExecute(userId, new BulkInput()
            {
                Upserts = input.entities ?? [input]
            }, input.entities != null);
        }

        public async Task<Response<object?>> Upsert<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null)
        {
            if (entity == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Entity is required."
                };
            }

            DataOperation dataOperation = DataOperation.Create;
            var id = entity.GetValue($"{entity.GetType().Name}Id");

            if (id != null)
            {
                var findResponse = await _dataRepository.Find(entity.GetType().Name, (Guid)id, true);
                if (findResponse is { Succeeded: true, Data.results.Count: > 0 })
                {
                    dataOperation = DataOperation.Update;
                }
            }

            return await ServiceExecute(userId, entity, dataOperation, parameters, parent);
        }

        public async Task<Response<object?>> Bulk(Guid userId, BulkInput input)
        {
            return await BulkExecute(userId, input, true);
        }

        public async Task<Response<object?>> Action(Guid userId, ActionInput input, HttpContext httpContext)
        {
            if (httpContext.Request.ContentType != null &&
                httpContext.Request.ContentType.Contains("multipart/form-data"))
            {
                IFormCollection formCollection = await httpContext.Request.ReadFormAsync();

                if (formCollection.ContainsKey("parameters") &&
                    !string.IsNullOrEmpty(formCollection["parameters"].ToString()))
                {
                    string jsonData = formCollection["parameters"].ToString();
                    input.parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonData);
                }
            }

            // Verify User has action permissions
            var permissions = await PermissionCache.GetUserPermissions(userId, [$"ACTION_{input.name}"]); 
            if (permissions.Length == 0)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Missing {input.name} permissions."
                };
            }

            if (!Cache.Instance.ServiceActions.TryGetValue(input.name, out var action))
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Could not find action {input.name}."
                };
            }

            Type actionType = action.Type;

            var instance = Activator.CreateInstance(actionType);
            var executeMethod = actionType.GetMethod("Execute");
            if (executeMethod == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = $"Could not find Execute method for action {input.name}."
                };
            }

            IFormFile? file = null;
            if (input is FileInput fileInput)
            {
                file = fileInput.file;
            }

            try
            {
                var pipelineContext = new PipelineContext()
                {
                    UserId = userId,
                    DataOperation = DataOperation.Read,
                    InputParameters = input.parameters ?? new Dictionary<string, JsonElement>(),
                    OutputParameters = new Dictionary<string, JsonElement>(),
                    SystemParameters = new SystemParameters(),
                    DataService = this,
                    DataRepository = _dataRepository,
                    MetadataRepository = _metadataRepository,
                    SecurityRepository = _securityRepository,
                    File = file
                };

                // Start the transaction
                await _dataRepository.BeginTransaction();

                Response<object?> response = await ((Task<Response<object?>>)executeMethod.Invoke(instance,
                [
                    new ActionServiceContext(pipelineContext)
                ])!);

                // If there were create \ update \ delete pipelines executed, then execute bulk service logic
                Response<object?> bulkServiceLogicResponse = await TryExecuteBulkServiceLogic(BulkStage.Post, userId);

                // If everything succeeded, commit\end the transaction
                if (response.Succeeded && bulkServiceLogicResponse.Succeeded)
                {
                    await _dataRepository.CommitTransaction();
                }
                else
                {
                    await _dataRepository.RollbackTransaction();
                }

                return response;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Action Failed: {Message}", e.Message);
                await _dataRepository.RollbackTransaction();
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = e.Message,
                    LogMessage = e.StackTrace
                };
            }
            finally
            {
                _dataRepository.Dispose();
            }
        }

        public Task<Response<object?>> Metadata(MetadataInput metadataInput, Guid userId)
        {
            return _metadataRepository.Metadata(metadataInput, userId);
        }

        public Task<Response<object?>> Permissions(PermissionsInput permissionsInput, Guid userId)
        {
            return _securityRepository.Get(permissionsInput, userId);
        }

        public T GetDbContext<T>() where T : BaseDbContext
        {
            return _dataRepository.GetDbContext<T>();
        }

        /// <summary>
        /// Execute bulk service logic only if create \ update \ delete pipeline has been triggered
        /// </summary>
        /// <param name="bulkStage"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Response<object?>> TryExecuteBulkServiceLogic(BulkStage bulkStage, Guid userId)
        {
            if (ServiceContexts.Count > 0)
            {
                return await ExecuteBulkServiceLogic(bulkStage, userId);
            }

            return ServiceResult.Success();
        }

        /// <summary>
        /// Execute bulk Create Update and Delete
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="input"></param>
        /// <param name="returnArray">If true, returns the results as an array versus a single entity</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        private async Task<Response<object?>> BulkExecute(Guid userId, BulkInput input, bool returnArray)
        {
            try
            {
                List<Input> all = new List<Input>();
                List<Input> creates = new List<Input>();
                List<Input> updates = new List<Input>();
                List<Input> deletes = new List<Input>();
                if (input.Upserts != null)
                {
                    // If there's no id field, it's a creation
                    creates.AddRange(input.Upserts.Where(x =>
                        x.fields != null && !x.fields.ContainsKey($"{x.tableName}Id")));
                    // Get upserts with valid guids
                    var potentialUpdates = input.Upserts
                        .Where(x => x.fields != null
                                    && x.fields.ContainsKey($"{x.tableName}Id")
                                    && Guid.TryParse(x.fields[$"{x.tableName}Id"].ToString(), out Guid _))
                        .Select(x => new
                        {
                            Id = Guid.Parse(x.fields?[$"{x.tableName}Id"].ToString()),
                            TableName = x.tableName ?? throw new InvalidOperationException("Table name is required."),
                            Input = x
                        }).ToList();

                    var distinctTables = potentialUpdates.Select(x => x.TableName).Distinct();

                    List<object> entities = new List<object>();
                    foreach (var table in distinctTables)
                    {
                        var ids = potentialUpdates
                            .Where(x => x.TableName == table)
                            .Select(x => (Guid)x.Id).ToArray();
                        var readResponse = await _dataRepository.Find(table, ids, true, [$"{table}Id"]);
                        if (readResponse.Data == null)
                        {
                            throw new Exception($"Failed to retrieve {table} records for upsert.");
                        }

                        entities.AddRange(readResponse.Data.results);
                    }

                    foreach (var potentialUpdate in potentialUpdates)
                    {
                        var entity = entities.FirstOrDefault(x =>
                            x.GetValue<Guid>($"root_{potentialUpdate.TableName}Id") == potentialUpdate.Id);
                        if (entity == null)
                        {
                            creates.Add(potentialUpdate.Input);
                        }

                        updates.Add(potentialUpdate.Input);
                    }
                }

                creates.AddRange(input.Creates ?? []);
                updates.AddRange(input.Updates ?? []);
                deletes.AddRange(input.Deletes ?? []);

                all.AddRange(creates);
                all.AddRange(updates);
                all.AddRange(deletes);

                // Validate all inputs
                foreach (var op in all)
                {
                    var validationResponse = Validate(op);
                    if (!validationResponse.Succeeded)
                    {
                        return validationResponse;
                    }
                }

                List<OperationInfo> operations = new List<OperationInfo>();
                operations.AddRange(creates.Select(x => new OperationInfo()
                {
                    TableName = x.tableName ?? "",
                    DataOperation = DataOperation.Create,
                    Entity = EntityUtil.GetEntity(x, DataOperation.Create, out string error).Data ?? throw new Exception(error),
                    Input = x
                }));

                operations.AddRange(updates.Select(x => new OperationInfo()
                {
                    TableName = x.tableName ?? "",
                    DataOperation = DataOperation.Update,
                    Entity = EntityUtil.GetEntity(x, DataOperation.Update, out string error).Data ?? throw new Exception(error),
                    Input = x
                }));

                operations.AddRange(deletes.Select(x => new OperationInfo()
                {
                    TableName = x.tableName ?? "",
                    DataOperation = DataOperation.Delete,
                    Entity = EntityUtil.GetEntity(x, DataOperation.Delete, out string error).Data ?? throw new Exception(error),
                    Input = x
                }));

                // Retrieve all permissions for each table because other operations may occur in service logic
                // And we want to prevent the system from retrieving the same permissions multiple times
                var requiredPermissions = all.Select(x => x.tableName).Distinct()
                    .Select(x =>
                    {
                        return new[]
                        {
                            $"TABLE_{x}_CREATE_USER",
                            $"TABLE_{x}_CREATE_TEAM",
                            $"TABLE_{x}_CREATE_SYSTEM",
                            $"TABLE_{x}_UPDATE_USER",
                            $"TABLE_{x}_UPDATE_TEAM",
                            $"TABLE_{x}_UPDATE_SYSTEM",
                            $"TABLE_{x}_DELETE_USER",
                            $"TABLE_{x}_DELETE_TEAM",
                            $"TABLE_{x}_DELETE_SYSTEM",
                            $"TABLE_{x}_ASSIGN_USER",
                            $"TABLE_{x}_ASSIGN_TEAM",
                            $"TABLE_{x}_ASSIGN_SYSTEM",
                        };
                    }).SelectMany(x => x).Distinct();

                var permissions = await PermissionCache.GetUserPermissions(userId, requiredPermissions.ToArray()); 

                // This acts as an immediate guard for unauthorized operations, it prevents the system from
                // retrieving a bunch of PreEntity records only to find out the user doesn't have the permissions.
                // Access via record ownership is performed below
                // Verify the user has all the required delete\update permissions before batch retrieving the PreEntities
                bool HasUpdatePermission(string tableName) => permissions.Contains($"TABLE_{tableName}_UPDATE_USER") ||
                                                              permissions.Contains($"TABLE_{tableName}_UPDATE_TEAM") ||
                                                              permissions.Contains($"TABLE_{tableName}_UPDATE_SYSTEM");

                bool HasDeletePermission(string tableName) => permissions.Contains($"TABLE_{tableName}_DELETE_USER") ||
                                                              permissions.Contains($"TABLE_{tableName}_DELETE_TEAM") ||
                                                              permissions.Contains($"TABLE_{tableName}_DELETE_SYSTEM");

                bool HasCreatePermission(string tableName) => permissions.Contains($"TABLE_{tableName}_CREATE_USER") ||
                                                              permissions.Contains($"TABLE_{tableName}_CREATE_TEAM") ||
                                                              permissions.Contains($"TABLE_{tableName}_CREATE_SYSTEM");

                var missingUpdatePermissions = operations
                    .Where(x => x.DataOperation is DataOperation.Update)
                    .Where(x => !HasUpdatePermission(x.TableName))
                    .Select(x => x.TableName).Distinct()
                    .ToList();

                var missingDeletePermissions = operations
                    .Where(x => x.DataOperation is DataOperation.Delete)
                    .Where(x => !HasDeletePermission(x.TableName))
                    .Select(x => x.TableName).Distinct()
                    .ToList();

                var missingCreatePermissions = operations
                    .Where(x => x.DataOperation is DataOperation.Create)
                    .Where(x => !HasCreatePermission(x.TableName))
                    .Select(x => x.TableName).Distinct()
                    .ToList();

                if (missingUpdatePermissions.Any())
                {
                    return ServiceResult.Error(
                        $"Missing update permissions for {string.Join(", ", missingUpdatePermissions)}");
                }

                if (missingDeletePermissions.Any())
                {
                    return ServiceResult.Error(
                        $"Missing delete permissions for {string.Join(", ", missingDeletePermissions)}");
                }

                if (missingCreatePermissions.Any())
                {
                    return ServiceResult.Error(
                        $"Missing create permissions for {string.Join(", ", missingCreatePermissions)}");
                }

                List<PipelineContext> pipelineContexts = new List<PipelineContext>();

                foreach (var operation in operations)
                {
                    var pipelineContext = new PipelineContext()
                    {
                        UserId = userId,
                        TableName = operation.TableName,
                        Entity = operation.Entity,
                        InputParameters = operation.Input.parameters ?? new Dictionary<string, JsonElement>(),
                        OutputParameters = new Dictionary<string, JsonElement>(),
                        SystemParameters = new SystemParameters()
                        {
                            // If this is a batch operation retrieve all after everything has processed
                            ReturnEmpty = returnArray,
                            // Prevent save if the table has no service logic to save all in bulk
                            PreventSave = !Cache.Instance.GetTableMetadata(operation.TableName).HasPostOpServiceLogic &&
                                          returnArray
                        },
                        DataOperation = operation.DataOperation,
                        DataService = this,
                        DataRepository = _dataRepository,
                        MetadataRepository = _metadataRepository,
                        SecurityRepository = _securityRepository,
                        Fields = operation.Input.fields
                    };
                    ServiceContexts.Add(pipelineContext.CreateServiceContext());
                    pipelineContexts.Add(pipelineContext);
                }

                // Batch retrieve PreEntities and run security validation
                // Pre-Entity is always required for Update and Delete to perform security checks
                var securityResponse = await BatchPreEntity(pipelineContexts);
                if (!securityResponse.Succeeded)
                {
                    return securityResponse;
                }

                await _dataRepository.BeginTransaction();

                var preBulkServiceLogicResponse =
                    await ExecuteBulkServiceLogic(BulkStage.Pre, userId);
                if (!preBulkServiceLogicResponse.Succeeded)
                {
                    return preBulkServiceLogicResponse;
                }

                // Process entities without service logic first, so we can call save in bulk
                pipelineContexts = pipelineContexts
                    .OrderByDescending(x => x.SystemParameters.PreventSave).ToList();

                List<object> results = new List<object>();

                int toSaveCount = 0;
                foreach (var pipelineContext in pipelineContexts)
                {
                    if (toSaveCount > 0 && !pipelineContext.SystemParameters.PreventSave)
                    {
                        // If this entity has a PostOperation service logic, save all pending changes
                        await _dataRepository.SaveChangesAsync();
                    }


                    var response = await ExecutePipeline(pipelineContext);

                    if (pipelineContext.SystemParameters.PreventSave)
                    {
                        toSaveCount++;
                    }

                    if (!response.Succeeded)
                    {
                        await _dataRepository.RollbackTransaction();
                        return response;
                    }

                    if (response.Data != null) results.Add(response.Data);
                }

                if (toSaveCount > 0)
                {
                    await _dataRepository.SaveChangesAsync();
                }

                var postBulkServiceLogicResponse =
                    await ExecuteBulkServiceLogic(BulkStage.Post, userId);
                if (!postBulkServiceLogicResponse.Succeeded)
                {
                    return postBulkServiceLogicResponse;
                }

                // If this was a batch operation, the results need to be retrieved from the database
                if (returnArray)
                {
                    var distinctTables = pipelineContexts.Select(x => x.TableName).Distinct();
                    foreach (var tableName in distinctTables)
                    {
                        Guid[] ids = pipelineContexts
                            .Where(x => x.TableName == tableName)
                            .Where(x => x.DataOperation is DataOperation.Create or DataOperation.Update)
                            .Select(x => x.Entity!.GetValue<Guid>($"{x.TableName}Id")).ToArray();

                        var readResponse = await _dataRepository.Find(tableName, ids, false, null, true);
                        if (!readResponse.Succeeded || readResponse.Data == null)
                        {
                            await _dataRepository.RollbackTransaction();
                            return new Response<object?>()
                            {
                                Succeeded = false,
                                FriendlyMessage = $"Failed to retrieve {tableName} records."
                            };
                        }

                        results.AddRange(readResponse.Data.results);
                    }
                }

                await _dataRepository.CommitTransaction();

                return new Response<object?>()
                {
                    Succeeded = true,
                    Data = returnArray ? results : results.FirstOrDefault()
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Operation Failed: {Message}", ex.Message);
                await _dataRepository.RollbackTransaction();
                throw;
            }
            finally
            {
                _dataRepository.Dispose();
            }
        }

        private async Task<Response<object?>> ServiceExecute<T>(Guid userId, T entity, DataOperation dataOperation,
            object? parameters = null, PipelineContext? parent = null)
        {
            if (entity == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Entity is required."
                };
            }
            
            
            // It's possible that hierarchy of deletes is occuring from a PostTraversalDelete and as part of the
            // ServiceLogic, a deleted is called on an entity that has been or will be deleted. In this case, we need
            // to prevent the delete from happening again.
            Type entityType = entity.GetType();
            var entityId = entity.GetIdValue(entityType);
            if (dataOperation is DataOperation.Delete)
            {
                
                if (!TrackDelete(entityType.Name, entityId))
                {
                    return new Response<object?>()
                    {
                        Succeeded = true
                    };
                }
            }
            
            // If attempting to update a record that's been deleted return success
            if (dataOperation is DataOperation.Update)
            {
                if (TrackingDelete(entityType.Name, entityId))
                {
                    return new Response<object?>()
                    {
                        Succeeded = true
                    };
                }
            }
            
            var pipelineContext = new PipelineContext()
            {
                Parent = parent,
                UserId = userId,
                Entity = entity,
                PreEntity = dataOperation is DataOperation.Delete or DataOperation.Update
                    ? await DynamicLinq<BaseDbContext>.Find(_dataRepository.CreateNewDbContext(), entityType,
                        (Guid)entity.GetIdValue(entityType))
                    : null,
                TableName = EntityUtil.GetTableName(entity.GetType(), EntityUtil.DbContext?.GetType() ?? throw new Exception("DbContext not yet initialized")).TableName,
                DataOperation = dataOperation,
                InputParameters = (parameters != null ? Util.ObjectToParameters(parameters) : null) ??
                                  new Dictionary<string, JsonElement>(),
                OutputParameters = new Dictionary<string, JsonElement>(),
                SystemParameters = new SystemParameters()
                {
                    ReturnEntity = true
                },
                DataService = this,
                DataRepository = _dataRepository,
                MetadataRepository = _metadataRepository,
                SecurityRepository = _securityRepository
            };
            ServiceContexts.Add(pipelineContext.CreateServiceContext());
            var securityResponse = await Pipelines.SecurityPipeline.Execute(pipelineContext);
            if (!securityResponse.Succeeded)
            {
                throw new Exception(securityResponse.FriendlyMessage);
            }

            var response = await ExecutePipeline(pipelineContext);
            if (!response.Succeeded)
            {
                // This was likely called from within a Service Logic, so we need to throw an exception if it fails
                throw new Exception(response.FriendlyMessage);
            }

            return response;
        }

        private async Task<Response<object?>> ExecuteBulkServiceLogic(
            BulkStage bulkStage, Guid userId)
        {
            // Call Bulk Service Logic
            var preBulkServiceLogics = Cache.Instance.BulkServiceLogics
                .Where(x => x.BulkServiceAttribute.Stage == bulkStage).ToList();

            foreach (var preBulkServiceLogic in preBulkServiceLogics)
            {
                try
                {
                    var instance = Activator.CreateInstance(preBulkServiceLogic.Type);
                    var executeMethod = preBulkServiceLogic.Type.GetMethod("Execute");
                    if (executeMethod != null)
                    {
                        var pipelineContext = new PipelineContext()
                        {
                            UserId = userId,
                            DataService = this,
                            DataRepository = _dataRepository,
                            MetadataRepository = _metadataRepository,
                            SecurityRepository = _securityRepository
                        };

                        BulkServiceContext bulkServiceContext =
                            new BulkServiceContext(pipelineContext, ServiceContexts);

                        Response<object?> response = await ((Task<Response<object?>>)executeMethod.Invoke(instance,
                        [
                            bulkServiceContext
                        ])!);

                        if (!response.Succeeded)
                        {
                            return response;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Bulk ServiceLogic Failed: {Message}", e.Message);
                    return ServiceResult.Error(e.Message);
                }
            }

            return ServiceResult.Success();
        }

        private async Task<Response<object?>> ExecutePipeline(PipelineContext pipelineContext)
        {
            // Save might be prevented in case we want to proxy the create\read\update\delete to another service
            pipelineContext.IsProxy = Cache.Instance.GetTableMetadata(pipelineContext.TableName).IsProxy;

            pipelineContext.DataService = this;
            pipelineContext.DataRepository = _dataRepository;
            pipelineContext.SecurityRepository = _securityRepository;
            pipelineContext.MetadataRepository = _metadataRepository;

            if (pipelineContext.DataOperation is DataOperation.Read)
            {
                return pipelineContext.IsProxy
                    ? await Pipelines.ReadProxy.Execute(pipelineContext)
                    : await Pipelines.Read.Execute(pipelineContext);
            }

            if (pipelineContext.DataOperation is DataOperation.Create)
            {
                return pipelineContext.IsProxy
                    ? await Pipelines.CreateProxy.Execute(pipelineContext)
                    : await Pipelines.Create.Execute(pipelineContext);
            }

            if (pipelineContext.DataOperation is DataOperation.Update)
            {
                return pipelineContext.IsProxy
                    ? await Pipelines.UpdateProxy.Execute(pipelineContext)
                    : await Pipelines.Update.Execute(pipelineContext);
            }

            if (pipelineContext.DataOperation is DataOperation.Delete)
            {
                return pipelineContext.IsProxy
                    ? await Pipelines.DeleteProxy.Execute(pipelineContext)
                    : await Pipelines.Delete.Execute(pipelineContext);
            }

            return new Response<object?>()
            {
                Succeeded = true
            };
        }

        /// <summary>
        /// Get the Pre-Entity in batches and verify the user has permission to perform the operation
        /// </summary>
        /// <param name="pipelineContexts">List of Pipeline Contexts</param>
        /// <param name="checkSecurity">Execute the Security Pipeline for all Pipeline Contexts</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Response<object?>> BatchPreEntity(List<PipelineContext> pipelineContexts,
            bool checkSecurity = true)
        {
            if (pipelineContexts.Count == 0)
            {
                return new Response<object?>()
                {
                    Succeeded = true
                };
            }

            // Group the Pipeline Contexts by Table Name to isolate queries
            var updateDeletePContexts = pipelineContexts
                .Where(x => x.DataOperation is DataOperation.Update or DataOperation.Delete).ToList();
            var deleteTableGroups = (from pipelineContext in updateDeletePContexts
                group pipelineContext by pipelineContext.TableName
                into g
                select new TableOperationGroup()
                {
                    TableName = g.Key,
                    PipelineContexts = g.ToList()
                }).ToList();


            // Check security for PreEntities in batches
            // This is to prevent a user with unauthorized access from querying all PreEntities
            using var cts = new CancellationTokenSource();
            Response<object?>? cancelledResponse = null;
            await Parallel.ForEachAsync(deleteTableGroups, cts.Token, async (tableGroup, token) =>
            {
                var ids = tableGroup.PipelineContexts
                    .Select(x => x.Entity?.GetValue<object>(Cache.Instance.GetTableMetadata(x.TableName).PrimaryKey)
                                 ?? throw new Exception($"Failed to get Id from {x.TableName} entity.")).ToList();
            
                var tableType = Cache.Instance.GetTableMetadata(tableGroup.TableName).Type;
                
                Stopwatch sw = new  Stopwatch();
                
                var preEntities =
                    await DynamicLinq<BaseDbContext>.BatchRequestThreaded(() => _dataRepository.CreateNewDbContext(),
                        tableType,
                        ids);
                
                // Set each PreEntity on the pipeline context and run security validation
                foreach (var pipelineContext in tableGroup.PipelineContexts.Where(x => x.PreEntity == null))
                {
                    var metadata = Cache.Instance.GetTableMetadata(pipelineContext.TableName);
                    pipelineContext.PreEntity = preEntities[pipelineContext.Entity?.GetValue<dynamic>(metadata.PrimaryKey)];
                    if (!checkSecurity)
                    {
                        continue;
                    }
                        
                    var response = await Pipelines.SecurityPipeline.Execute(pipelineContext);
                    if (!response.Succeeded)
                    {
                        cancelledResponse = response;
                        await cts.CancelAsync();
                    }
                }
            });
            

            if (cancelledResponse != null)
            {
                return cancelledResponse;
            }

            // Run security on the remaining operations
            if (!checkSecurity)
            {
                return new Response<object?>()
                {
                    Succeeded = true
                };
            }

            var otherPipelineContexts = pipelineContexts
                .Where(x => x.DataOperation is not (DataOperation.Update or DataOperation.Delete)).ToList();
            foreach (var pipelineContext in otherPipelineContexts)
            {
                var response = await Pipelines.SecurityPipeline.Execute(pipelineContext);
                if (!response.Succeeded)
                {
                    return response;
                }
            }

            return new Response<object?>()
            {
                Succeeded = true
            };
        }


        private Response<object?> Validate(Input input)
        {
            if (input.fields == null)
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Fields are required."
                };
            }

            if (string.IsNullOrEmpty(input.tableName))
            {
                return new Response<object?>()
                {
                    Succeeded = false,
                    FriendlyMessage = "Table name is required."
                };
            }

            return new Response<object?>()
            {
                Succeeded = true
            };
        }
    }

    public class TableOperationGroup
    {
        public required string TableName { get; set; }
        public required List<PipelineContext> PipelineContexts { get; set; }
    }

    public class OperationInfo
    {
        // public required Guid Id { get; set; }
        public required DataOperation DataOperation { get; set; }
        public required string TableName { get; set; }
        public required object Entity { get; set; }
        public required Input Input { get; set; }
        public object? PreEntity { get; set; }
    }
}
