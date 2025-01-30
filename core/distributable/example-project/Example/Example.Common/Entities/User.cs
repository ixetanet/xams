using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table(nameof(User))]
public class User
{
    public Guid UserId { get; set; }
    [MaxLength(250)]
    public string? Name { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    
    public ICollection<UserRole> UserRoles { get; set; } = null!;
}