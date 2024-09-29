using System.ComponentModel.DataAnnotations.Schema;

namespace MyXProject.Common.Entities;

[Table(nameof(UserRole))]
public class UserRole
{
    public Guid UserRoleId { get; set; }
    public User User { get; set; } = null!;
    public Guid UserId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid RoleId { get; set; }
}