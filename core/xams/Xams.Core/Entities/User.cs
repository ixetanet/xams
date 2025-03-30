using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Xams.Core.Entities;

[Table("User")]
public class User
{
    public Guid UserId { get; set; }
    [MaxLength(100)]
    public string? Name { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    
    public int Discriminator { get; set; }
    
    // public ICollection<UserRole<User, TRole>> UserRoles { get; set; } = null!;
}