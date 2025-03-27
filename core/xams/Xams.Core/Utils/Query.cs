using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Attributes;

namespace Xams.Core.Utils;

public class Query
{
    public string TableName { get; private set; } = null!;
    public string RootAlias { get; }
    private readonly DbContext _dbContext;
    private DynamicLinq<DbContext> _dynamicLinq = null!;
    private IQueryable _query = null!;
    private IOrderedQueryable _orderedQueryable = null!;
    private int _top { get; set; } = -1;
    private string[] _fields { get; set; }
    private List<string> _allFields { get; set; } = new();
    private Dictionary<string, Type> _allFieldTypes { get; set; } = new();
    private List<string> _selectedFields { get; set; } = new();
    private List<string> _orders { get; set; } = new();
    private List<string> _aliases { get; set; } = new();
    private bool _isDistinct { get; set; }
    private Dictionary<string, string> _fieldMap { get; set; } = new();

    public Query(DbContext dbContext, string[] fields, string rootAlias = "root")
    {
        _dbContext = dbContext;
        RootAlias = rootAlias;
        _selectedFields.AddRange(fields.Select(x => $"{rootAlias}_{x}").ToList());
        _fields = fields;
    }

    public Query Distinct()
    {
        _isDistinct = true;
        return this;
    }
    public Query Top(int value)
    {
        _top = value;
        return this;
    }

    public Query From(string tableName)
    {
        if (!FieldsExist(_fields, tableName, out string missingField))
        {
            throw new Exception($"Field {missingField} does not exist in table {tableName}");
        }

        TableName = tableName;
        var baseType = Cache.Instance.GetTableMetadata(tableName).Type;
        _dynamicLinq = new DynamicLinq<DbContext>(_dbContext, baseType);
        _query = _dynamicLinq.Query;

        bool selectAll = _selectedFields.Contains($"{RootAlias}_*");
        if (selectAll)
        {
            _selectedFields.Clear();
        }

        List<string> resultSelector = new List<string>();
        foreach (var property in baseType.GetEntityProperties())
        {
            if (IsPrimitive(property.PropertyType))
            {
                resultSelector.Add($"{property.Name} as {RootAlias}_{property.Name}");
            }
            else
            {
                string lookupName = GetNameLookup(property.PropertyType);
                resultSelector.Add($"{property.Name}.{lookupName} as {RootAlias}_{property.Name}");
            }

            _allFields.Add($"{RootAlias}_{property.Name}");
            _allFieldTypes.Add($"{RootAlias}_{property.Name}", property.PropertyType);
            _fieldMap.Add($"{RootAlias}_{property.Name}", tableName);
            if (selectAll)
            {
                _selectedFields.Add($"{RootAlias}_{property.Name}");
            }
        }

        _query = _query.Select($"new({string.Join(",", resultSelector)})");

        return this;
    }

    public Query Join(string from, string to, string alias, string[] fields)
    {
        if (alias == RootAlias)
        {
            throw new Exception($"Alias cannot be '{RootAlias}'");
        }

        bool selectAll = fields is ["*"];
        if (!selectAll)
        {
            _selectedFields.AddRange(fields.Select(x => $"{alias}_{x}").ToList());
        }

        string[] toParts = to.Split(".");

        if (!FieldsExist(fields, toParts[0], out string missingField))
        {
            throw new Exception($"Field {missingField} does not exist in table {toParts[0]}");
        }

        string fromKey = GetFromKey(from);
        _aliases.Add(alias);

        Type joinType = Cache.Instance.GetTableMetadata(toParts[0]).Type;
        DynamicLinq<DbContext> dynamicLinqJoin = new DynamicLinq<DbContext>(_dbContext, joinType);
        IQueryable joinQuery = dynamicLinqJoin.Query;

        List<string> resultSelector = new List<string>();
        // Include all outer fields
        foreach (var field in _allFields)
        {
            resultSelector.Add($"outer.{field} as {field}");
        }

        foreach (var property in joinType.GetEntityProperties())
        {
            if (IsPrimitive(property.PropertyType))
            {
                resultSelector.Add($"inner.{property.Name} as {alias}_{property.Name}");
            }
            else
            {
                string lookupName = GetNameLookup(property.PropertyType);
                resultSelector.Add($"inner.{property.Name}.{lookupName} as {alias}_{property.Name}");
            }

            _allFields.Add($"{alias}_{property.Name}");
            _allFieldTypes.Add($"{alias}_{property.Name}", property.PropertyType);
            _fieldMap.Add($"{alias}_{property.Name}", toParts[0]);
            if (selectAll)
            {
                _selectedFields.Add($"{alias}_{property.Name}");
            }
        }

        _query = _query.Join(joinQuery,
            fromKey,
            toParts[1],
            $"new {{{string.Join(",", resultSelector)}}}");

        return this;
    }

    public Query Join(string from, string to, Query joinQuery)
    {
        DuplicateAliasCheck(joinQuery);

        if (joinQuery._fields is ["*"])
        {
            _selectedFields.AddRange(joinQuery._allFields.Select(x => x).ToList());
        }
        else
        {
            _selectedFields.AddRange(joinQuery._fields.Select(x => $"{joinQuery.RootAlias}_{x}").ToList());
        }

        string fromKey = GetFromKey(from);

        List<string> resultSelector = new List<string>();
        resultSelector.AddRange(_allFields.Select(x => $"outer.{x} as {x}"));
        resultSelector.AddRange(joinQuery._allFields.Select(x => $"inner.{x} as {x}"));
        _allFields.AddRange(joinQuery._allFields);
        _allFieldTypes = _allFieldTypes.Concat(joinQuery._allFieldTypes).ToDictionary(x => x.Key, x => x.Value);
        _aliases.Add(joinQuery.RootAlias);
        _aliases.AddRange(joinQuery._aliases);

        _query = _query.Join(joinQuery.ToQueryableRaw(),
            fromKey,
            to.Replace(".", "_"),
            $"new {{{string.Join(",", resultSelector)}}}");
        return this;
    }

    public Query LeftJoin(string from, string to, string alias, string[] fields)
    {
        if (alias == RootAlias)
        {
            throw new Exception($"Alias cannot be '{RootAlias}'");
        }

        bool selectAll = fields is ["*"];
        if (!selectAll)
        {
            _selectedFields.AddRange(fields.Select(x => $"{alias}_{x}").ToList());
        }

        string[] toParts = to.Split(".");

        if (!FieldsExist(fields, toParts[0], out string missingField))
        {
            throw new Exception($"Field {missingField} does not exist in table {toParts[0]}");
        }

        string fromKey = GetFromKey(from);
        _aliases.Add(alias);

        var joinType = Cache.Instance.GetTableMetadata(toParts[0]).Type;
        DynamicLinq<DbContext> dynamicLinqJoin = new DynamicLinq<DbContext>(_dbContext, joinType);

        List<string> joinQueryFields = new List<string>();
        List<string> selectManyInnerFields = new List<string>();
        List<string> selectManyOuterFields = new List<string>();

        foreach (var field in _allFields)
        {
            selectManyOuterFields.Add($"x.outerEntity.{field} as {field}");
        }

        foreach (var property in joinType.GetEntityProperties())
        {
            if (IsPrimitive(property.PropertyType))
            {
                joinQueryFields.Add($"{property.Name} as {RootAlias}_{property.Name}");
            }
            else
            {
                string lookupName = GetNameLookup(property.PropertyType);
                joinQueryFields.Add($"{property.Name}.{lookupName} as {RootAlias}_{property.Name}");
            }

            Type fieldType = property.PropertyType;
            fieldType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            if (fieldType == typeof(string))
            {
                selectManyInnerFields.Add($"y.{RootAlias}_{property.Name} as {alias}_{property.Name}");
            } 
            else if (IsPrimitive(fieldType))
            {
                // In case the joined record is null, we have to make sure the field is nullable
                selectManyInnerFields.Add($"{fieldType.Name}?(y.{RootAlias}_{property.Name}) as {alias}_{property.Name}");
            }
            else
            {
                selectManyInnerFields.Add($"y.{RootAlias}_{property.Name} as {alias}_{property.Name}");
            }
            
            _allFields.Add($"{alias}_{property.Name}");
            _allFieldTypes.Add($"{alias}_{property.Name}", property.PropertyType);
            _fieldMap.Add($"{alias}_{property.Name}", toParts[0]);
            if (selectAll)
            {
                _selectedFields.Add($"{alias}_{property.Name}");
            }
        }
        
        IQueryable joinQuery = dynamicLinqJoin.Query.Select($"new ({string.Join(",", joinQueryFields)})");

        var result = _query.GroupJoin(
            joinQuery,
            $"it.{fromKey}",
            $"{RootAlias}_{toParts[1]}",
            "new (outer as outerEntity, inner as innerEntity)"
        );
        result = result.SelectMany($"innerEntity.DefaultIfEmpty()",
            $"new ({string.Join(",", selectManyInnerFields)}, {string.Join(",", selectManyOuterFields)})");

        _query = result;

        return this;
    }

    public Query LeftJoin(string from, string to, Query joinQuery)
    {
        DuplicateAliasCheck(joinQuery);

        if (joinQuery._fields is ["*"])
        {
            _selectedFields.AddRange(joinQuery._allFields.Select(x => x).ToList());
        }
        else
        {
            _selectedFields.AddRange(joinQuery._fields.Select(x => $"{joinQuery.RootAlias}_{x}").ToList());
        }

        string fromKey = GetFromKey(from);
        List<string> selectManyInnerFields = new List<string>();
        foreach (var field in joinQuery._allFieldTypes)
        {
            Type fieldType = field.Value;
            fieldType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;
            if (fieldType == typeof(string))
            {
                selectManyInnerFields.Add($"y.{field.Key} as {field.Key}");
            } 
            else if (IsPrimitive(fieldType))
            {
                // In case the joined record is null, we have to make sure the field is nullable
                selectManyInnerFields.Add($"{fieldType.Name}?(y.{field.Key}) as {field.Key}");
            }
            else
            {
                selectManyInnerFields.Add($"y.{field.Key} as {field.Key}");
            }
        }

        List<string> selectManyOuterFields = _allFields.Select(x => $"x.outerEntity.{x} as {x}").ToList();

        _allFields.AddRange(joinQuery._allFields);
        _allFieldTypes = _allFieldTypes.Concat(joinQuery._allFieldTypes).ToDictionary(x => x.Key, x => x.Value);
        _aliases.Add(joinQuery.RootAlias);
        _aliases.AddRange(joinQuery._aliases);

        var result = _query.GroupJoin(
            joinQuery.ToQueryableRaw(),
            $"it.{fromKey}",
            to.Replace(".", "_"),
            "new (outer as outerEntity, inner as innerEntity)"
        );
        result = result.SelectMany($"innerEntity.DefaultIfEmpty()",
            $"new ({string.Join(",", selectManyInnerFields)}, {string.Join(",", selectManyOuterFields)})");
        
        _query = result;
        return this;
    }
    

    public Query Where(string where, params object[]? args)
    {
        _query = args != null ? _query.Where(where, args) : _query.Where(where);
        return this;
    }

    public Query OrderBy(string field, string order)
    {
        string orderBy = $"{field} {order}";
        if (_orders.Count > 0)
        {
            _orderedQueryable = _orderedQueryable.ThenBy(orderBy);
            _query = _orderedQueryable;
        }
        else
        {
            _orderedQueryable = _query.OrderBy(orderBy);
            _query = _orderedQueryable;
        }

        _orders.Add(orderBy);

        return this;
    }

    public Query Union(Query query)
    {
        var queryable = ToQueryable();
        _query = queryable.Provider.CreateQuery(
            Expression.Call(
                typeof(Queryable),
                "Union",
                new[] { queryable.ElementType },
                queryable.Expression,
                query.ToQueryable().Expression
            ));
        return this;
    }

    public Query Except(string fromField, Query excludeQuery)
    {
        if (fromField.Contains("."))
        {
            fromField = fromField.Replace(".", "_");
        }
        else
        {
            fromField = $"{RootAlias}_{fromField}";
        }
        
        var exclude = excludeQuery.ToQueryable();
        exclude = exclude.Select(excludeQuery._selectedFields[0]);

        Type tableType = Cache.Instance.GetTableMetadata(_fieldMap[fromField]).Type;
        Type? fieldType = tableType.GetProperty(fromField.Split("_")[1])?.PropertyType;

        if (fieldType == null)
        {
            throw new Exception($"Could not find field {fromField} in table {tableType.Name}");
        }

        // Define the parameters.
        var entity = Expression.Parameter(_query.ElementType, "entity");
        var excludeId = Expression.Parameter(fieldType, "excludeId");
        // Build the inner lambda: excludeId => excludeId == entity.{PrimaryKey}
        var primaryKeyProperty = Expression.Property(entity, fromField);
        var innerLambdaBody = Expression.Equal(excludeId, primaryKeyProperty);
        var innerLambda = Expression.Lambda(innerLambdaBody, excludeId);

        // Build the call to Any: excludeQuery.Any(excludeId => excludeId == entity.PermissionId)
        MethodInfo anyMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == "Any" && m.GetParameters().Count() == 2)
            .Single(m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
            .MakeGenericMethod(fieldType);
        var anyCall = Expression.Call(anyMethod, exclude.Expression, innerLambda);

        // Negate the result of the Any call
        var notAny = Expression.Not(anyCall);

        // Build the final lambda for Where: entity => !excludeQuery.Any(excludeId => excludeId == entity.PermissionId)
        var finalLambda = Expression.Lambda(notAny, entity);

        // Apply the lambda to the original query dynamically
        IEnumerable<MethodInfo?> whereMethods = typeof(Queryable).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m is { Name: "Where", IsGenericMethod: true }
                        && m.GetGenericArguments().Length == 1
                        && m.DeclaringType?.Namespace == "System.Linq");

        MethodInfo? targetMethod = null;

        foreach (var method in whereMethods)
        {
            var parameters = method?.GetParameters();
            if (parameters is { Length: 2 } &&
                parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(IQueryable<>) &&
                parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>))
            {
                targetMethod = method;
                break;
            }
        }

        if (targetMethod == null)
            throw new InvalidOperationException("Could not find the desired Where method.");

        MethodInfo whereMethod = targetMethod.MakeGenericMethod(_query.ElementType);


        _query = _query.Provider.CreateQuery(Expression.Call(
            null,
            whereMethod,
            new[] { _query.Expression, Expression.Quote(finalLambda) }));

        return this;
    }

    public async Task<List<dynamic>> ToDynamicListAsync()
    {
        var result = Final();
        return await result.ToDynamicListAsync();
    }

    public IQueryable ToQueryable()
    {
        return Final();
    }

    public IQueryable ToQueryableRaw()
    {
        IQueryable result = _query;
        if (_isDistinct)
        {
            result.Distinct();
        }

        if (_top != -1)
        {
            result = result.Take(_top);
        }

        return result;
    }

    private IQueryable Final()
    {
        IQueryable result = _query
            .Select($"new({string.Join(",", _selectedFields)})");
        IOrderedQueryable orderedQueryable = null!;
        if (_isDistinct)
        {
            result = result.Distinct();
            if (_orders.Select(x => x.Split(" ")[0]).Any(x => !_selectedFields.Contains(x)))
            {
                throw new Exception("OrderBy field must be in the final result set when using Distinct");
            }

            // Need to re-apply order otherwise it's lost
            for (int i = 0; i < _orders.Count; i++)
            {
                string orderBy = _orders[i];
                orderedQueryable = i == 0 ? result.OrderBy(orderBy) : orderedQueryable.ThenBy(orderBy);

                result = orderedQueryable;
            }
        }

        if (_top != -1)
        {
            result = result.Take(_top);
        }

        return result;
    }

    private string GetFromKey(string from)
    {
        if (!from.Contains("."))
        {
            return from;
        }
        string[] fromParts = from.Split(".");
        string fromKey;
        if (fromParts[0] == TableName)
        {
            fromKey = $"{RootAlias}_{fromParts[1]}";
        }
        else
        {
            if (!_aliases.Contains(fromParts[0]))
            {
                throw new Exception($"Alias {fromParts[0]} does not exist in the query");
            }

            fromKey = from.Replace(".", "_");
        }

        return fromKey;
    }

    private bool IsPrimitive(Type propertyType)
    {
        Type? nullableType = Nullable.GetUnderlyingType(propertyType);
        if (nullableType != null)
        {
            propertyType = nullableType;
        }

        if (propertyType.IsPrimitive
            || propertyType == typeof(Guid)
            || propertyType == typeof(string)
            || propertyType == typeof(decimal)
            || propertyType == typeof(DateTime))
        {
            return true;
        }

        return false;
    }

    private bool FieldsExist(string[] fields, string tableName, out string s)
    {
        if (fields == null) throw new ArgumentNullException(nameof(fields));
        foreach (var field in fields)
        {
            if (field == "*") continue;
            if (Cache.Instance.GetTableMetadata(tableName).Type.GetEntityProperties().All(x => x.Name != field))
            {
                s = field;
                return false;
            }
        }

        s = string.Empty;
        return true;
    }

    private void DuplicateAliasCheck(Query joinQuery)
    {
        if (RootAlias == joinQuery.RootAlias || _aliases.Contains(joinQuery.RootAlias))
        {
            throw new Exception($"Alias {joinQuery.RootAlias} already exists in the query");
        }

        foreach (var joinQueryAlias in joinQuery._aliases)
        {
            if (_aliases.Contains(joinQueryAlias))
            {
                throw new Exception($"Alias {joinQueryAlias} already exists in the query");
            }
        }
    }

    private string GetNameLookup(Type propertyType)
    {
        string propertyName = string.Empty;

        var properties = propertyType.GetEntityProperties();

        // Check for "Name" first
        foreach (var property in properties)
        {
            if (property.PropertyType == typeof(string) && property.Name == "Name")
            {
                propertyName = property.Name;
            }
        }

        foreach (var property in properties)
        {
            if (property.GetCustomAttributes()
                    .FirstOrDefault(x => x.GetType() == typeof(UINameAttribute)) != null)
            {
                propertyName = property.Name;
            }
        }

        if (string.IsNullOrEmpty(propertyName))
        {
            // User the PrimaryKey if there's no name field
            propertyName = Cache.Instance.GetTableMetadata(propertyType.Name).PrimaryKey;
        }

        return propertyName;
    }
}
/* Examples
    // var query1 = new Query<DataContext>(db, new[] { "ADXUser" })
   //     .Distinct()
   //     .Top(10)
   //     .From("User")
   //     .Join("User.UserId", "TeamUser.UserId", "tu", Array.Empty<string>())
   //     .LeftJoin("tu.TeamId", "Team.TeamId", "t", Array.Empty<string>())
   //     .LeftJoin("t.TeamId","Setting.SettingId", "s", new[] { "SettingId" })
   //     .Where("root_Name == @0 OR root_Name == @1 OR t_Name == @2", "Zhen Wu", "Zachary T D'Agata", "Booger")
   //     .OrderBy("root_ADXUser", "desc");
   // var query1Queryable = query1.ToQueryable();
   //
   // var query2 = new Query<DataContext>(db, new[] { "Name" })
   //     .Distinct()
   //     .Top(10)
   //     .From("User")
   //     .Join("User.UserId", "TeamUser.UserId", "tu", new[] { "TeamId" })
   //     .Join("tu.TeamId", "Team.TeamId", "t", new[] { "Name" })
   //     .Where("root_Name == @0 OR root_Name == @1", "Yashasvi Soni", "Xiao D He")
   //     .OrderBy("root_Name", "desc")
   //     .Union(query1);
   //
   // var except = new Query<DataContext>(db, new[] { "Name" })
   //     .From("User")
   //     .Where("root_Name == @0", "Xiao D He");
   //
   // query2 = query2.Except("root_Name", except);
   //
   // var result = query2.ToQueryable();
*/
