using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Xams.Core.Entities;

[Table("UserRole")]
public class UserRole<TUser, TRole> : IdentityUserRole<Guid>
    where TUser : User
    where TRole : Role
{
    // public TUser User { get; set; } = null!;
    // public TRole Role { get; set; } = null!;
}