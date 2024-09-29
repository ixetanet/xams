using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("TeamRole")]
public class TeamRole
{
    public Guid TeamRoleId { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; }
    public Guid RoleId { get; set; }
    public Role Role { get; set; }
}