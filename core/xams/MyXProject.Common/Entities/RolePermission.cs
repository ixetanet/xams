using System.ComponentModel.DataAnnotations.Schema;

namespace MyXProject.Common.Entities;

[Table(nameof(RolePermission))]
public class RolePermission
{
    public Guid RolePermissionId { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}