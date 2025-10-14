using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Xams.Core.Utils;

public static class EmbeddedRoutingUtil
{
    private static readonly ConcurrentDictionary<string, bool> FileExistsCache = new();

    /// <summary>
    /// Sets up Next.js static export routing for embedded resources at /xams path
    /// Handles client-side routing by rewriting URLs to their corresponding .html files
    /// </summary>
    public static async Task SetupEmbeddedRoutes(
        HttpContext context,
        Func<Task> next,
        IFileProvider embeddedFileProvider)
    {
        var path = context.Request.Path.Value;

        // Only process /xams paths
        if (path == null || !path.StartsWith("/xams"))
        {
            await next();
            return;
        }

        // Skip processing for actual files (js, css, images, etc.)
        if (path.Contains('.') && !path.EndsWith(".html"))
        {
            await next();
            return;
        }

        // Remove /xams prefix for file provider lookups
        var relativePath = path.Substring(5).TrimStart('/');

        // If the root /xams path is requested, serve index.html
        if (string.IsNullOrEmpty(relativePath))
        {
            context.Request.Path = "/xams/index.html";
            await next();
            return;
        }

        // Check if the requested HTML file exists (without .html in the URL)
        var htmlFilePath = $"{relativePath}.html";

        // Use cache to check if file exists in embedded resources
        var cacheKey = $"embedded:{htmlFilePath}";
        bool fileExists = FileExistsCache.GetOrAdd(cacheKey, _ =>
        {
            var fileInfo = embeddedFileProvider.GetFileInfo(htmlFilePath);
            return fileInfo.Exists && !fileInfo.IsDirectory;
        });

        if (fileExists)
        {
            // Rewrite to the actual .html file
            context.Request.Path = $"/xams/{htmlFilePath}";
            await next();
            return;
        }
        

        // Otherwise, let the request continue (will likely 404)
        await next();
    }
}