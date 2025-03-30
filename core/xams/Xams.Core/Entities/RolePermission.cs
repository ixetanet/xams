using System.ComponentModel.DataAnnotations.Schema;

namespace Xams.Core.Entities;

[Table("RolePermission")]
public class RolePermission<TRole>
    where TRole : Role
{
    public Guid RolePermissionId { get; set; }
    public Guid RoleId { get; set; }
    public TRole Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}