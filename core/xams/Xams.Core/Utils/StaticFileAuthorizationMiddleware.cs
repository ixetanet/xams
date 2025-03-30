using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Xams.Core.Utils;

public class StaticFileAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _protectedPath;

    public StaticFileAuthorizationMiddleware(RequestDelegate next, string protectedPath)
    {
        _next = next;
        _protectedPath = protectedPath;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request is for the protected path
        if (context.Request.Path.StartsWithSegments(_protectedPath))
        {
            // Check if user is authenticated
            if (context.User.Identity is { IsAuthenticated: false })
            {
                context.Response.StatusCode = 401; // Unauthorized
                return;
            }

            // Check if user has required role/permission
            // if (!context.User.IsInRole("Admin")) // Or any other authorization check
            // {
            //     context.Response.StatusCode = 403; // Forbidden
            //     return;
            // }
        }

        // Call the next middleware in the pipeline
        await _next(context);
    }
}

// Extension method to make it easier to add the middleware
public static class StaticFileAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseStaticFileAuthorization(
        this IApplicationBuilder builder, string protectedPath)
    {
        return builder.UseMiddleware<StaticFileAuthorizationMiddleware>(protectedPath);
    }
}