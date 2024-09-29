using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("RolePermission")]
public class RolePermission
{
    public Guid RolePermissionId { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; }
}