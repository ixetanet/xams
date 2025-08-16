using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Jobs;
using Xams.Core.Services.Jobs;
using Xams.Core.Utils;
// ReSharper disable InconsistentNaming

namespace Xams.Core.Actions;

[ServiceAction("ADMIN_ImportData")]
public class ADMIN_ImportData : IServiceAction
{
    public async Task<Response<object?>> Execute(ActionServiceContext context)
    {
        if (context.File == null && !context.Parameters.ContainsKey("jobHistoryId"))
        {
            return ServiceResult.Error("Import Data must have a 'jobHistoryId' parameter or a file to import.");
        }

        // Save the file to the system to be processed
        // by the import job
        if (context.File != null)
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                throw new Exception("Unable to load entry assembly");
            }

            var assemblyDirectory = Path.GetDirectoryName(entryAssembly.Location);
            if (string.IsNullOrEmpty(assemblyDirectory))
            {
                throw new Exception("Unable to load assembly directory");
            }
                
            var importId = Guid.NewGuid();
            var fileName = $"{importId}.json";
            var directory = Path.Combine(assemblyDirectory, "data_import");
            var filePath = Path.Combine(directory, fileName);

            // Ensure directory exists
            Directory.CreateDirectory(directory);
            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await context.File.CopyToAsync(stream);
            }

            // Trigger the job
            await context.ExecuteJob(new JobOptions()
            {
                JobName = nameof(ImportDataJob),
                JobServer = Cache.Instance.ServerName,
                JobHistoryId = importId,
                Parameters = new
                {
                    FilePath = filePath,
                }
            });

            return ServiceResult.Success(new
            {
                jobHistoryId = importId,
            });
        }

        if (context.Parameters.ContainsKey("jobHistoryId"))
        {
            var jobHistoryIdString = context.Parameters["jobHistoryId"].ToString();
            if (!Guid.TryParse(jobHistoryIdString, out Guid jobHistoryId))
            {
                return ServiceResult.Error("Unable to parse Job History Id");
            }

            var db = context.GetDbContext<IXamsDbContext>();
            var jobHistory = await db.JobHistoriesBase
                .FirstOrDefaultAsync(x => x.JobHistoryId == jobHistoryId);

            return ServiceResult.Success(new
            {
                jobStatus = jobHistory == null ? "Not Started" : jobHistory.Status,
                jobMessage = jobHistory == null ? "Not Started" : jobHistory.Message,
            });
        }


        return new Response<object?>()
        {
            Succeeded = false,
            FriendlyMessage = "Import Data must have a 'jobHistoryId' parameter or a file to import.",
            Data = null
        };
    }
}