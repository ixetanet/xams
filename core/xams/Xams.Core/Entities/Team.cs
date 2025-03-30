using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xams.Core.Entities;

[Table("Team")]
public class Team
{
    public Guid TeamId { get; set; }
    [MaxLength(250)]
    public string? Name { get; set; }
    
    public int Discriminator { get; set; }
    
    // public ICollection<TeamRole> TeamRoles { get; set; } = null!;
}