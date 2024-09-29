using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("Role")]
public class Role
{
    public Guid RoleId { get; set; }
    public string Name { get; set; }
    
    public ICollection<RolePermission> RolePermissions { get; set; } = null!;
}