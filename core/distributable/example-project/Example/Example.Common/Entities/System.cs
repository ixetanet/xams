using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table(nameof(System))]
public class System
{
    public Guid SystemId { get; set; }
    [MaxLength(250)]
    public string? Name { get; set; }
    [MaxLength(250)]
    public string? Value { get; set; }
    public DateTime? DateTime { get; set; }
}