using System.ComponentModel.DataAnnotations.Schema;

namespace Xams.Core.Entities;

[Table("TeamUser")]
public class TeamUser<TUser, TTeam>
where TUser : User
where TTeam : Team
{
    public Guid TeamUserId { get; set; }
    public Guid TeamId { get; set; }
    public TTeam Team { get; set; } = null!;
    public Guid UserId { get; set; }
    public TUser User { get; set; } = null!;
}