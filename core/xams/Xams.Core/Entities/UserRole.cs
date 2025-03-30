using System.ComponentModel.DataAnnotations.Schema;

namespace Xams.Core.Entities;

[Table("UserRole")]
public class UserRole<TUser, TRole>
    where TUser : User
    where TRole : Role
{
    public Guid UserRoleId { get; set; }
    public TUser User { get; set; } = null!;
    public Guid UserId { get; set; }
    public TRole Role { get; set; } = null!;
    public Guid RoleId { get; set; }
}