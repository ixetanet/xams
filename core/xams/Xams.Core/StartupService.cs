using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Startup;

namespace Xams.Core
{
    public class StartupService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public StartupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
                var dataServiceType = dataService.GetType();
                var dbContextType = dataServiceType.GenericTypeArguments[0];
                Cache.Initialize(dbContextType);
                await ExecuteStartupServices(StartupOperation.Pre, scope.ServiceProvider);
                var systemRecords = new SystemRecords(dataService);
                await systemRecords.CreateSystemUser();
                await systemRecords.CreateSystemRoles();
                await systemRecords.CreateSystemTeams();
                await systemRecords.CreateSystemPermissions();
                await systemRecords.CreateRolePermissions();
                await systemRecords.CreateUserRoles();
                await systemRecords.CreateTeamRoles();
                await systemRecords.CreateTeamUsers();
                await systemRecords.CreateSettingAndSystemRecords();
                await ExecuteStartupServices(StartupOperation.Post, scope.ServiceProvider);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        
        public async Task ExecuteStartupServices(StartupOperation startupOperation, IServiceProvider serviceProvider)
        {
            List<Cache.ServiceStartupInfo> startupServices = Cache.Instance.ServiceStartupInfos
                .Where(x => x.ServiceStartupAttribute.StartupOperation == startupOperation)
                .OrderBy(x => x.ServiceStartupAttribute.Order).ToList();

            var dataService = serviceProvider.GetRequiredService<IDataService>();
            var startupContext = new StartupContext(serviceProvider, dataService);

            foreach (var startupService in startupServices)
            {
                if (startupService.Type != null)
                {
                    object? service = Activator.CreateInstance(startupService.Type);
                    MethodInfo? methodInfo = startupService.Type.GetMethod("Execute");
                    if (methodInfo == null)
                    {
                        throw new Exception($"Method Execute not found in {startupService.Type.Name}");
                    }
                    
                    Response<object?> response = await ((Task<Response<object?>>)methodInfo.Invoke(service,
                    [
                        startupContext
                    ])!);
                        
                    if (!response.Succeeded)
                    {
                        throw new Exception(response.FriendlyMessage);
                    }
                }
            }
        }
    }
}