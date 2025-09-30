using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Xams.Core.Services.Logging;

public class HttpContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _contextAccessor;
    
    public HttpContextEnricher(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _contextAccessor.HttpContext;
        if (httpContext == null) return;
        
        // Add request information
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("RequestId", httpContext.TraceIdentifier));
            
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path.Value));
            
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty("RequestMethod", httpContext.Request.Method));
            
        // Add client information
        var clientIp = GetClientIpAddress(httpContext);
        if (!string.IsNullOrEmpty(clientIp))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("ClientIp", clientIp));
        }
        
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        if (!string.IsNullOrEmpty(userAgent))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("UserAgent", userAgent));
        }
        
        // Add user context if available from Xams context
        if (httpContext.Items.TryGetValue("ExecutingUserId", out var userId) && userId is Guid userGuid)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("UserId", userGuid));
        }
        
        if (httpContext.Items.TryGetValue("ExecutingUserName", out var userName) && userName is string userNameStr)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("UserName", userNameStr));
        }
        
        // Add correlation ID if available
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId) && correlationId is Guid correlationGuid)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("CorrelationId", correlationGuid));
        }
        
        // Add response status code if response has started
        if (httpContext.Response.HasStarted)
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("StatusCode", httpContext.Response.StatusCode));
        }
    }
    
    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers first (when behind proxy/load balancer)
        var xForwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            // X-Forwarded-For can contain multiple IPs, take the first one
            return xForwardedFor.Split(',')[0].Trim();
        }
        
        var xRealIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }
        
        // Fall back to direct connection IP
        return context.Connection.RemoteIpAddress?.ToString();
    }
}