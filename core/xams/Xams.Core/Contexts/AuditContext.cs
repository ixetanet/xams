using Xams.Core.Entities;

namespace Xams.Core.Contexts;

/// <summary>
/// Passed to OnCreateAudit after the audit records have been committed.
/// </summary>
public class AuditContext : AuditBaseContext
{
    /// <summary>
    /// The committed audit records grouped by table name.
    /// </summary>
    public Dictionary<string, List<AuditHistoryEntity>> AuditHistoryEntities { get; set; } = new();
}

public class AuditHistoryEntity
{
    public AuditHistory AuditHistory { get; internal set; } = null!;
    public Dictionary<string, AuditHistoryDetail> AuditHistoryDetails { get; internal set; } = new();
}
