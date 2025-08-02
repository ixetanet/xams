using System.Linq.Dynamic.Core;
using Xams.Core.Base;

namespace Xams.Core.Utils;

public static class Queries
{
    public static async Task<string?> GetCreateSetting(IXamsDbContext db, string name, string defaultValue)
    {
        Type settingType = Cache.Instance.GetTableMetadata("Setting").Type;
        DynamicLinq dynamicLinq = new DynamicLinq(db, settingType);
        var settings = (await dynamicLinq.Query.Where("Name == @0", name)
            .ToDynamicListAsync()).Select(x => (object)x).ToList();
        
        if (!settings.Any())
        {
            var entity = EntityUtil.DictionaryToEntity(settingType, new Dictionary<string, dynamic>
            {
                ["Name"] = name,
                ["Value"] = defaultValue
            });
            db.Add(entity);
            await db.SaveChangesAsync();
            return defaultValue;
        }

        return settings.First().GetValue<string?>("Value");
    }

    public static async Task UpdateSystemRecord(IXamsDbContext db, string name, string value)
    {
        var systemType = Cache.Instance.GetTableMetadata("System").Type;
        var dynamicLinq = new DynamicLinq(db, systemType);
        var query = dynamicLinq.Query.Where("Name == @0", "AuditLastRefresh");
        var auditSystemRecord = (await query.ToDynamicListAsync()).FirstOrDefault();
        if (auditSystemRecord == null)
        {
            throw new Exception($"Could not find system record with name 'AuditLastRefresh'");
        }

        auditSystemRecord.Value = value;
        db.Update(auditSystemRecord);
        await db.SaveChangesAsync();
    }
}