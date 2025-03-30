using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;
using Xams.Core.Entities;

namespace Xams.Core.Base;

[Index(nameof(OwningUserId))]
[Index(nameof(OwningTeamId))]
public class BaseEntity
{
    
    [UIDisplayName("Owning User")]
    public Guid? OwningUserId { get; set; }
    public User? OwningUser { get; set; }
    [UIDisplayName("Owning Team")]
    public Guid? OwningTeamId { get; set; }
    public Team? OwningTeam { get; set; }
    [UIDateFormat("lll")]
    public DateTime CreatedDate { get; set; }
    [UIDateFormat("lll")]
    public DateTime UpdatedDate { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
    
    public bool IsActive { get; set; }
}