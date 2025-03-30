using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Xams.Core.Entities;

[Table(nameof(System))]
[Index(nameof(Name), nameof(DateTime), Name = "IX_System_Name_DateTime")]
public class System
{
    public Guid SystemId { get; set; }
    [MaxLength(250)]
    public string? Name { get; set; }
    [MaxLength(250)]
    public string? Value { get; set; }
    public DateTime? DateTime { get; set; }
}