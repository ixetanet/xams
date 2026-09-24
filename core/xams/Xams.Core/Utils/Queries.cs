using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Sets the value of the named System record, creating the record if it doesn't exist.
    /// </summary>
    public static async Task UpdateSystemRecord(IXamsDbContext db, string name, string value)
    {
        var systemRecord = await db.SystemsBase.AsNoTracking()
            .Where(x => x.Name == name)
            .OrderBy(x => x.SystemId)
            .FirstOrDefaultAsync();
        if (systemRecord == null)
        {
            db.Add(new Entities.System { SystemId = Guid.NewGuid(), Name = name, Value = value });
        }
        else
        {
            systemRecord.Value = value;
            db.Update(systemRecord);
        }

        await db.SaveChangesAsync();
    }
}
