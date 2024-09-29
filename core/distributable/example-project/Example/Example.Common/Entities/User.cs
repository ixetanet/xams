using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("User")]
public class User
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    
    public ICollection<UserRole> UserRoles { get; set; } = null!;
}