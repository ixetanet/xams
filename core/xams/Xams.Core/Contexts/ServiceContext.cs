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
            if (Entity == null) throw new Exception("Entity is null. Cannot check if value changed.");
            var entity = Entity;

            if (PreEntity == null) return true;

            var propertyInfo = entity.GetType().GetProperty(fieldName);
            if (propertyInfo != null)
            {
                var currentValue = propertyInfo.GetValue(entity);
                var originalValue = propertyInfo.GetValue(PreEntity);

                if (originalValue == null && currentValue != null) return true;
                if (originalValue != null && currentValue == null) return true;

                // Check if the property type is GUID
                if (propertyInfo.PropertyType == typeof(Guid) && currentValue != null && originalValue != null)
                {
                    // Compare as Guid
                    return (Guid)currentValue != (Guid)originalValue;
                }

                return !Equals(currentValue, originalValue);
            }

            return false;
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
