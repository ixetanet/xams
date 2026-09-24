using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

// Run every 30 minutes and delete audit history older than the retention period
[JobServer(ExecuteJobOn.One)]
[ServiceJob(nameof(AuditHistoryRetentionJob), "System-AuditHistory", "00:30:00", JobSchedule.Interval, DaysOfWeek.All, "System")]
public class AuditHistoryRetentionJob : IServiceJob
{
    internal const int DefaultRetentionDays = 30;
    internal const int BatchSize = 1000;

    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<IXamsDbContext>();
        var setting = await Queries.GetCreateSetting(db, AuditStartupService.AuditRetentionSetting,
            DefaultRetentionDays.ToString());
        if (!int.TryParse(setting, out var retentionDays) || retentionDays < 1)
        {
            context.Logger.LogWarning(
                "Invalid {Setting} value '{Value}', using {Default} days",
                AuditStartupService.AuditRetentionSetting, setting, DefaultRetentionDays);
            retentionDays = DefaultRetentionDays;
        }

        await Purge(db, DateTime.UtcNow.AddDays(-retentionDays));

        return ServiceResult.Success();
    }

    /// <summary>
    /// Deletes audit history created before the cutoff, with its details, in set-based batches. The delete
    /// pipeline is not used because audit records cannot be changed or deleted through it.
    /// </summary>
    internal static async Task<int> Purge(IXamsDbContext db, DateTime cutoff)
    {
        var deleted = 0;
        while (true)
        {
            var ids = await db.AuditHistoriesBase
                .Where(x => x.CreatedDate < cutoff)
                .OrderBy(x => x.CreatedDate)
                .Select(x => x.AuditHistoryId)
                .Take(BatchSize)
                .ToListAsync();
            if (ids.Count == 0)
            {
                return deleted;
            }

            await db.AuditHistoryDetailsBase
                .Where(x => ids.Contains(x.AuditHistoryId))
                .ExecuteDeleteAsync();
            deleted += await db.AuditHistoriesBase
                .Where(x => ids.Contains(x.AuditHistoryId))
                .ExecuteDeleteAsync();
        }
    }
}
