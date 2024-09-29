namespace Xams.Core.Attributes
{
    [Flags]
    public enum DataOperation
    {
        Create = 1,
        Read = 2,
        Update = 4,
        Delete = 8,
        Action = 16,
    }
    
    [Flags]
    public enum LogicStage
    {
        PreOperation = 2,
        PostOperation = 4,
    }

    public class ServiceLogicAttribute : Attribute
    {
        public string TableName { get; set; }
        public DataOperation DataOperation { get; set; }
        public LogicStage LogicStage { get; set; }
        public int Order { get; set; }
    
    
        public ServiceLogicAttribute(string tableName, DataOperation dataOperation, LogicStage logicStage, int order = 0)
        {
            this.TableName = tableName;
            this.DataOperation = dataOperation;
            this.LogicStage = logicStage;
            this.Order = order;
        }
    }
}