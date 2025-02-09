using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Attributes;

namespace MyXProject.Common.Entities;

[Table(nameof(Server))]
public class Server
{
    public Guid ServerId { get; set; }
    [MaxLength(200)]
    public string Name { get; set; } = null!;
    [UIDateFormat("MMM D, h:mm:ss A")]
    public DateTime LastPing { get; set; }
}