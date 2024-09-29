using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyXProject.Common.Entities;

[Table(nameof(Role))]
public class Role
{
    public Guid RoleId { get; set; }
    [MaxLength(250)]
    public string? Name { get; set; }
    
    public ICollection<RolePermission> RolePermissions { get; set; } = null!;
}