using Xams.Core.Attributes;
using Xams.Core.Base;
using Xams.Core.Contexts;
using Xams.Core.Dtos;
using Xams.Core.Interfaces;
using Xams.Core.Utils;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// This Creates the AuditHistory records for the AuditHistory table
/// </summary>
[ServiceLogic("*", DataOperation.Read, LogicStage.PostOperation, Int32.MaxValue)]
internal class AuditHistoryBulkService : IServiceLogic
{
    public async Task<Response<object?>> Execute(ServiceContext context)
    {
        var db = context.GetDbContext<IXamsDbContext>();
        var newDb = context.DataRepository.CreateNewDbContext();
        
        
        var auditInfo = Cache.Instance.TableAuditInfo[context.TableName];
        if (auditInfo.IsReadAuditEnabled)
        {
            var options = new AuditLogic.AuditHistoryRecordOptions()
            {
                TableName = context.TableName,
                DataOperation = DataOperation.Read,
                ExecutingUserId = context.ExecutingUserId,
                TransactionDb = db,
                NewDb = newDb,
                ReadInput = context.ReadInput,
                ReadOutput = context.ReadOutput
            };
            AuditLogic.AddAuditHistoryRecord(options);
        }
        
        await db.SaveChangesAsync();

        return ServiceResult.Success();
    }
}