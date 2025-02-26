using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace MyXProject.Common.Entities;

[Table(nameof(Setting))]
public class Setting
{
    public Guid SettingId { get; set; }
    [UIRequired]
    [MaxLength(250)]
    public string? Name { get; set; }
    [MaxLength(2000)]
    public string? Value { get; set; } 
}