using System.Text.Json;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;
using Xams.Core.Pipeline;
using Xams.Core.Utils;

namespace Xams.Core.Contexts
{
    public class ServiceContext : BaseServiceContext
    {
        public ServiceContext TopParent => GetRoot(this);
        public ServiceContext? Parent => PipelineContext.Parent?.ServiceContext;
        public int Depth => GetDepth(this, 1);
        public string TableName => PipelineContext.TableName;
        public LogicStage LogicStage { get; internal set; }
        public DataOperation DataOperation { get; private set; }
        public object? Entity => DataOperation == DataOperation.Delete ? PipelineContext.PreEntity : PipelineContext.Entity;
        public object? PreEntity => PipelineContext.PreEntity;
        public List<object> Entities { get; internal set; }
        public ReadInput? ReadInput { get => PipelineContext.ReadInput; set => PipelineContext.ReadInput = value; }
        public ReadOutput? ReadOutput { get; internal set; }

        public ServiceContext(
            PipelineContext pipelineContext,
            LogicStage logicStage,
            DataOperation dataOperation,
            List<object>? entities,
            ReadOutput? readOutput) : base(pipelineContext)
        {
            LogicStage = logicStage;
            DataOperation = dataOperation;
            Entities = entities ?? new List<object>();
            ReadOutput = readOutput;
        }

        public bool ValueChanged(string fieldName)
        {
            return EntityUtil.ValueChanged(Entity, PreEntity, fieldName);
        }

        public object GetId()
        {
            if (Entity == null) throw new Exception("Entity is null. Cannot get Id.");
            return Entity.GetId();
        }

        public T GetEntity<T>() where T : class
        {
            return Entity as T ?? throw new Exception($"Entity is null. Cannot cast to type {typeof(T).Name}.");
        }

        public T GetPreEntity<T>() where T : class
        {
            if (PreEntity == null) throw new Exception($"PreEntity is null. Cannot cast to type {typeof(T).Name}.");
            // Always return a copy to ensure the properties cannot be modified for later service logic
            var copy = JsonSerializer.Deserialize(JsonSerializer.Serialize(PreEntity), PreEntity.GetType());
            if (copy == null) throw new Exception($"Failed to create PreEntity {typeof(T).Name}.");
            return (T)copy;
        }

        private int GetDepth(ServiceContext serviceContext, int depth)
        {
            if (serviceContext.Parent == null) return depth;
            return GetDepth(serviceContext.Parent, depth + 1);
        }
        
        private ServiceContext GetRoot(ServiceContext serviceContext)
        {
            if (serviceContext.Parent == null) return serviceContext;
            return GetRoot(serviceContext.Parent);
        }
    }
}
