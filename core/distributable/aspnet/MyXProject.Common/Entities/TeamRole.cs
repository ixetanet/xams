using System.ComponentModel.DataAnnotations.Schema;

namespace MyXProject.Common.Entities;

[Table(nameof(TeamRole))]
public class TeamRole
{
    public Guid TeamRoleId { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
}