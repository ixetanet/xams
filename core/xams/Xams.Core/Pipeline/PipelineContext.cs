using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos.Data;
using Xams.Core.Interfaces;
using Xams.Core.Repositories;

namespace Xams.Core.Pipeline;

public class PipelineContext
{
    public PipelineContext? Parent { get; internal set; }
    public Guid UserId { get; internal set; }
    public string TableName { get; internal set; } = "";
    public object? Entity { get; internal set; }
    public string[] Permissions { get; internal set; } = [];
    public object? PreEntity { get; internal set; }
    public DataOperation DataOperation { get; internal set; }
    public Dictionary<string, JsonElement> InputParameters { get; internal set; } = new();
    public Dictionary<string, JsonElement> OutputParameters { get; internal set; } = new();
    public SystemParameters SystemParameters { get; internal set; }
    public ReadInput? ReadInput { get; internal set; }
    public ReadOutput? ReadOutput { get; internal set; }
    public Dictionary<string, dynamic>? Fields { get; internal set; }
    public List<object>? Entities { get; internal set; }
    public IFormFile? File { get; internal set; }
    public bool IsProxy { get; internal set; }
    public IDataService DataService { get; internal set; } = null!;
    public DataRepository DataRepository { get; internal set; } = null!;
    public MetadataRepository MetadataRepository { get; internal set; } = null!;
    public SecurityRepository SecurityRepository { get; internal set; } = null!;
    public ServiceContext ServiceContext { get; private set; } = null!;
    public int Depth { get; internal set; }
    public PipelineContext()
    {
        SystemParameters = new SystemParameters();
    }
    

    public ServiceContext CreateServiceContext()
    {
        ServiceContext = new ServiceContext(
            this,
            LogicStage.PreOperation,
            DataOperation,
            Entities,
            ReadOutput
        );
        return ServiceContext;
    }
    
}