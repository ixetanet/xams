using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("Team")]
public class Team
{
    public Guid TeamId { get; set; }
    public string Name { get; set; }
    
    public ICollection<TeamRole> TeamRoles { get; set; } = null!;
}