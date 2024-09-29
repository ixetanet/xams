using Example.Data;
using Xams.Core.Attributes;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Example.Services.ServiceJobs;

// Run at 1am every day
[ServiceJob(nameof(DeleteOldWidgets), "Primary", "01:00:00", JobSchedule.TimeOfDay)]
public class DeleteOldWidgets : IServiceJob
{
    public async Task<Response<object?>> Execute(JobServiceContext context)
    {
        var db = context.GetDbContext<DataContext>();
        
        // Delete all but 20 widgets
        var widgets = db.Widgets.Skip(20);
        db.Widgets.RemoveRange(widgets);
        
        await db.SaveChangesAsync();

        return ServiceResult.Success();
    }
}