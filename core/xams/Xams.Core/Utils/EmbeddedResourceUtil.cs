using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Xams.Core.Utils;

public static class EmbeddedResourceUtil
{
    private static ManifestEmbeddedFileProvider? _embeddedFileProvider;

    public static Assembly Assembly => typeof(EmbeddedResourceUtil).Assembly;

    public static string GetNamespace() => typeof(EmbeddedResourceUtil).Namespace;

    /// <summary>
    /// Lists all embedded resources in the assembly
    /// </summary>
    public static string[] GetResourceNames()
    {
        return Assembly.GetManifestResourceNames();
    }

    /// <summary>
    /// Gets or creates the embedded file provider instance
    /// </summary>
    internal static IFileProvider GetEmbeddedFileProvider()
    {
        if (_embeddedFileProvider == null)
        {
            var assembly = typeof(EmbeddedResourceUtil).Assembly;
            _embeddedFileProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot/xams");
        }
        return _embeddedFileProvider;
    }

    public class XamsDashboardOptions
    {
        public bool RequireAuthorization { get; set; }
    }

    /// <summary>
    /// Adds static files from embedded resources at /xams path with Next.js routing support
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="options"></param>
    /// <returns>The application builder</returns>
    internal static IApplicationBuilder UseXamsDashboard(
        this IApplicationBuilder app,
        Action<XamsDashboardOptions> options)
    {
        var opts = new XamsDashboardOptions();
        options.Invoke(opts);

        var embeddedFileProvider = GetEmbeddedFileProvider();

        // Add routing middleware for Next.js static export
        app.Use(async (context, next) =>
        {
            await EmbeddedRoutingUtil.SetupEmbeddedRoutes(context, next, embeddedFileProvider);
        });

        // Serve embedded static files at /xams path
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedFileProvider,
            RequestPath = "/xams"
        });

        return app;
    }
}
