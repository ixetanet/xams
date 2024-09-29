using Microsoft.EntityFrameworkCore;
using Example.Common.Entities;

namespace Example.Common.Base;

[Index(nameof(OwningUserId))]
[Index(nameof(OwningTeamId))]
public class BaseEntity
{
    
    public Guid? OwningUserId { get; set; }
    public User? OwningUser { get; set; }
    public Guid? OwningTeamId { get; set; }
    public Team? OwningTeam { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public Guid CreatedById { get; set; }
    public User? CreatedBy { get; set; }
    public Guid UpdatedById { get; set; }
    public User? UpdatedBy { get; set; }
    
    public bool IsActive { get; set; }
}