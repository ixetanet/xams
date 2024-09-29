using Microsoft.Extensions.FileProviders;
using Example.Data;
using Xams.Core;
using Xams.Core.Interfaces;
using Xams.Core.Services;

var builder = WebApplication.CreateBuilder(args);

string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";
// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

string corsPolicy = "_myAllowSpecificOrigins";
if (environment is "Local" or "Development")
{
    builder.Services.AddCors(x => x.AddPolicy(corsPolicy, corsPolicyBuilder =>
    {
        corsPolicyBuilder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyMethod()
            .AllowAnyHeader();
    }));
}
else
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(corsPolicy, policy =>
        {
            if (environment == "Development")
                policy.WithOrigins("https://xxx.dev").AllowAnyHeader().AllowAnyMethod();
            if (environment == "Test")
                policy.WithOrigins("https://test.xxx.dev").AllowAnyHeader().AllowAnyMethod();
            if (environment == "Prod")
            {
                policy.WithOrigins("https://xxx.com").AllowAnyHeader().AllowAnyMethod();
                policy.WithOrigins("https://www.xxx.com").AllowAnyHeader().AllowAnyMethod();
            }
        });
    });
}

builder.Services.AddSwaggerGen();

// Add services to the container.
builder.Services.AddHostedService<StartupService>();
builder.Services.AddScoped<IDataService, DataService<DataContext>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.EnableTryItOutByDefault();
    });
}

app.UseCors(corsPolicy);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Reroute pages to use *.html 
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value != null && context.Request.Path.Value != "/" && !context.Request.Path.Value.Contains('.'))
    {
        context.Request.Path += ".html";
    }

    if (string.IsNullOrEmpty(context.Request.Path.Value) || context.Request.Path.Value == "/")
    {
        context.Request.Path = "/index.html";
    }
    await next(context);
});
app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot"))
});

app.Run();