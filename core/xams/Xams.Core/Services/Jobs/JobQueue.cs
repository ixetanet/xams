using Microsoft.Extensions.DependencyInjection;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Interfaces;

// ReSharper disable ConvertToPrimaryConstructor
namespace Xams.Core.Services.Jobs;

public class JobQueue
{
    public string? Name { get; init; }
    private readonly IServiceProvider _serviceProvider;

    private bool _isRunning;
    
    private static readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    public JobQueue(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task Execute(List<Job> jobs)
    {
        try
        {
            if (_isRunning)
            {
                return;
            }

            await _semaphoreSlim.WaitAsync();
            _isRunning = true;
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                // Jobs that need to execute
                foreach (var job in jobs)
                {
                    await job.Execute(scope);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error executing queue {Name}: {e.Message}");
            }
            finally
            {
                // Make sure to release the lock
                _semaphoreSlim.Release();
            }
            
            _isRunning = false;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error running jobs: {e.Message}");
            _isRunning = false;
        }
    }
}