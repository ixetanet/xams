using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Xams.Core.Base;
using Xams.Core.Pipeline;

namespace Xams.Core.Contexts;

public class HubSendContext : BaseServiceContext
{
    public object? Message { get; }
    public IGroupManager Groups { get; }
    public IHubClients Clients { get; }
    
    public HubSendContext(PipelineContext pipelineContext, object? message, IHubContext<SignalRHub> signalRHub) : base(pipelineContext)
    {
        Message = message;
        Groups = signalRHub.Groups;
        Clients = signalRHub.Clients;
    }
    
    public T GetMessage<T>() where T : class
    {
        if (Message == null)
        {
            throw new Exception("Message is null or empty");
        }
        return (T)Message;
    }
}