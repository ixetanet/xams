using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;
using Xams.Core.Repositories;

namespace Xams.Core.Utils;

/// <summary>
/// Reads the rows each section's query selects, compares them with the submitted data, and plans the writes.
/// </summary>
internal sealed class AggregateDiff
{
    private readonly BaseServiceContext _context;
    private readonly DateTime _now;
    private readonly DateTime? _loadedAt;
    private readonly bool _ignoreConflicts;
    private readonly IReadOnlyDictionary<Type, AggregateTableRule> _rules;
    private readonly List<AggregateError> _errors = new();
    private readonly Dictionary<(string Table, Guid Id), Planned> _planned = new();
    private readonly List<Planned> _order = new();
    private readonly List<ReferenceCheck> _referenceChecks = new();

    internal AggregateDiff(BaseServiceContext context, DateTime now, DateTime? loadedAt, bool ignoreConflicts,
        IReadOnlyDictionary<Type, AggregateTableRule> rules)
    {
        _context = context;
        _now = now;
        _loadedAt = loadedAt;
        _ignoreConflicts = ignoreConflicts;
        _rules = rules;
    }

    internal async Task<AggregateChangeSet> RunAsync(List<AggregateSection> sections)
    {
        var states = new List<SectionState>();
        foreach (var section in sections)
        {
            var roots = ParseSubmitted(section);
            if (roots != null)
            {
                states.Add(new SectionState(section, roots));
            }
        }

        ThrowIfErrors();

        foreach (var state in states)
        {
            await ReadCurrent(state);
        }

        ThrowIfErrors();

        foreach (var state in states)
        {
            Diff(state);
        }

        ThrowIfErrors();

        BuildCreates();
        await RunCreateHooks();
        ThrowIfErrors();

        CheckCreatedScopes();
        CheckReferences();
        ThrowIfErrors();

        await CheckNewIdsAreUnused();
        ThrowIfErrors();

        await LoadStubColumns();
        CheckConflicts();
        ThrowIfErrors();

        return BuildChangeSet();
    }

    private void ThrowIfErrors()
    {
        if (_errors.Count > 0)
        {
            throw new AggregateUpdateException(_errors.ToList());
        }
    }

    // ---- Submitted data ----

    private List<SubmittedRecord>? ParseSubmitted(AggregateSection section)
    {
        var table = section.Root.TableName;
        if (section.Submitted is not { } element ||
            element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (section.IsRecord)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                _errors.Add(Error(AggregateErrorKind.Invalid, table, null, null,
                    $"{table}: expected a single record (a JSON object), got {element.ValueKind}."));
                return null;
            }

            var record = ParseRecord(section.Root, element, null);
            return record == null ? [] : [record];
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            _errors.Add(Error(AggregateErrorKind.Invalid, table, null, null,
                $"{table}: expected a list of records (a JSON array), got {element.ValueKind}."));
            return null;
        }

        var records = new List<SubmittedRecord>();
        foreach (var item in element.EnumerateArray())
        {
            var record = ParseRecord(section.Root, item, null);
            if (record != null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private SubmittedRecord? ParseRecord(AggregateNode node, JsonElement element, SubmittedRecord? parent)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, null, null,
                $"{node.TableName}: each record must be a JSON object, got {element.ValueKind}."));
            return null;
        }

        var record = new SubmittedRecord { Node = node, Parent = parent };
        var arrays = new List<(AggregateNode Node, JsonElement Items)>();
        foreach (var property in element.EnumerateObject())
        {
            var child = node.Children.FirstOrDefault(x => x.Alias == property.Name);
            if (child != null)
            {
                if (!child.Writable || property.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
                {
                    continue;
                }

                if (property.Value.ValueKind != JsonValueKind.Array)
                {
                    _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, null, property.Name,
                        $"{node.TableName}: \"{property.Name}\" must be a JSON array of {child.TableName} records."));
                    continue;
                }

                arrays.Add((child, property.Value));
                continue;
            }

            if (node.ScalarProperties.ContainsKey(property.Name))
            {
                record.Fields[property.Name] = property.Value;
            }
        }

        // A root or child record without a key is new; a reference record must say which record it is
        var key = record.Fields.TryGetValue(node.PrimaryKey, out var keyElement) ? keyElement.ToString() : "";
        if (Guid.TryParse(key, out var id) && id != Guid.Empty)
        {
            record.Id = id;
        }
        else if (string.IsNullOrWhiteSpace(key) || Guid.TryParse(key, out _))
        {
            if (node.Kind == AggregateNodeKind.Reference)
            {
                _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, null, node.PrimaryKey,
                    $"{node.TableName}: a record in \"{node.Alias}\" needs its {node.PrimaryKey}."));
                return null;
            }

            record.Id = Guid.NewGuid();
            record.IdAssigned = true;
        }
        else
        {
            _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, null, node.PrimaryKey,
                $"{node.TableName}: {node.PrimaryKey} \"{key}\" isn't a Guid."));
            return null;
        }

        foreach (var (child, items) in arrays)
        {
            var list = new List<SubmittedRecord>();
            foreach (var item in items.EnumerateArray())
            {
                var childRecord = ParseRecord(child, item, record);
                if (childRecord != null)
                {
                    list.Add(childRecord);
                }
            }

            record.Arrays[child] = list;
        }

        return record;
    }

    // ---- Current data ----

    private async Task ReadCurrent(SectionState state)
    {
        var section = state.Section;
        var table = section.Root.TableName;
        var response = await _context.DataRepository.Read(_context.ExecutingUserId, section.ScopeReadInput,
            new ReadOptions { Permissions = [], BypassSecurity = true });

        if (!response.Succeeded || response.Data == null)
        {
            _errors.Add(Error(AggregateErrorKind.Setup, table, null, null,
                $"{table}: the query couldn't be read: {response.FriendlyMessage}"));
            return;
        }

        var output = response.Data;
        if (output.pages > 1)
        {
            _errors.Add(Error(AggregateErrorKind.Setup, table, null, null,
                $"{table}: the query selects {output.totalResults} rows but reads only {output.maxResults}. " +
                "Raise its Top so it reads every row; rows it doesn't read would look deleted."));
            return;
        }

        foreach (var node in section.Nodes.Where(x => x.Writable))
        {
            state.Current[node] = new Dictionary<Guid, CurrentRecord>();
        }

        foreach (var row in output.results.Cast<IDictionary<string, object?>>())
        {
            var ids = new Dictionary<AggregateNode, Guid?>();
            foreach (var node in section.Nodes.Where(x => x.Writable))
            {
                var id = AsGuid(row.TryGetValue(node.ResultPrefix + node.PrimaryKey, out var value) ? value : null);
                ids[node] = id;
                if (id == null || state.Current[node].ContainsKey(id.Value))
                {
                    continue;
                }

                var values = new Dictionary<string, object?>();
                foreach (var (key, fieldValue) in row)
                {
                    if (node.Kind == AggregateNodeKind.Root ? !key.Contains('.') : key.StartsWith(node.ResultPrefix))
                    {
                        values[key.Substring(node.ResultPrefix.Length)] = fieldValue;
                    }
                }

                state.Current[node][id.Value] = new CurrentRecord
                {
                    Id = id.Value,
                    ParentId = node.Parent != null ? ids.GetValueOrDefault(node.Parent) : null,
                    Values = values
                };
            }
        }

        foreach (var (node, records) in state.Current)
        {
            state.ChildrenByParent[node] = records.Values.Where(x => x.ParentId != null)
                .ToLookup(x => x.ParentId!.Value);
        }
    }

    // ---- Diff ----

    private void Diff(SectionState state)
    {
        var section = state.Section;
        var root = section.Root;
        Guid? keyFilter = section.ScopeFilters.TryGetValue(root.PrimaryKey, out var keyValue) &&
                          Guid.TryParse(keyValue, out var parsedKey)
            ? parsedKey
            : null;

        foreach (var record in state.Roots)
        {
            if (keyFilter != null)
            {
                if (record.IdAssigned)
                {
                    record.Id = keyFilter.Value;
                    record.IdAssigned = false;
                }
                else if (record.Id != keyFilter.Value)
                {
                    _errors.Add(Error(AggregateErrorKind.OutOfScope, root.TableName, record.Id, root.PrimaryKey,
                        $"{root.TableName} {record.Id} isn't the record the query selects ({keyFilter})."));
                    continue;
                }
            }

            ProcessRecord(state, record);
        }

        // Rows the query selects that the submitted data left out are deleted
        if (!section.IsRecord)
        {
            foreach (var current in state.Current[root].Values)
            {
                if (!state.Submitted(root).ContainsKey(current.Id))
                {
                    DeleteRecursive(state, root, current);
                }
            }
        }

        // A child an array leaves out is deleted, unless it was moved under another parent. A parent that is a
        // reference shared by several records has one array per copy, and every copy must keep the same children.
        foreach (var node in section.Nodes.Where(x => x.Writable && x.Kind == AggregateNodeKind.Child))
        {
            var arrays = state.Arrays(node);
            var submitted = state.Submitted(node);
            foreach (var current in state.Current[node].Values)
            {
                if (current.ParentId is not { } parentId || !arrays.TryGetValue(parentId, out var copies) ||
                    copies.All(x => x.Contains(current.Id)))
                {
                    continue;
                }

                if (!submitted.TryGetValue(current.Id, out var kept))
                {
                    DeleteRecursive(state, node, current);
                }
                else if (kept.Parent!.Id == parentId)
                {
                    _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, current.Id, null,
                        $"{node.TableName} {current.Id} is deleted in one place and kept in another."));
                }
            }
        }
    }

    private void ProcessRecord(SectionState state, SubmittedRecord record)
    {
        var node = record.Node;
        var submitted = state.Submitted(node);
        if (submitted.TryGetValue(record.Id, out var first))
        {
            // Each copy of a reference shared by several records repeats the records nested under it. Those are
            // copies of one record and are merged; any other repeat is an error.
            var isCopy = first.Parent != null && record.Parent != null && first.Parent != record.Parent &&
                         first.Parent.Id == record.Parent.Id;
            if (!isCopy)
            {
                _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, record.Id, null,
                    $"{node.TableName} {record.Id} appears more than once in the submitted data."));
                return;
            }
        }
        else
        {
            submitted[record.Id] = record;
        }

        PlanRecord(state, record);
        ProcessArrays(state, record);
    }

    private void ProcessArrays(SectionState state, SubmittedRecord record)
    {
        foreach (var (child, items) in record.Arrays)
        {
            if (child.Kind == AggregateNodeKind.Reference)
            {
                ProcessReference(state, record, child, items);
                continue;
            }

            state.ArraysOf(child, record.Id).Add(items.Select(x => x.Id).ToHashSet());
            foreach (var item in items)
            {
                ProcessRecord(state, item);
            }
        }
    }

    private void PlanRecord(SectionState state, SubmittedRecord record)
    {
        var node = record.Node;
        var current = state.Current[node].GetValueOrDefault(record.Id);
        var errorCount = _errors.Count;

        if (current != null)
        {
            var changes = new Dictionary<string, AggregateFieldChange>();
            foreach (var field in node.UpdateFields)
            {
                if (!record.Fields.TryGetValue(field, out var element) ||
                    !TryConvert(node, record.Id, field, element, out var value))
                {
                    continue;
                }

                var old = current.Values.GetValueOrDefault(field);
                if (!current.Values.ContainsKey(field) ||
                    !AggregateValueComparer.AreEqual(node.ScalarProperties[field].PropertyType, old, value))
                {
                    changes[field] = new AggregateFieldChange(old, value);
                }
            }

            if (node.Kind == AggregateNodeKind.Child && current.ParentId != record.Parent!.Id)
            {
                // A move writes the link field, so a table whose rules don't let the client change it can't move.
                // It's an error rather than ignored: a move is never an echo of what the client read.
                if (_rules.GetValueOrDefault(node.EntityType)?.ClientWritesOnUpdate(node.LinkField!) == false)
                {
                    _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, record.Id, node.LinkField,
                        $"{node.TableName} {record.Id} can't be moved from {node.Parent!.TableName} {current.ParentId} " +
                        $"to {record.Parent.Id}, because the table's rules don't let the client change {node.LinkField}."));
                }
                else
                {
                    changes[node.LinkField!] = new AggregateFieldChange(current.ParentId, record.Parent.Id);
                }
            }

            if (node.Kind == AggregateNodeKind.Root)
            {
                CheckScope(state.Section, record, changes);
            }

            DateTime? token = null;
            if (!_ignoreConflicts && record.Fields.TryGetValue("UpdatedDate", out var tokenElement) &&
                node.ScalarProperties.TryGetValue("UpdatedDate", out var tokenProperty))
            {
                token = ReadToken(node, record.Id, tokenProperty, tokenElement);
            }

            if (_errors.Count > errorCount)
            {
                return;
            }

            var planned = new Planned
            {
                Node = node,
                Id = record.Id,
                Kind = changes.Count > 0 ? PlannedKind.Update : PlannedKind.Keep,
                Changes = changes,
                Current = current.Values
            };

            // Only a copy that changes the record has to have loaded its current version
            if (changes.Count > 0 && token != null)
            {
                planned.Tokens.Add(token.Value);
            }

            Plan(planned);
            return;
        }

        var values = new Dictionary<string, object?>();
        foreach (var field in node.CreateFields)
        {
            if (record.Fields.TryGetValue(field, out var element) &&
                TryConvert(node, record.Id, field, element, out var value))
            {
                values[field] = value;
            }
        }

        if (node.Kind == AggregateNodeKind.Child)
        {
            values[node.LinkField!] = record.Parent!.Id;
        }

        if (node.Kind == AggregateNodeKind.Root)
        {
            StampScope(state.Section, record, values);
        }

        if (_errors.Count > errorCount)
        {
            return;
        }

        var create = new Planned
        {
            Node = node,
            Id = record.Id,
            Kind = PlannedKind.Create,
            Values = values,
            ClientSuppliedId = !record.IdAssigned
        };

        if (node.Kind == AggregateNodeKind.Child)
        {
            create.Links[node.LinkField!] = record.Parent!.Id;
        }

        if (node.Kind == AggregateNodeKind.Root)
        {
            create.Sections.Add(state.Section);
        }

        Plan(create);
    }

    private void ProcessReference(SectionState state, SubmittedRecord parent, AggregateNode node,
        List<SubmittedRecord> items)
    {
        var parentNode = parent.Node;
        if (items.Count > 1)
        {
            _errors.Add(Error(AggregateErrorKind.Invalid, parentNode.TableName, parent.Id, node.Alias,
                $"{parentNode.TableName} {parent.Id}: \"{node.Alias}\" can hold at most one {node.TableName} record."));
            return;
        }

        // Whether the parent points at this record is known only once every record is planned
        var referenced = items.FirstOrDefault();
        _referenceChecks.Add(new ReferenceCheck(parentNode, parent.Id, node, referenced?.Id));
        if (referenced == null)
        {
            return;
        }

        // A reference shared by several records arrives once per record. Every copy is planned, nested arrays
        // included, and Plan merges them, so an edit counts wherever it was made and different edits conflict.
        PlanRecord(state, referenced);
        ProcessArrays(state, referenced);
    }

    private void DeleteRecursive(SectionState state, AggregateNode node, CurrentRecord record)
    {
        foreach (var child in node.Children.Where(x => x.Writable && x.Kind == AggregateNodeKind.Child))
        {
            foreach (var childRecord in state.ChildrenByParent[child][record.Id])
            {
                if (!state.Submitted(child).ContainsKey(childRecord.Id))
                {
                    DeleteRecursive(state, child, childRecord);
                }
            }
        }

        Plan(new Planned
        {
            Node = node,
            Id = record.Id,
            Kind = PlannedKind.Delete,
            Current = record.Values
        });
    }

    private void CheckScope(AggregateSection section, SubmittedRecord record,
        Dictionary<string, AggregateFieldChange> changes)
    {
        foreach (var field in section.ScopeFilters.Keys.Where(changes.ContainsKey))
        {
            _errors.Add(Error(AggregateErrorKind.OutOfScope, record.Node.TableName, record.Id, field,
                $"{record.Node.TableName} {record.Id}: changing {field} would move it out of the rows the query selects."));
        }
    }

    private void StampScope(AggregateSection section, SubmittedRecord record, Dictionary<string, object?> values)
    {
        var node = record.Node;
        foreach (var (field, filterValue) in section.ScopeFilters)
        {
            if (field == node.PrimaryKey)
            {
                continue;
            }

            var property = node.ScalarProperties[field];
            if (!TryScopeValue(property, filterValue, out var scopeValue))
            {
                continue;
            }

            if (values.TryGetValue(field, out var submitted) &&
                !AggregateValueComparer.AreEqual(property.PropertyType, scopeValue, submitted))
            {
                _errors.Add(Error(AggregateErrorKind.OutOfScope, node.TableName, record.Id, field,
                    $"{node.TableName} {record.Id}: {field} must be {filterValue ?? "null"} to be part of the rows the query selects."));
                continue;
            }

            values[field] = scopeValue;
        }
    }

    // The typed value of an == filter. A null filter value matches null, which only a nullable property can hold.
    private static bool TryScopeValue(PropertyInfo property, string? filterValue, out object? value)
    {
        value = null;
        if (filterValue == null)
        {
            return !property.PropertyType.IsValueType || Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        try
        {
            return EntityUtil.ConvertValue(property, filterValue, out value) == ValueConversion.Set;
        }
        catch (Exception e) when (e is ValueConversionException or FormatException)
        {
            return false;
        }
    }

    // ---- Merging records planned more than once ----

    private void Plan(Planned incoming)
    {
        var key = (incoming.Node.TableName, incoming.Id);
        if (!_planned.TryGetValue(key, out var existing))
        {
            _planned[key] = incoming;
            _order.Add(incoming);
            return;
        }

        var table = incoming.Node.TableName;
        var id = incoming.Id;
        if (existing.Kind == PlannedKind.Delete && incoming.Kind == PlannedKind.Delete)
        {
            return;
        }

        if (existing.Kind == PlannedKind.Delete || incoming.Kind == PlannedKind.Delete)
        {
            _errors.Add(Error(AggregateErrorKind.Invalid, table, id, null,
                $"{table} {id} is deleted in one place and kept in another."));
            return;
        }

        if ((existing.Kind == PlannedKind.Create) != (incoming.Kind == PlannedKind.Create))
        {
            _errors.Add(Error(AggregateErrorKind.OutOfScope, table, id, null,
                $"{table} {id} is new to one query but already exists in another's rows."));
            return;
        }

        if (existing.Kind == PlannedKind.Create)
        {
            foreach (var (field, value) in incoming.Values)
            {
                if (existing.Values.TryGetValue(field, out var other) &&
                    !AggregateValueComparer.AreEqual(incoming.Node.ScalarProperties[field].PropertyType, other, value))
                {
                    AddEditedTwice(table, id, field);
                    continue;
                }

                existing.Values[field] = value;
            }

            existing.Sections.AddRange(incoming.Sections);
            // A conflicting link was already reported with the values
            foreach (var (field, parentId) in incoming.Links)
            {
                existing.Links.TryAdd(field, parentId);
            }

            return;
        }

        foreach (var (field, change) in incoming.Changes)
        {
            if (existing.Changes.TryGetValue(field, out var other) &&
                !AggregateValueComparer.AreEqual(incoming.Node.ScalarProperties[field].PropertyType, other.New, change.New))
            {
                AddEditedTwice(table, id, field);
                continue;
            }

            existing.Changes[field] = change;
        }

        existing.Tokens.AddRange(incoming.Tokens);
        existing.Kind = existing.Changes.Count > 0 ? PlannedKind.Update : PlannedKind.Keep;
    }

    private void AddEditedTwice(string table, Guid id, string field) =>
        _errors.Add(Error(AggregateErrorKind.Conflict, table, id, field,
            $"{table} {id}: {field} is submitted with different values in two places."));

    // ---- Checks once every record is planned ----

    // Builds the instances to insert, defaults included
    private void BuildCreates()
    {
        foreach (var planned in _order.Where(x => x.Kind == PlannedKind.Create))
        {
            planned.Entity = CreateEntity(planned.Node, planned);
        }
    }

    // Gives each table's OnCreate hooks every record the save creates in it, in one call per hook. Rejections are
    // collected so every hook still runs. A hook can reach any new record through Created<T>(), so once they have
    // all run, every new record's key and parent links are checked, in tables with hooks or without.
    private async Task RunCreateHooks()
    {
        var creates = _order.Where(x => x.Kind == PlannedKind.Create).ToList();
        var hooked = creates.Select(x => x.Node.EntityType).Distinct()
            .Where(x => _rules.TryGetValue(x, out var rule) && rule.OnCreate.Count > 0)
            .ToList();
        if (hooked.Count == 0)
        {
            return;
        }

        var created = creates.Select(x => x.Entity!).ToList();
        foreach (var type in HookOrder(hooked))
        {
            var planned = creates.Where(x => x.Node.EntityType == type).ToList();
            var records = planned.Select(x => x.Entity!).ToList();
            var context = new AggregateCreateContext(planned[0].Node.TableName,
                planned.ToDictionary(x => x.Entity!, x => x.Id, ReferenceEqualityComparer.Instance), created, _errors);
            foreach (var hook in _rules[type].OnCreate)
            {
                await hook(records, context);
            }
        }

        foreach (var planned in creates)
        {
            CheckHookKeptKeys(planned);
        }
    }

    // Principal tables first: a table's hooks run after those of every table it depends on through foreign keys,
    // directly or through other tables (with hooks or without), so a new record's hook sees its new ancestors filled
    // in. Otherwise, and in a cycle, the first table planned goes first.
    private List<Type> HookOrder(List<Type> types)
    {
        var model = _context.GetDbContext<IXamsDbContext>().Model;
        var principals = types.ToDictionary(type => type,
            type => DependsOn(model, type).Where(x => x != type && types.Contains(x)).ToHashSet());

        var ordered = new List<Type>();
        var remaining = types.ToList();
        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(x => !principals[x].Overlaps(remaining)) ?? remaining[0];
            ordered.Add(next);
            remaining.Remove(next);
        }

        return ordered;
    }

    // Every entity type a type reaches through foreign keys, directly or through other tables
    private static HashSet<Type> DependsOn(IModel model, Type type)
    {
        var reached = new HashSet<Type>();
        var pending = new Stack<Type>([type]);
        while (pending.Count > 0)
        {
            var entityType = model.FindEntityType(pending.Pop());
            if (entityType == null)
            {
                continue;
            }

            foreach (var principal in entityType.GetForeignKeys().Select(x => x.PrincipalEntityType.ClrType))
            {
                if (reached.Add(principal))
                {
                    pending.Push(principal);
                }
            }
        }

        return reached;
    }

    // The key and the link to each parent the record was nested under come from the submitted data and the nesting;
    // the rest of the save relies on them
    private void CheckHookKeptKeys(Planned planned)
    {
        var node = planned.Node;
        var fields = new List<(string Field, Guid? Expected)> { (node.PrimaryKey, planned.Id) };
        fields.AddRange(planned.Links.Select(x => (x.Key, (Guid?)x.Value)));

        foreach (var (field, expected) in fields)
        {
            if (AsGuid(node.ScalarProperties[field].GetValue(planned.Entity)) != expected)
            {
                _errors.Add(Error(AggregateErrorKind.Setup, node.TableName, planned.Id, field,
                    $"{node.TableName} {planned.Id}: an OnCreate hook changed {field}. A hook can't change a new " +
                    "record's key or its link to its parent."));
            }
        }
    }

    // A new root record must still be one of its query's rows after the defaults and any OnCreate hook
    private void CheckCreatedScopes()
    {
        foreach (var planned in _order.Where(x => x.Kind == PlannedKind.Create))
        {
            foreach (var section in planned.Sections)
            {
                CheckCreatedScope(section, planned);
            }
        }
    }

    // A default such as IsActive = true can contradict a filter such as IsActive == false
    private void CheckCreatedScope(AggregateSection section, Planned planned)
    {
        var node = planned.Node;
        foreach (var (field, filterValue) in section.ScopeFilters)
        {
            var property = node.ScalarProperties[field];
            if (field == node.PrimaryKey || !TryScopeValue(property, filterValue, out var scopeValue))
            {
                continue;
            }

            var value = property.GetValue(planned.Entity);
            if (!AggregateValueComparer.AreEqual(property.PropertyType, scopeValue, value))
            {
                _errors.Add(Error(AggregateErrorKind.OutOfScope, node.TableName, planned.Id, field,
                    $"{node.TableName} {planned.Id}: a new record gets {field} = {value ?? "null"}, so it wouldn't be one of " +
                    $"the rows the query selects ({field} == {filterValue ?? "null"})."));
            }
        }
    }

    // A writable reference must hold the record the parent's foreign key points at after the save. That is the
    // submitted key only when the save writes it; otherwise it is the stored key, or the one nesting, the query's
    // filters or the create defaults set.
    private void CheckReferences()
    {
        foreach (var check in _referenceChecks)
        {
            var parentNode = check.ParentNode;
            if (!_planned.TryGetValue((parentNode.TableName, check.ParentId), out var parent) ||
                parent.Kind == PlannedKind.Delete)
            {
                continue;
            }

            var foreignKey = check.Node.ParentForeignKey!;
            Guid? pointsAt;
            if (parent.Kind == PlannedKind.Create)
            {
                pointsAt = parentNode.ScalarProperties.TryGetValue(foreignKey, out var property)
                    ? AsGuid(property.GetValue(parent.Entity))
                    : null;
            }
            else
            {
                pointsAt = AsGuid(parent.Changes.TryGetValue(foreignKey, out var change)
                    ? change.New
                    : parent.Current!.GetValueOrDefault(foreignKey));
            }

            if (pointsAt != check.ReferencedId)
            {
                _errors.Add(Error(AggregateErrorKind.Invalid, parentNode.TableName, check.ParentId, foreignKey,
                    $"{parentNode.TableName} {check.ParentId}: {foreignKey} is {pointsAt?.ToString() ?? "empty"} after the save but " +
                    $"\"{check.Node.Alias}\" holds {check.ReferencedId?.ToString() ?? "no record"}. They must point at the same " +
                    $"{check.Node.TableName}."));
            }
        }
    }

    // ---- Checks that need the database ----

    // A client-supplied key that already exists belongs to a row outside the query
    private async Task CheckNewIdsAreUnused()
    {
        var groups = _order.Where(x => x.Kind == PlannedKind.Create && x.ClientSuppliedId)
            .GroupBy(x => x.Node.TableName);
        foreach (var group in groups)
        {
            var node = group.First().Node;
            var rows = await FindRows(group.Key, group.Select(x => (object)x.Id).ToArray(), [node.PrimaryKey]);
            foreach (var row in rows)
            {
                var id = AsGuid(row.GetValueOrDefault(node.PrimaryKey));
                _errors.Add(Error(AggregateErrorKind.OutOfScope, group.Key, id, null,
                    $"{group.Key} {id} already exists but isn't one of the rows the query selects."));
            }
        }
    }

    // Hidden columns can't be selected by the scope read, so load them by key for the rows being attached
    private async Task LoadStubColumns()
    {
        var groups = _order.Where(x => x.Kind is PlannedKind.Update or PlannedKind.Delete)
            .Where(x => x.Node.StubColumns.Any(column => !x.Current!.ContainsKey(column)))
            .GroupBy(x => x.Node.TableName);
        foreach (var group in groups)
        {
            var node = group.First().Node;
            var columns = node.StubColumns.ToArray();
            var rows = await FindRows(group.Key, group.Select(x => (object)x.Id).ToArray(), columns);
            var byId = rows.ToDictionary(x => AsGuid(x.GetValueOrDefault(node.PrimaryKey)) ?? Guid.Empty);
            foreach (var planned in group)
            {
                if (!byId.TryGetValue(planned.Id, out var row))
                {
                    continue;
                }

                foreach (var column in columns)
                {
                    planned.Current!.TryAdd(column, row.GetValueOrDefault(column));
                }
            }
        }
    }

    private void CheckConflicts()
    {
        if (_ignoreConflicts)
        {
            return;
        }

        foreach (var planned in _order)
        {
            var table = planned.Node.TableName;
            // Every copy that changed the record was checked separately, so one up-to-date copy can't vouch for a stale one
            if (planned.Kind == PlannedKind.Update &&
                planned.Current!.GetValueOrDefault("UpdatedDate") is DateTime stored &&
                planned.Tokens.Any(token => !AggregateValueComparer.DatesEqual(stored, token)))
            {
                _errors.Add(Error(AggregateErrorKind.Conflict, table, planned.Id, "UpdatedDate",
                    $"{table} {planned.Id} was changed by someone else after it was loaded."));
            }

            if (planned.Kind == PlannedKind.Delete && _loadedAt != null)
            {
                var loadedAt = AggregateValueComparer.TruncateToMilliseconds(_loadedAt.Value);
                bool After(string field) => planned.Current!.GetValueOrDefault(field) is DateTime value &&
                                            AggregateValueComparer.TruncateToMilliseconds(value) > loadedAt;
                if (After("CreatedDate") || After("UpdatedDate"))
                {
                    _errors.Add(Error(AggregateErrorKind.Conflict, table, planned.Id, null,
                        $"{table} {planned.Id} was added or changed by someone else after it was loaded, so it isn't deleted."));
                }
            }
        }
    }

    private async Task<List<Dictionary<string, object?>>> FindRows(string table, object[] ids, string[] columns)
    {
        var response = await _context.DataRepository.Find(table, ids, false, columns, true);
        if (!response.Succeeded || response.Data == null)
        {
            throw new AggregateUpdateException(Error(AggregateErrorKind.Database, table, null, null,
                $"{table}: couldn't read rows by key: {response.FriendlyMessage}"));
        }

        return response.Data.results.Cast<IDictionary<string, object?>>()
            .Select(x => new Dictionary<string, object?>(x)).ToList();
    }

    // ---- Change set ----

    private AggregateChangeSet BuildChangeSet()
    {
        var creates = new List<AggregateCreate>();
        var updates = new List<AggregateUpdate>();
        var deletes = new List<AggregateDelete>();
        foreach (var planned in _order)
        {
            var node = planned.Node;
            switch (planned.Kind)
            {
                case PlannedKind.Create:
                    creates.Add(new AggregateCreate(node.TableName, planned.Id, planned.Entity!));
                    break;
                case PlannedKind.Update:
                    updates.Add(new AggregateUpdate(node.TableName, planned.Id, node.EntityType, planned.Current!,
                        planned.Changes, node.SettableProperties));
                    break;
                case PlannedKind.Delete:
                    deletes.Add(new AggregateDelete(node.TableName, planned.Id, node.EntityType, planned.Current!));
                    break;
            }
        }

        return new AggregateChangeSet(creates, updates, deletes);
    }

    private object CreateEntity(AggregateNode node, Planned planned)
    {
        var entity = Activator.CreateInstance(node.EntityType)
                     ?? throw new InvalidOperationException($"Couldn't create a {node.EntityType.Name}.");
        node.ScalarProperties[node.PrimaryKey].SetValue(entity, planned.Id);
        foreach (var (field, value) in planned.Values)
        {
            node.ScalarProperties[field].SetValue(entity, value);
        }

        SetCreateDefaults(entity, _context.ExecutingUserId, _now);
        return entity;
    }

    // Mirrors PipeSetDefaultFields for a create
    internal static void SetCreateDefaults(object entity, Guid userId, DateTime now)
    {
        var type = entity.GetType();
        var owningUser = type.GetProperty("OwningUserId");
        var owningTeam = type.GetProperty("OwningTeamId");
        if (owningUser?.GetValue(entity) == null && owningTeam?.GetValue(entity) == null)
        {
            owningUser?.SetValue(entity, userId);
        }

        type.GetProperty("CreatedById")?.SetValue(entity, userId);
        type.GetProperty("CreatedDate")?.SetValue(entity, now);
        type.GetProperty("IsActive")?.SetValue(entity, true);
        type.GetProperty("UpdatedById")?.SetValue(entity, userId);
        type.GetProperty("UpdatedDate")?.SetValue(entity, now);
    }

    // ---- Conversion helpers ----

    private bool TryConvert(AggregateNode node, Guid id, string field, JsonElement element, out object? value)
    {
        value = null;
        var property = node.ScalarProperties[field];
        var text = element.ToString();
        try
        {
            if (EntityUtil.ConvertValue(property, text, out value) == ValueConversion.Set)
            {
                return true;
            }

            // The converter skips an empty Guid, which counts as not sent; one it can't read is an error
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }
        }
        catch (Exception e) when (e is ValueConversionException or FormatException)
        {
        }

        _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, id, field,
            $"{node.TableName} {id}: {field} \"{text}\" isn't a valid {UnderlyingType(property).Name}."));
        return false;
    }

    // The UpdatedDate a record was loaded with. Null or empty means the client has none, so there's nothing to
    // check; a value that isn't a date is an error rather than a reason to skip the check.
    private DateTime? ReadToken(AggregateNode node, Guid id, PropertyInfo property, JsonElement element)
    {
        var text = element.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            if (EntityUtil.ConvertValue(property, text, out var value) == ValueConversion.Set && value is DateTime date)
            {
                return date;
            }
        }
        catch (Exception e) when (e is ValueConversionException or FormatException)
        {
        }

        _errors.Add(Error(AggregateErrorKind.Invalid, node.TableName, id, "UpdatedDate",
            $"{node.TableName} {id}: UpdatedDate \"{text}\" isn't a date, so the save can't tell whether someone else " +
            "changed the record."));
        return null;
    }

    private static Type UnderlyingType(PropertyInfo property) =>
        Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

    internal static Guid? AsGuid(object? value) => value switch
    {
        Guid guid => guid,
        string text when Guid.TryParse(text, out var parsed) => parsed,
        _ => null
    };

    private static AggregateError Error(AggregateErrorKind kind, string table, Guid? id, string? field,
        string message) => new(kind, table, id, field, message);

    // ---- State ----

    private enum PlannedKind
    {
        Keep,
        Create,
        Update,
        Delete
    }

    private sealed class Planned
    {
        public required AggregateNode Node { get; init; }
        public required Guid Id { get; init; }
        public PlannedKind Kind { get; set; }
        public Dictionary<string, AggregateFieldChange> Changes { get; init; } = new();
        public Dictionary<string, object?> Values { get; init; } = new();
        public Dictionary<string, object?>? Current { get; init; }
        public bool ClientSuppliedId { get; init; }

        // The UpdatedDate of each submitted copy that changes the record
        public List<DateTime> Tokens { get; } = new();

        // Create: the instance to insert, and the sections whose query it must stay in (root records only)
        public object? Entity { get; set; }
        public List<AggregateSection> Sections { get; } = new();

        // Create: the parent each nested occurrence links it to, by link field. A record submitted both as a root
        // and nested keeps the link from the nesting, whichever came first.
        public Dictionary<string, Guid> Links { get; } = new();
    }

    private sealed record ReferenceCheck(AggregateNode ParentNode, Guid ParentId, AggregateNode Node, Guid? ReferencedId);

    private sealed class SubmittedRecord
    {
        public required AggregateNode Node { get; init; }
        public SubmittedRecord? Parent { get; init; }
        public Guid Id { get; set; }
        public bool IdAssigned { get; set; }
        public Dictionary<string, JsonElement> Fields { get; } = new();

        // Only the joins whose key was present; an absent key means "don't touch"
        public Dictionary<AggregateNode, List<SubmittedRecord>> Arrays { get; } = new();
    }

    private sealed class CurrentRecord
    {
        public required Guid Id { get; init; }
        public Guid? ParentId { get; init; }
        public required Dictionary<string, object?> Values { get; init; }
    }

    private sealed class SectionState(AggregateSection section, List<SubmittedRecord> roots)
    {
        private readonly Dictionary<AggregateNode, Dictionary<Guid, SubmittedRecord>> _submitted = new();
        private readonly Dictionary<AggregateNode, Dictionary<Guid, List<HashSet<Guid>>>> _arrays = new();

        public AggregateSection Section { get; } = section;
        public List<SubmittedRecord> Roots { get; } = roots;
        public Dictionary<AggregateNode, Dictionary<Guid, CurrentRecord>> Current { get; } = new();
        public Dictionary<AggregateNode, ILookup<Guid, CurrentRecord>> ChildrenByParent { get; } = new();

        // Submitted records per node, by ID: the first occurrence of each
        public Dictionary<Guid, SubmittedRecord> Submitted(AggregateNode node) => Get(_submitted, node);

        // For a child join, by parent ID: the IDs in each array submitted for that parent. A parent that is a
        // reference shared by several records has one array per copy.
        public Dictionary<Guid, List<HashSet<Guid>>> Arrays(AggregateNode node) => Get(_arrays, node);

        public List<HashSet<Guid>> ArraysOf(AggregateNode node, Guid parentId)
        {
            var arrays = Arrays(node);
            if (!arrays.TryGetValue(parentId, out var list))
            {
                list = new List<HashSet<Guid>>();
                arrays[parentId] = list;
            }

            return list;
        }

        private static T Get<T>(Dictionary<AggregateNode, T> map, AggregateNode node) where T : new()
        {
            if (!map.TryGetValue(node, out var value))
            {
                value = new T();
                map[node] = value;
            }

            return value;
        }
    }
}
