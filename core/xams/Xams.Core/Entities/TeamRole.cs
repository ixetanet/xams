using System.ComponentModel.DataAnnotations.Schema;

namespace Xams.Core.Entities;

[Table("TeamRole")]
public class TeamRole<TTeam, TRole> 
    where TTeam : Team
    where TRole : Role
{
    public Guid TeamRoleId { get; set; }
    public Guid TeamId { get; set; }
    public TTeam Team { get; set; } = null!;
    public Guid RoleId { get; set; }
    public TRole Role { get; set; } = null!;
}