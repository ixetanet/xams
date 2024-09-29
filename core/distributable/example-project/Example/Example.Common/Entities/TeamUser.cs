using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("TeamUser")]
public class TeamUser
{
    public Guid TeamUserId { get; set; }
    public Guid TeamId { get; set; }
    public Team Team { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
}