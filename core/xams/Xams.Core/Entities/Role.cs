using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Xams.Core.Entities;

[Table(nameof(Role))]
public class Role : IdentityRole<Guid>
{
    // [MaxLength(250)]
    // public string? Name { get; set; }
    public int Discriminator { get; set; }
    
    // public ICollection<RolePermission> RolePermissions { get; set; } = null!;
}