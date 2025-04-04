// XamsApiExtensions.cs

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Base;
using Xams.Core.Dtos;
using Xams.Core.Dtos.Data;
using Xams.Core.Entities;
using Xams.Core.Interfaces;
using Xams.Core.Services;
using Xams.Core.Utils;

namespace Xams.Core
{
    public static class ProgramStart
    {
        public static IServiceCollection AddXamsServices<TDataContext>(this IServiceCollection services)
            where TDataContext : XamsDbContext,
            new()
        {
            services.AddHostedService<StartupService>();
            services.AddScoped<IDataService, DataService<TDataContext>>();
            return services;
        }
        
        public static IServiceCollection AddXamsServices<TDataContext, TAppUser>(this IServiceCollection services)
            where TDataContext : XamsDbContext<TAppUser>
            where TAppUser : User,
            new()
        {
            services.AddHostedService<StartupService>();
            services.AddScoped<IDataService, DataService<TDataContext, TAppUser>>();
            return services;
        }
        
        public static IServiceCollection AddXamsServices<TDataContext, TAppUser, TAppTeam>(this IServiceCollection services)
        where TDataContext : XamsDbContext<TAppUser, TAppTeam>
        where TAppUser : User
        where TAppTeam : Team,
        new()
        {
            services.AddHostedService<StartupService>();
            services.AddScoped<IDataService, DataService<TDataContext, TAppUser, TAppTeam>>();
            return services;
        }
        
        public static IServiceCollection AddXamsServices<TDataContext, TAppUser, TAppTeam, TAppRole>(this IServiceCollection services)
            where TDataContext : XamsDbContext<TAppUser, TAppTeam, TAppRole>
            where TAppUser : User
            where TAppTeam : Team
            where TAppRole : Role, 
            new()
        {
            services.AddHostedService<StartupService>();
            services.AddScoped<IDataService, DataService<TDataContext, TAppUser, TAppTeam, TAppRole>>();
            return services;
        }
        
        public static IServiceCollection AddXamsServices<TDataContext, TAppUser, TAppTeam, TAppRole, TAppSetting>(this IServiceCollection services)
            where TDataContext : XamsDbContext<TAppUser, TAppTeam, TAppRole, TAppSetting>
            where TAppUser : User
            where TAppTeam : Team
            where TAppRole : Role
            where TAppSetting: Setting, 
            new()
        {
            services.AddHostedService<StartupService>();
            services.AddScoped<IDataService, DataService<TDataContext, TAppUser, TAppTeam, TAppRole, TAppSetting>>();
            return services;
        }

        public class AddXamsApiOptions
        {
            public bool UseDashboard { get; set; }
            public string UrlPath { get; set; } = "xams";
            public bool RequireAuthorization { get; set; } = false;
            public Func<HttpContext, Task<Guid>>? GetUserId { get; set; }
        }

        public static IEndpointRouteBuilder AddXamsApi(this WebApplication app,
            Action<AddXamsApiOptions>? options = null)
        {
            var opts = new AddXamsApiOptions();
            options?.Invoke(opts);

            if (opts.UseDashboard)
            {
                app.UseXamsDashboard(dashOptions =>
                {
                    dashOptions.Path = $"/{opts.UrlPath}";
                    dashOptions.RequireAuthorization = opts.RequireAuthorization;
                });
            }

            var group = app.MapGroup(opts.UrlPath);
            
            var whoAmI = group.MapGet("whoami",
                async (IDataService dataService, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.WhoAmI(userId);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                whoAmI.RequireAuthorization();
            }


            var permissions = group.MapPost("permissions",
                async (IDataService dataService, [FromBody] PermissionsInput permissionsInput,
                    HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Permissions(permissionsInput, userId);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                permissions.RequireAuthorization();
            }

            var metadata = group.MapPost("metadata",
                async (IDataService dataService, [FromBody] MetadataInput metadataInput, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Metadata(metadataInput, userId);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                metadata.RequireAuthorization();
            }

            var read = group.MapPost("read",
                async (IDataService dataService, [FromBody] ReadInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var response = await dataService.Read(userId, input);
                    var result = new Response<object?>()
                    {
                        Succeeded = response.Succeeded,
                        FriendlyMessage = response.FriendlyMessage,
                        LogMessage = response.LogMessage,
                        Data = response.Data
                    };
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                read.RequireAuthorization();
            }

            var create = group.MapPost("create",
                async (IDataService dataService, [FromBody] BatchInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Create(userId, input);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                create.RequireAuthorization();   
            }

            var update = group.MapPatch("update",
                async (IDataService dataService, [FromBody] BatchInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Update(userId, input);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                update.RequireAuthorization();
            }

            var delete = group.MapDelete("delete",
                async (IDataService dataService, [FromBody] BatchInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Delete(userId, input);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                delete.RequireAuthorization();
            }

            var upsert = group.MapPost("upsert",
                async (IDataService dataService, [FromBody] BatchInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Upsert(userId, input);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                upsert.RequireAuthorization();
            }

            var bulk = group.MapPost("bulk",
                async (IDataService dataService, [FromBody] BulkInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Bulk(userId, input);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                bulk.RequireAuthorization();
            }

            var action = group.MapPost("action",
                async (IDataService dataService, [FromBody] ActionInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Action(userId, input, httpContext);
                    return ToApiResponse(result);
                });
            if (opts.RequireAuthorization)
            {
                action.RequireAuthorization();
            }

            var file = group.MapPost("file",
                async (IDataService dataService, [FromForm] FileInput input, HttpContext httpContext) =>
                {
                    Guid userId = await (opts.GetUserId?.Invoke(httpContext) ?? GetUserId(httpContext));
                    var result = await dataService.Action(userId, input, httpContext);
                    return ToApiResponse(result);
                }).DisableAntiforgery();
            if (opts.RequireAuthorization)
            {
                file.RequireAuthorization();
            }

            return app;
        }

        private static async Task<Guid> GetUserId(HttpContext httpContext)
        {
            if (httpContext.Request.Headers.ContainsKey("UserId"))
            {
                string userId = httpContext.Request.Headers["UserId"].ToString();
                if (Guid.TryParse(userId, out Guid guid))
                {
                    return guid;
                }
                throw new Exception("UserId in header is not a Guid");
            }
            
            throw new Exception("UserId not found in request headers");
        }


        public static IResult ToApiResponse(Response<object?> response)
        {
            try
            {
                ApiResponse apiResponse;
                if (response.ResponseType == ResponseType.Json)
                {
                    apiResponse = new ApiResponse()
                    {
                        succeeded = response.Succeeded,
                        friendlyMessage = response.FriendlyMessage,
                        logMessage = response.LogMessage,
                        data = response.Data
                    };
                    if (apiResponse.succeeded)
                    {
                        return Results.Json(apiResponse, new JsonSerializerOptions()
                        {
                            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                            PropertyNamingPolicy = null
                        });
                    }

                    return Results.BadRequest(apiResponse);
                }

                if (response.ResponseType == ResponseType.File)
                {
                    if (response.Data is FileData)
                    {
                        var fileData = (FileData)response.Data;
                        if (response.Data != null)
                        {
                            return Results.File(fileData.Stream, fileData.ContentType, fileData.FileName);
                        }
                    }
                }

                apiResponse = new ApiResponse()
                {
                    succeeded = response.Succeeded,
                    friendlyMessage = response.FriendlyMessage,
                    logMessage = response.LogMessage,
                    data = response.Data
                };
                if (apiResponse.succeeded)
                {
                    return Results.Json(apiResponse, new JsonSerializerOptions()
                    {
                        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                        PropertyNamingPolicy = null
                    });
                }

                return Results.BadRequest(apiResponse);
            }
            catch (Exception e)
            {
                return Results.BadRequest(new ApiResponse()
                {
                    succeeded = false,
                    friendlyMessage = e.Message,
                    logMessage = e.InnerException?.Message
                });
            }
        }
    }
}