using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.FileProviders;

namespace Xams.Core.Utils;

public static class EmbeddedResourceUtil
{
    public static Assembly Assembly => typeof(EmbeddedResourceUtil).Assembly;
        
    public static string GetNamespace() => typeof(EmbeddedResourceUtil).Namespace;
        
    /// <summary>
    /// Lists all embedded resources in the assembly
    /// </summary>
    public static string[] GetResourceNames()
    {
        return Assembly.GetManifestResourceNames();
    }

    public class XamsDashboardOptions
    {
        public string Path { get; set; } = "/xams";
        public bool RequireAuthorization { get; set; }
    }

    /// <summary>
    /// Adds static files from embedded resources in the class library
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="options"></param>
    /// <returns>The application builder</returns>
    public static IApplicationBuilder UseXamsDashboard(
        this IApplicationBuilder app,
        Action<XamsDashboardOptions> options)
    {
        var opts = new XamsDashboardOptions();
        options.Invoke(opts);

        if (opts.RequireAuthorization)
        {
            app.UseStaticFileAuthorization(opts.Path);
        }
        
        var pathRegex = opts.Path.TrimStart('/');
        
        app.UseRewriter(new RewriteOptions()
            .AddRewrite($"^{pathRegex}/admin$", $"/xams/index.html", skipRemainingRules: true)
            .AddRewrite("^xams/Permissions", $"{opts.Path}/Permissions", skipRemainingRules: true)
            .AddRewrite("^xams/MetaData", $"{opts.Path}/MetaData", skipRemainingRules: true)
            .AddRewrite("^xams/Read", $"{opts.Path}/Read", skipRemainingRules: true)
            .AddRewrite("^xams/Create", $"{opts.Path}/Create", skipRemainingRules: true)
            .AddRewrite("^xams/Update", $"{opts.Path}/Update", skipRemainingRules: true)
            .AddRewrite("^xams/Delete", $"{opts.Path}/Delete", skipRemainingRules: true)
            .AddRewrite("^xams/Upsert", $"{opts.Path}/Upsert", skipRemainingRules: true)
            .AddRewrite("^xams/Bulk", $"{opts.Path}/Bulk", skipRemainingRules: true)
            .AddRewrite("^xams/Action", $"{opts.Path}/Action", skipRemainingRules: true)
            .AddRewrite("^xams/File", $"{opts.Path}/File", skipRemainingRules: true));
        
        var assembly = typeof(EmbeddedResourceUtil).Assembly;
        var embeddedFileProvider = new ManifestEmbeddedFileProvider(
            assembly, "wwwroot/admin"); 
            
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedFileProvider,
            RequestPath = opts.Path
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedFileProvider,
            RequestPath = "/xams",
        });
            
        return app;
    }
}
