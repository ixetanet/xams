using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xams.Core.Attributes;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Utils;

internal enum AggregateNodeKind
{
    Root,

    // One-to-many: the joined table's foreign key points back at the parent
    Child,

    // Many-to-one: the parent's foreign key points at the joined table's primary key
    Reference,

    // Joined on columns that aren't keys; display or filter data only
    Other
}

/// <summary>
/// The root table or one join of a section's query.
/// </summary>
internal sealed class AggregateNode
{
    internal static readonly string[] SystemFields = ["CreatedDate", "CreatedById", "UpdatedDate", "UpdatedById"];

    public required string Alias { get; init; }
    public required string TableName { get; init; }
    public required Type EntityType { get; init; }
    public required string PrimaryKey { get; init; }
    public required AggregateNodeKind Kind { get; init; }
    public AggregateNode? Parent { get; init; }
    public Join? Join { get; init; }
    public List<AggregateNode> Children { get; } = new();
    public bool Writable { get; set; }

    // EF-mapped scalar properties that have a CLR property
    public Dictionary<string, PropertyInfo> ScalarProperties { get; set; } = new();

    // Fields taken from the submitted data: selected, saveable, not keys, system fields or ReadOnlyFields, and
    // allowed by the table's rules for a create or an update
    public HashSet<string> CreateFields { get; set; } = new();
    public HashSet<string> UpdateFields { get; set; } = new();

    // Properties a business rule may set on an update through AggregateUpdate.Set
    public Dictionary<string, PropertyInfo> SettableProperties { get; set; } = new();

    // Columns an attached instance needs so EF orders and checks the save: key, foreign keys, created and updated
    // dates, and EF concurrency tokens
    public HashSet<string> StubColumns { get; set; } = new();

    // Columns the scope read returns for this node
    public HashSet<string> ReadColumns { get; set; } = new();

    // Whether the caller's query selects "*", and whether its selection includes the primary key
    public bool SelectsAll { get; set; }
    public bool SelectsKey { get; set; }

    // Child: the foreign key on this node that points at the parent
    public string? LinkField => Kind == AggregateNodeKind.Child ? Join!.toField : null;

    // Reference: the foreign key on the parent that points at this node
    public string? ParentForeignKey => Kind == AggregateNodeKind.Reference ? Join!.fromField : null;

    // Prefix of this node's columns in a flat read result
    public string ResultPrefix => Kind == AggregateNodeKind.Root ? string.Empty : $"{Alias}.";
}

/// <summary>
/// One query plus the data submitted for it.
/// </summary>
internal sealed class AggregateSection
{
    public required ServiceQuery Query { get; init; }
    public required bool IsRecord { get; init; }
    public required JsonElement? Submitted { get; init; }
    public required AggregateSectionOptions Options { get; init; }

    public ReadInput ReadInput { get; set; } = null!;
    public ReadInput ScopeReadInput { get; set; } = null!;
    public AggregateNode Root { get; set; } = null!;

    // Parents before children
    public List<AggregateNode> Nodes { get; } = new();

    // Top-level AND equality filters on root fields: stamped onto new root records. A null value means "== null".
    public Dictionary<string, string?> ScopeFilters { get; } = new();

    public string Name => Root?.TableName ?? Query.ToReadInput().tableName;
}

/// <summary>
/// Builds a section's join tree from its query and checks that the query can drive a save.
/// </summary>
internal static class AggregateTree
{
    /// <summary>
    /// Checks each Table&lt;T&gt; rule names a Xams table and only fields the save takes from the client, and
    /// returns the rules by the table's entity type.
    /// </summary>
    internal static Dictionary<Type, AggregateTableRule> ResolveRules(IEnumerable<AggregateTableRule> rules,
        IModel model, List<AggregateError> errors)
    {
        var resolved = new Dictionary<Type, AggregateTableRule>();
        foreach (var rule in rules)
        {
            // The cache also maps each entity's base class to its table, so a base class (even object) would find
            // some table. Only the table's own entity type is unambiguous.
            var name = rule.EntityType.Name;
            if (!Cache.Instance.TableTypeMetadata.TryGetValue(rule.EntityType, out var metadata) ||
                metadata.Type != rule.EntityType || model.FindEntityType(rule.EntityType) is not { } entityType)
            {
                errors.Add(Setup(name, $"Table<{name}>: {name} isn't the entity type of a Xams table in the DbContext model."));
                continue;
            }

            var table = metadata.TableName;
            var discriminator = entityType.FindDiscriminatorProperty()?.Name;
            foreach (var field in rule.Editable.Concat(rule.CreateOnly).Distinct())
            {
                if (entityType.FindProperty(field) is not { } property || !IsSettable(property, discriminator))
                {
                    errors.Add(new AggregateError(AggregateErrorKind.Setup, table, null, field,
                        $"{table}: {field} can't be Editable or CreateOnly, because the save never takes it from the " +
                        "client. It sets keys and system fields itself, and doesn't write unmapped or store-generated columns."));
                }
            }

            foreach (var field in rule.Editable.Where(rule.CreateOnly.Contains))
            {
                errors.Add(new AggregateError(AggregateErrorKind.Setup, table, null, field,
                    $"{table}: {field} is both Editable and CreateOnly. Editable already lets the client set it on create."));
            }

            resolved[rule.EntityType] = rule;
        }

        return resolved;
    }

    internal static void Build(AggregateSection section, IModel model,
        IReadOnlyDictionary<Type, AggregateTableRule> rules, List<AggregateError> errors)
    {
        // A DiffAsync that failed can be retried, so start again from the query
        section.Nodes.Clear();
        section.ScopeFilters.Clear();
        section.Root = null!;

        ReadInput readInput;
        try
        {
            readInput = section.Query.ToReadInput();
        }
        catch (Exception e)
        {
            errors.Add(Setup("", e.Message));
            return;
        }

        section.ReadInput = readInput;
        var table = readInput.tableName;
        int errorCount = errors.Count;

        if (readInput.distinct == true)
        {
            errors.Add(Setup(table, $"{table}: a Distinct query can't be saved, because it merges rows."));
        }

        if (readInput.page > 1)
        {
            errors.Add(Setup(table, $"{table}: the query reads page {readInput.page}. A saved query must read page 1."));
        }

        var root = CreateNode(table, table, AggregateNodeKind.Root, null, null, model, errors);
        if (root == null)
        {
            return;
        }

        root.Writable = true;
        section.Root = root;
        section.Nodes.Add(root);
        var nodesByAlias = new Dictionary<string, AggregateNode>(StringComparer.Ordinal);

        foreach (var join in readInput.joins ?? [])
        {
            var alias = string.IsNullOrEmpty(join.alias) ? join.toTable : join.alias;
            if (alias == table || nodesByAlias.ContainsKey(alias))
            {
                errors.Add(Setup(table, $"{table}: the join alias \"{alias}\" is used twice. Give each join its own alias."));
                continue;
            }

            AggregateNode? parent = join.fromTable == table ? root : nodesByAlias.GetValueOrDefault(join.fromTable);
            if (parent == null)
            {
                errors.Add(Setup(table,
                    $"{table}: the join \"{alias}\" starts from \"{join.fromTable}\", which isn't the root table or an earlier join's alias."));
                continue;
            }

            Cache.MetadataInfo toMetadata;
            try
            {
                toMetadata = Cache.Instance.GetTableMetadata(join.toTable);
            }
            catch (Exception)
            {
                errors.Add(Setup(table, $"{table}: the join \"{alias}\" targets unknown table {join.toTable}."));
                continue;
            }

            var kind = join.toField == toMetadata.PrimaryKey ? AggregateNodeKind.Reference
                : join.fromField == parent.PrimaryKey ? AggregateNodeKind.Child
                : AggregateNodeKind.Other;

            var node = CreateNode(join.toTable, alias, kind, parent, join, model, errors);
            if (node == null)
            {
                continue;
            }

            node.Writable = parent.Writable && kind switch
            {
                AggregateNodeKind.Child => !section.Options.ReadOnlyAliases.Contains(alias),
                AggregateNodeKind.Reference => section.Options.WritableAliases.Contains(alias),
                _ => false
            };

            if (section.Options.WritableAliases.Contains(alias))
            {
                if (kind == AggregateNodeKind.Other)
                {
                    errors.Add(Setup(table,
                        $"{table}: the join \"{alias}\" isn't on a key ({join.fromTable}.{join.fromField} = {join.toTable}.{join.toField}), so it can't be written."));
                }
                else if (!parent.Writable)
                {
                    errors.Add(Setup(table,
                        $"{table}: the join \"{alias}\" can't be writable because the join it hangs off, \"{parent.Alias}\", is read-only."));
                }
            }

            if (node.Writable)
            {
                CheckForeignKey(table, node, parent, model, errors);
            }

            parent.Children.Add(node);
            nodesByAlias[alias] = node;
            section.Nodes.Add(node);
        }

        CheckOptions(section, nodesByAlias, errors);

        if (section.Nodes.Any(x => x.Kind != AggregateNodeKind.Root && x.Writable) && readInput.denormalize != true)
        {
            errors.Add(Setup(table,
                $"{table}: the query has writable joins, so it must call Denormalize(). Flat rows can't show which records belong to which parent."));
        }

        if (errors.Count > errorCount)
        {
            return;
        }

        foreach (var node in section.Nodes)
        {
            var fields = node.Kind == AggregateNodeKind.Root ? readInput.fields : node.Join!.fields;
            SetFields(node, fields ?? [], section.Options, rules.GetValueOrDefault(node.EntityType), model);
        }

        CheckKeysAreSelected(section, errors);
        CheckConcurrencyTokens(section, model, errors);
        if (errors.Count > errorCount)
        {
            return;
        }

        SetScopeFilters(section, readInput.filters);
        section.ScopeReadInput = CreateScopeRead(section);
    }

    private static AggregateNode? CreateNode(string tableName, string alias, AggregateNodeKind kind,
        AggregateNode? parent, Join? join, IModel model, List<AggregateError> errors)
    {
        Cache.MetadataInfo metadata;
        try
        {
            metadata = Cache.Instance.GetTableMetadata(tableName);
        }
        catch (Exception)
        {
            errors.Add(Setup(tableName, $"Table {tableName} isn't a Xams table."));
            return null;
        }

        if (metadata.PrimaryKeyType != typeof(Guid) || metadata.PrimaryKey != $"{tableName}Id")
        {
            errors.Add(Setup(tableName,
                $"{tableName}: AggregateUpdater needs a Guid primary key named {tableName}Id."));
            return null;
        }

        if (model.FindEntityType(metadata.Type) == null)
        {
            errors.Add(Setup(tableName, $"{tableName} isn't in the DbContext model."));
            return null;
        }

        return new AggregateNode
        {
            Alias = alias,
            TableName = tableName,
            EntityType = metadata.Type,
            PrimaryKey = metadata.PrimaryKey,
            Kind = kind,
            Parent = parent,
            Join = join
        };
    }

    private static void CheckForeignKey(string table, AggregateNode node, AggregateNode parent, IModel model,
        List<AggregateError> errors)
    {
        // Child: the joined table holds the foreign key; reference: the parent does
        var (dependent, principal, field) = node.Kind == AggregateNodeKind.Child
            ? (node.EntityType, parent.EntityType, node.Join!.toField)
            : (parent.EntityType, node.EntityType, node.Join!.fromField);

        var hasForeignKey = model.FindEntityType(dependent)!.GetForeignKeys().Any(fk =>
            fk.Properties.Count == 1 && fk.Properties[0].Name == field &&
            fk.PrincipalEntityType.ClrType.IsAssignableFrom(principal));

        if (!hasForeignKey)
        {
            errors.Add(Setup(table,
                $"{table}: the join \"{node.Alias}\" is writable, but the DbContext model has no foreign key {dependent.Name}.{field} → {principal.Name}. EF needs it to order the save."));
        }
    }

    private static void CheckOptions(AggregateSection section, Dictionary<string, AggregateNode> nodesByAlias,
        List<AggregateError> errors)
    {
        var table = section.ReadInput.tableName;
        foreach (var alias in section.Options.WritableAliases.Concat(section.Options.ReadOnlyAliases))
        {
            if (!nodesByAlias.ContainsKey(alias))
            {
                errors.Add(Setup(table, $"{table}: the query has no join with the alias \"{alias}\"."));
            }
        }

        foreach (var field in section.Options.ReadOnlyFieldNames)
        {
            var parts = field.Split('.');
            AggregateNode? node = parts.Length == 1 ? section.Root : nodesByAlias.GetValueOrDefault(parts[0]);
            var name = parts[^1];
            if (node == null || parts.Length > 2)
            {
                errors.Add(Setup(table, $"{table}: ReadOnlyFields(\"{field}\") doesn't name a field of the query."));
                continue;
            }

            if (node.EntityType.GetProperty(name) == null)
            {
                errors.Add(Setup(table, $"{table}: {node.TableName} has no field {name}."));
            }
        }
    }

    private static void SetFields(AggregateNode node, string[] fields, AggregateSectionOptions options,
        AggregateTableRule? rule, IModel model)
    {
        var entityType = model.FindEntityType(node.EntityType)!;
        var discriminator = entityType.FindDiscriminatorProperty()?.Name;

        foreach (var property in entityType.GetProperties())
        {
            if (property.PropertyInfo != null && property.Name != discriminator)
            {
                node.ScalarProperties[property.Name] = property.PropertyInfo;
            }
        }

        var visible = node.EntityType.GetEntityProperties()
            .Where(x => !IsHidden(x) && x.Name != discriminator)
            .Select(x => x.Name)
            .ToHashSet();

        var selectsAll = fields.Contains("*");
        var selected = selectsAll ? visible : fields.ToHashSet();
        node.SelectsAll = selectsAll;
        node.SelectsKey = selected.Contains(node.PrimaryKey);

        var readOnly = options.ReadOnlyFieldNames
            .Select(x => x.Split('.'))
            .Where(x => node.Kind == AggregateNodeKind.Root ? x.Length == 1 : x.Length == 2 && x[0] == node.Alias)
            .Select(x => x[^1])
            .ToHashSet();

        foreach (var property in entityType.GetProperties())
        {
            if (!IsSettable(property, discriminator))
            {
                continue;
            }

            node.SettableProperties[property.Name] = property.PropertyInfo!;

            if (!selected.Contains(property.Name) || property.Name == node.LinkField || readOnly.Contains(property.Name))
            {
                continue;
            }

            if (rule?.ClientWritesOnCreate(property.Name) != false)
            {
                node.CreateFields.Add(property.Name);
            }

            if (rule?.ClientWritesOnUpdate(property.Name) != false)
            {
                node.UpdateFields.Add(property.Name);
            }
        }

        node.StubColumns.Add(node.PrimaryKey);
        foreach (var foreignKey in entityType.GetForeignKeys())
        {
            foreach (var property in foreignKey.Properties.Where(x => x.PropertyInfo != null))
            {
                node.StubColumns.Add(property.Name);
            }
        }

        foreach (var field in new[] { "CreatedDate", "UpdatedDate" })
        {
            if (node.ScalarProperties.ContainsKey(field))
            {
                node.StubColumns.Add(field);
            }
        }

        // EF puts concurrency tokens in the UPDATE and DELETE WHERE clause, so the attached instance needs the
        // values that were read. Without them every write would look like a conflict.
        foreach (var property in entityType.GetProperties().Where(x => x.IsConcurrencyToken && x.PropertyInfo != null))
        {
            node.StubColumns.Add(property.Name);
        }

        // The scope read needs each node's key and link columns to build the tree, plus the stub columns when
        // they can be selected. A "*" selection can't be combined with explicit fields and already has them.
        node.ReadColumns = selected.Where(x => node.ScalarProperties.ContainsKey(x)).ToHashSet();
        if (!selectsAll)
        {
            node.ReadColumns.Add(node.PrimaryKey);
            if (node.Join != null)
            {
                node.ReadColumns.Add(node.Join.toField);
            }

            foreach (var child in node.Children)
            {
                node.ReadColumns.Add(child.Join!.fromField);
            }

            foreach (var column in node.StubColumns)
            {
                if (!IsHidden(node.EntityType.GetProperty(column)))
                {
                    node.ReadColumns.Add(column);
                }
            }
        }
    }

    // A record sent back without its key looks new, so the stored record it came from would look left out: the
    // save would delete it and insert a copy under a new key. The Get query must return every saved table's key.
    private static void CheckKeysAreSelected(AggregateSection section, List<AggregateError> errors)
    {
        var table = section.Root.TableName;
        foreach (var node in section.Nodes.Where(x => x.Writable && !x.SelectsKey))
        {
            var selection = node.Kind == AggregateNodeKind.Root ? "the query" : $"the \"{node.Alias}\" join";
            var fix = node.SelectsAll
                ? $"{node.PrimaryKey} has [UIHide], so \"*\" can't return it."
                : $"Add {node.PrimaryKey} to {selection}'s fields or select \"*\".";
            var alternative = node.Kind switch
            {
                AggregateNodeKind.Child => $" Or make the join read-only with ReadOnly(\"{node.Alias}\").",
                AggregateNodeKind.Reference => $" Or leave out Writable(\"{node.Alias}\").",
                _ => ""
            };

            errors.Add(Setup(table,
                $"{table}: {selection} doesn't select {node.TableName}'s key, {node.PrimaryKey}. The records it returns " +
                $"couldn't be matched to stored ones when saved, so they would be deleted and re-created. {fix}{alternative}"));
        }
    }

    // An attached instance gets its original values through CLR properties. A shadow concurrency token (one without
    // a property) would keep its default, so EF would compare against the wrong value and report a conflict.
    private static void CheckConcurrencyTokens(AggregateSection section, IModel model, List<AggregateError> errors)
    {
        var table = section.Root.TableName;
        foreach (var node in section.Nodes.Where(x => x.Writable))
        {
            var token = model.FindEntityType(node.EntityType)!.GetProperties()
                .FirstOrDefault(x => x.IsConcurrencyToken && x.PropertyInfo == null);
            if (token != null)
            {
                errors.Add(Setup(table,
                    $"{table}: {node.TableName}.{token.Name} is a concurrency token without a property on {node.EntityType.Name}, " +
                    "so the save can't give EF the value it was read with. Map it to a property."));
            }
        }
    }

    // A field the save may write: mapped to a CLR property, saved by EF on insert and update, and not the key, the
    // discriminator or a system field the save sets itself
    private static bool IsSettable(IProperty property, string? discriminator) =>
        property.PropertyInfo != null && !property.IsPrimaryKey() && property.Name != discriminator &&
        property.GetBeforeSaveBehavior() == PropertySaveBehavior.Save &&
        property.GetAfterSaveBehavior() == PropertySaveBehavior.Save &&
        !AggregateNode.SystemFields.Contains(property.Name);

    private static bool IsHidden(PropertyInfo? property) =>
        property != null && Attribute.GetCustomAttribute(property, typeof(UIHideAttribute)) != null;

    private static void SetScopeFilters(AggregateSection section, Filter[]? filters)
    {
        foreach (var filter in filters ?? [])
        {
            if (!string.IsNullOrEmpty(filter.logicalOperator))
            {
                if (string.Equals(filter.logicalOperator, "AND", StringComparison.OrdinalIgnoreCase))
                {
                    SetScopeFilters(section, filter.filters);
                }

                continue;
            }

            if (filter.field != null && !filter.field.Contains('.') && filter.@operator == "==" &&
                section.Root.ScalarProperties.ContainsKey(filter.field))
            {
                section.ScopeFilters[filter.field] = filter.value;
            }
        }
    }

    // The same rows as the caller's query, read flat, with the columns the diff needs
    private static ReadInput CreateScopeRead(AggregateSection section)
    {
        var readInput = section.ReadInput;
        var nodesByJoin = section.Nodes.Where(x => x.Join != null).ToDictionary(x => x.Join!);

        string[] Columns(AggregateNode node, string[]? fields) =>
            fields != null && fields.Contains("*") ? fields : node.ReadColumns.ToArray();

        return new ReadInput
        {
            tableName = readInput.tableName,
            fields = Columns(section.Root, readInput.fields),
            page = 1,
            maxResults = readInput.maxResults,
            filters = readInput.filters,
            joins = readInput.joins?.Select(join => new Join
            {
                type = join.type,
                alias = join.alias,
                fromTable = join.fromTable,
                fromField = join.fromField,
                toTable = join.toTable,
                toField = join.toField,
                filters = join.filters,
                fields = nodesByJoin.TryGetValue(join, out var node) ? Columns(node, join.fields) : join.fields
            }).ToArray(),
            except = readInput.except,
            parameters = readInput.parameters,
            distinct = false,
            denormalize = false
        };
    }

    private static AggregateError Setup(string table, string message) =>
        new(AggregateErrorKind.Setup, table, null, null, message);
}
