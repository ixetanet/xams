using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table(nameof(Setting))]
public class Setting
{
    public Guid SettingId { get; set; }
    [MaxLength(250)]
    public string? Name { get; set; }
    [MaxLength(2000)]
    public string? Value { get; set; } 
}