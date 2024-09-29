using System.ComponentModel.DataAnnotations.Schema;

namespace Example.Common.Entities;

[Table("Permission")]
public class Permission
{
    public Guid PermissionId { get; set; }
    public string Name { get; set; }
    public string? Tag { get; set; }
}