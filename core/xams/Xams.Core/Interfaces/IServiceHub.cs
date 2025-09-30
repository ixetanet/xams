using Xams.Core.Contexts;
using Xams.Core.Dtos;

namespace Xams.Core.Interfaces;

public interface IServiceHub
{
    /// <summary>
    /// Client connected to the hub and has permission to use it.
    /// Can use this method to add the user to a group, initialize state, etc.
    /// Also called if a client is connected, and they've recently been given permission to use the hub.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task<Response<object?>> OnConnected(HubContext context);

    /// <summary>
    /// Client disconnected from the hub.
    /// Can use this method to clean up state, remove the user from a group, etc.
    /// Also called if the client recently lost permission to use the hub,
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task<Response<object?>> OnDisconnected(HubContext context);
    
    /// <summary>
    /// On Receive method is called when a message is received from the client.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task<Response<object?>> OnReceive(HubContext context);
    
    /// <summary>
    /// Send method is called to send a message to the client.
    /// This will only be called from server-side code
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public Task<Response<object?>> Send(HubSendContext context);
    
}