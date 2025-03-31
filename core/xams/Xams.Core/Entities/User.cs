using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Xams.Core.Entities;

[Table("User")]
public class User : IdentityUser<Guid>
{
    [MaxLength(100)]
    public string? Name { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    
    public int Discriminator { get; set; }
    
    // public ICollection<UserRole<User, TRole>> UserRoles { get; set; } = null!;
}