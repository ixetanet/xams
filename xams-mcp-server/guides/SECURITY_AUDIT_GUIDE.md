# Security Audit Guide

## Quick Start

**Step 1**: Build Entity Security Matrix

| Entity | BaseEntity? | [OwningUser] Fields | UIReadOnly/UICreateOnly? | [UIReadOnly] on State/Counts? | Vulnerabilities |
|--------|-------------|---------------------|--------------------------|-------------------------------|-----------------|
| Post | ✅ | None | ⚠️ CHECK | ⚠️ CHECK | TBD |
| Friendship | ✅ | RequesterId, ReceiverId | ⚠️ CHECK | ⚠️ CHECK | TBD |
| Comment | ✅ | None | ⚠️ CHECK | ⚠️ CHECK | TBD |

**Step 2**: For each row, read Entity files and apply CLAUDE.md security checklist (check for UIReadOnly/UICreateOnly attributes)

**Step 3**: Mark vulnerabilities:
- ❌ CRITICAL: Missing owner field enforcement
- ❌ HIGH: Missing [UIReadOnly], exceptions in Service Logic
- ⚠️ MEDIUM: Missing rate limiting, content validation
- ℹ️ LOW: Missing audit logging

## Common Vulnerability Patterns

### Pattern 1: [OwningUser] Without UICreateOnly
```csharp
// ❌ VULNERABLE: Friendship.cs
public class Friendship : BaseEntity
{
    [OwningUser]
    public Guid RequesterId { get; set; }  // Missing UICreateOnly = impersonation!

    [OwningUser]
    public Guid ReceiverId { get; set; }
}

// ✅ SECURE: Add class-level attributes
[Table(nameof(Friendship))]
[UIReadOnly(nameof(OwningUserId))]                      // Auto-set to current user
[UICreateOnly(nameof(RequesterId), nameof(ReceiverId))] // Set once, then immutable
public class Friendship : BaseEntity
{
    [OwningUser]
    public Guid RequesterId { get; set; }

    [OwningUser]
    public Guid ReceiverId { get; set; }
}
```

### Pattern 2: Status/Count Fields Without [UIReadOnly]
```csharp
// ❌ VULNERABLE
public string Status { get; set; } = "Pending";  // Client can bypass workflow
public int LikeCount { get; set; }               // Client can manipulate metrics

// ✅ SECURE: Field-level UIReadOnly
[UIReadOnly]
public string Status { get; set; } = "Pending";  // Only Service Logic can change
[UIReadOnly]
public int LikeCount { get; set; }               // Only Service Logic can increment

// ✅ SECURE: Class-level UIReadOnly for BaseEntity fields
[UIReadOnly(nameof(OwningUserId), nameof(CreatedDate), nameof(UpdatedDate))]
public class Post : BaseEntity  // Protects multiple BaseEntity fields
```

### Pattern 3: Exceptions Instead of ServiceResult
```csharp
// ❌ VULNERABLE
throw new Exception("Invalid");

// ✅ SECURE
return ServiceResult.Error("Invalid");
```

### Pattern 4: Hub Joins Without Permission Check
```csharp
// ❌ VULNERABLE: PostHub.cs
await context.Groups.AddToGroupAsync(connectionId, $"post-{postId}");  // No auth!

// ✅ SECURE: Check access first
var post = await db.Posts.FindAsync(postId);
if (!await CanUserViewPost(userId, post, db))
    return ServiceResult.Error("Access denied");
await context.Groups.AddToGroupAsync(connectionId, $"post-{postId}");
```

## Report Template

```markdown
# Security Audit Report

**Date**: [Date]
**Entities Audited**: [Count]

## Severity Summary
- CRITICAL: [count] - Immediate fix required
- HIGH: [count] - Fix this sprint
- MEDIUM: [count] - Plan next sprint
- LOW: [count] - Future enhancement

## CRITICAL Findings

### CRITICAL-001: [Entity] Missing UICreateOnly on [OwningUser] Fields
**Location**: [Entity].cs:[line]
**Issue**: Missing UICreateOnly attribute on [OwningUser] fields
**Risk**: Users can impersonate other users, manipulate ownership
**Fix**:
```csharp
[UIReadOnly(nameof(OwningUserId))]
[UICreateOnly(nameof(SenderId), nameof(ReceiverId))]
public class [Entity] : BaseEntity
```

## Remediation Plan

**Sprint 1 (Critical)**: Fix issues 001-005
**Sprint 2 (High)**: Fix issues 006-009
**Sprint 3 (Medium)**: Fix issues 010-012
```

## Detection Script

```bash
#!/bin/bash
# Run from project root

echo "=== Entities with [OwningUser] Fields ==="
grep -r "\[OwningUser\]" aspnet/*/Entities/ | cut -d: -f1 | sort -u

echo "\n=== Checking for UICreateOnly on entities with [OwningUser] ==="
for file in $(grep -l "\[OwningUser\]" aspnet/*/Entities/*.cs); do
  if ! grep -q "UICreateOnly" "$file"; then
    echo "⚠️  MISSING UICreateOnly: $file"
  fi
done

echo "\n=== BaseEntity Classes Without UIReadOnly on OwningUserId ==="
for file in $(grep -l ": BaseEntity" aspnet/*/Entities/*.cs); do
  if ! grep -q "UIReadOnly.*OwningUserId" "$file"; then
    echo "⚠️  MISSING UIReadOnly(OwningUserId): $file"
  fi
done

echo "\n=== State/Count Fields Without [UIReadOnly] ==="
grep -rE "(Status|State|Count).*\{.*get.*set" aspnet/*/Entities/ | grep -v "UIReadOnly"

echo "\n=== Service Logic Exceptions ==="
grep -r "throw new Exception" aspnet/*/Services/

echo "\n=== ASSIGN_SYSTEM Permissions ==="
grep -r "ASSIGN_SYSTEM" aspnet/*/Startup/
```
