using Xams.Core.Base;

namespace Xams.Core.Services.Jobs;

public class JobPing
{
    private BaseDbContext _dbContext { get; set; }
    private dynamic _job { get; set; }
    private Timer? _timer { get; set; }
    private bool _isRunning { get; set; }

    public JobPing(BaseDbContext dbContext, dynamic job)
    {
        _dbContext = dbContext;
        _job = job;
    }

    public void Start()
    {
        _timer = new Timer(Ping, null, TimeSpan.Zero, TimeSpan.FromSeconds(JobService.Singleton!.PingInterval));
    }

    public async Task End()
    {
        if (_timer != null) await _timer.DisposeAsync();
    }

    private void Ping(object? state)
    {
        if (_isRunning) return;
        try
        {
            _isRunning = true;
            // Get latest in case the job was updated
            _dbContext.ChangeTracker.Clear();
            var job = _dbContext.Find(_job.GetType(), _job.JobId);
            job.Ping = DateTime.UtcNow;
            _dbContext.Update(job);
            _dbContext.SaveChanges();
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _isRunning = false;
        }
    }
}