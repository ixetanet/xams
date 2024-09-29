using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("Setting")]
public class Setting
{
    public Guid SettingId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
}