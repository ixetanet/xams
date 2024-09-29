using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("UserRole")]
public class UserRole
{
    public Guid UserRoleId { get; set; }
    public User User { get; set; }
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public Guid RoleId { get; set; }
}