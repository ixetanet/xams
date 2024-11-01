using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Pipeline;
using Xams.Core.Repositories;

namespace Xams.Core.Interfaces
{
    public interface IDataService
    {
        public Guid GetExecutionId();
        public DataRepository GetDataRepository();
        public MetadataRepository GetMetadataRepository();
        public SecurityRepository GetSecurityRepository();
        Task<Response<ReadOutput>> Read(Guid userId, ReadInput input, PipelineContext? parent = null);
        Task<Response<object?>> Create(Guid userId, BatchInput createInput);

        Task<Response<object?>> Create<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null);

        Task<Response<object?>> Update(Guid userId, BatchInput input);

        Task<Response<object?>> Update<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null);

        Task<Response<object?>> Delete(Guid userId, BatchInput input);

        Task<Response<object?>> Delete<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null);

        Task<Response<object?>> Upsert(Guid userId, BatchInput input);

        Task<Response<object?>> Upsert<T>(Guid userId, T entity, object? parameters = null,
            PipelineContext? parent = null);

        Task<Response<object?>> Bulk(Guid userId, BulkInput input);
        Task<Response<object?>> Action(Guid userId, ActionInput input, HttpContext httpContext);
        Task<Response<object?>> Metadata(MetadataInput metadataInput, Guid userId);
        Task<Response<object?>> Permissions(PermissionsInput permissionsInput, Guid userId);
        public T GetDbContext<T>() where T : BaseDbContext;
        internal Task<Response<object?>> TryExecuteBulkServiceLogic(BulkStage bulkStage, Guid userId);

        internal Task<Response<object?>> BatchPreEntitySecurity(List<PipelineContext> pipelineContexts,
            bool checkSecurity);

        internal bool TrackDelete(string entity, Guid id);
        
        internal bool TrackingDelete(string entity, Guid id);
        
        internal ILogger GetLogger();
    }
}