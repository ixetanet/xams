using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Xams.Core.Base;
using Xams.Core.Pipeline;

namespace Xams.Core.Contexts;

public class HubContext : BaseServiceContext
{
    public string Message { get; }
    public HubCallerContext SignalRContext { get; }
    public IGroupManager Groups { get; }
    public IHubCallerClients Clients { get; }
    
    public HubContext(PipelineContext pipelineContext, string message, SignalRHub signalRHub) : base(pipelineContext)
    {
        Message = message;
        SignalRContext = signalRHub.Context;
        Groups = signalRHub.Groups;
        Clients = signalRHub.Clients;;
    }
    
    public T GetMessage<T>() where T : class
    {
        if (string.IsNullOrEmpty(Message))
        {
            throw new Exception("Message is null or empty");    
        }
        return JsonSerializer.Deserialize<T>(Message) ?? 
               throw new Exception($"Failed to parse hub message to type {typeof(T).Name}.");
        
    }
}