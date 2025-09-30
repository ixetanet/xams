using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xams.Core.Pipeline.Stages;

namespace Xams.Core.Entities;

[Table("Log")]
public class Log
{
    [Key]
    public Guid LogId { get; set; }
    
    // Core indexed fields for fast querying
    public DateTime Timestamp { get; set; }
    [MaxLength(20)]
    public string Level { get; set; } = null!;
    [MaxLength(4000)]
    public string Message { get; set; } = null!;
    [MaxLength(4000)]
    public string? MessageTemplate { get; set; }
    
    // Common queryable properties as columns
    [MaxLength(500)]
    public string? SourceContext { get; set; }
    [MaxLength(100)]
    public string? MachineName { get; set; }
    [MaxLength(50)]
    public string? Environment { get; set; }
    [MaxLength(100)]
    public string? ApplicationName { get; set; }
    [MaxLength(50)]
    public string? Version { get; set; }
    public int? ThreadId { get; set; }
    
    // Request/Response tracking
    public Guid? CorrelationId { get; set; }
    [MaxLength(100)]
    public string? RequestId { get; set; }
    [MaxLength(500)]
    public string? RequestPath { get; set; }
    [MaxLength(20)]
    public string? RequestMethod { get; set; }
    public int? StatusCode { get; set; }
    public double? ElapsedMs { get; set; }
    
    // User/Security context
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(256)]
    public string? UserName { get; set; }
    [MaxLength(45)]
    public string? ClientIp { get; set; }
    [MaxLength(1000)]
    public string? UserAgent { get; set; }
    
    // Error details
    [MaxLength(50000)]
    public string? Exception { get; set; }
    [MaxLength(500)]
    public string? ExceptionType { get; set; }
    [MaxLength(2000)]
    public string? ExceptionMessage { get; set; }
    // Additional structured data as JSON
    [MaxLength(10000)]
    public string? Properties { get; set; }
    
    public Guid? JobHistoryId { get; set; }
    public JobHistory? JobHistory { get; set; }

}