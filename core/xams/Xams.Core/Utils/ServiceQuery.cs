using System.Globalization;
using Xams.Core.Dtos.Data;
using JoinDto = Xams.Core.Dtos.Data.Join;
using OrderByDto = Xams.Core.Dtos.Data.OrderBy;

namespace Xams.Core.Utils;

/// <summary>
/// Fluent query builder for backend reads, mirroring the frontend Query class.
/// Produces a ReadInput that can be executed with BaseServiceContext.Read.
/// </summary>
/// <example>
/// var query = new ServiceQuery("*")
///     .From("Widget")
///     .Join("Option.OptionId", "Widget.WidgetTypeId", "type")
///     .Where("Name", "==", "Test")
///     .And(ServiceQuery.Exp("Price", ">", 10).Or("Price", "==", 0))
///     .OrderBy("Name")
///     .Top(100);
/// </example>
public class ServiceQuery
{
    private int? _maxResults;
    private int _page = 1;
    private readonly string[] _fields;
    private string _tableName = "";
    private readonly List<ServiceFilter> _filters = new();
    private readonly List<JoinDto> _joins = new();
    private readonly List<OrderByDto> _orderBy = new();
    private bool _distinct;
    private bool _denormalize;
    private readonly List<Exclude> _except = new();

    public ServiceQuery(params string[] fields)
    {
        _fields = fields.Length == 0 ? ["*"] : fields;
    }

    /// <summary>
    /// Limit the number of results per page. If not set, the read defaults to 50 results.
    /// </summary>
    public ServiceQuery Top(int maxResults)
    {
        _maxResults = maxResults;
        return this;
    }

    public ServiceQuery Page(int page)
    {
        _page = page;
        return this;
    }

    public ServiceQuery From(string tableName)
    {
        _tableName = tableName;
        return this;
    }

    public ServiceQuery Where(ServiceFilter filter)
    {
        _filters.Add(filter);
        return this;
    }

    public ServiceQuery Where(string field, string @operator, object? value)
    {
        _filters.Add(Exp(field, @operator, value));
        return this;
    }

    public ServiceQuery And(ServiceFilter filter)
    {
        LastFilter().And(filter);
        return this;
    }

    public ServiceQuery And(string field, string @operator, object? value)
    {
        LastFilter().And(field, @operator, value);
        return this;
    }

    public ServiceQuery Or(ServiceFilter filter)
    {
        LastFilter().Or(filter);
        return this;
    }

    public ServiceQuery Or(string field, string @operator, object? value)
    {
        LastFilter().Or(field, @operator, value);
        return this;
    }

    /// <summary>
    /// Inner join another table. From and to are in the format "TableName.FieldName".
    /// </summary>
    public ServiceQuery Join(string from, string to, string alias, string[]? fields = null)
    {
        _joins.Add(CreateJoin(from, to, alias, fields, "inner"));
        return this;
    }

    /// <summary>
    /// Left join another table. From and to are in the format "TableName.FieldName".
    /// </summary>
    public ServiceQuery LeftJoin(string from, string to, string alias, string[]? fields = null)
    {
        _joins.Add(CreateJoin(from, to, alias, fields, "left"));
        return this;
    }

    /// <summary>
    /// Exclude records whose primary key appears in the given field of the subquery results.
    /// </summary>
    public ServiceQuery Except(string fromField, ServiceQuery query)
    {
        _except.Add(new Exclude
        {
            fromField = fromField,
            query = query.ToReadInput()
        });
        return this;
    }

    public ServiceQuery OrderBy(string field, string order = "asc")
    {
        _orderBy.Add(new OrderByDto
        {
            field = field,
            order = order
        });
        return this;
    }

    public ServiceQuery Distinct()
    {
        _distinct = true;
        return this;
    }

    public ServiceQuery Denormalize()
    {
        _denormalize = true;
        return this;
    }

    /// <summary>
    /// Create a filter expression that can be composed with And / Or.
    /// </summary>
    public static ServiceFilter Exp(string field, string @operator, object? value)
    {
        var filter = new ServiceFilter();
        filter.AddCondition(field, @operator, value);
        return filter;
    }

    public ReadInput ToReadInput()
    {
        if (string.IsNullOrEmpty(_tableName))
        {
            throw new Exception("ServiceQuery requires a table name. Call From(tableName).");
        }

        return new ReadInput
        {
            tableName = _tableName,
            fields = _fields,
            page = _page,
            maxResults = _maxResults,
            orderBy = _orderBy.ToArray(),
            filters = BuildFilters(),
            joins = _joins.ToArray(),
            distinct = _distinct,
            denormalize = _denormalize,
            except = _except.ToArray()
        };
    }

    private Filter[] BuildFilters()
    {
        if (_filters.Count == 0)
        {
            return [];
        }

        if (_filters.Count == 1)
        {
            return [_filters[0].ToFilter()];
        }

        // Multiple Where calls are combined with AND
        return
        [
            new Filter
            {
                logicalOperator = "AND",
                filters = _filters.Select(x => x.ToFilter()).ToArray()
            }
        ];
    }

    private ServiceFilter LastFilter()
    {
        if (_filters.Count == 0)
        {
            throw new Exception("Call Where before And / Or.");
        }

        return _filters[^1];
    }

    private static JoinDto CreateJoin(string from, string to, string alias, string[]? fields, string type)
    {
        string[] fromParts = from.Split('.');
        string[] toParts = to.Split('.');
        if (fromParts.Length < 2 || toParts.Length < 2)
        {
            throw new Exception("Join must include a table and field part ie: tableName.fieldName");
        }

        return new JoinDto
        {
            fromTable = fromParts[0],
            fromField = fromParts[1],
            toTable = toParts[0],
            toField = toParts[1],
            alias = alias,
            fields = fields ?? [],
            type = type
        };
    }

    /// <summary>
    /// Convert a filter value to the string format the read pipeline expects.
    /// </summary>
    internal static string? ConvertValue(object? value)
    {
        return value switch
        {
            null => null,
            string s => s,
            bool b => b ? "true" : "false",
            DateTime dt => (dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime()).ToString("O"),
            DateTimeOffset dto => dto.UtcDateTime.ToString("O"),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}

/// <summary>
/// Composable filter expression for ServiceQuery, mirroring the frontend Filter class.
/// Create with ServiceQuery.Exp and chain And / Or.
/// </summary>
public class ServiceFilter
{
    private string _logicalOperator = "AND";
    private readonly List<ServiceFilter> _filters = new();
    private readonly List<Filter> _conditions = new();
    private Filter? _lastCondition;

    public ServiceFilter And(ServiceFilter filter)
    {
        _filters.Add(filter);
        return this;
    }

    public ServiceFilter And(string field, string @operator, object? value)
    {
        if (_logicalOperator == "OR")
        {
            // AND binds tighter than OR; group the new condition with the previous one
            ServiceFilter filter;
            Filter? rootLastCondition = _conditions.Count > 0 ? _conditions[^1] : null;
            if (_lastCondition != null && _lastCondition == rootLastCondition)
            {
                filter = new ServiceFilter();
                _conditions.RemoveAt(_conditions.Count - 1);
                filter._conditions.Add(rootLastCondition!);
                _filters.Add(filter);
            }
            else
            {
                filter = _filters[^1];
            }

            _lastCondition = CreateCondition(field, @operator, value);
            filter._conditions.Add(_lastCondition);
            return this;
        }

        AddCondition(field, @operator, value);
        return this;
    }

    public ServiceFilter Or(ServiceFilter filter)
    {
        _filters.Add(filter);
        return this;
    }

    public ServiceFilter Or(string field, string @operator, object? value)
    {
        // On the first Or, move the existing AND conditions into their own group
        if (_logicalOperator == "AND")
        {
            _logicalOperator = "OR";
            if (_conditions.Count > 0)
            {
                var filter = new ServiceFilter();
                filter._conditions.AddRange(_conditions);
                _filters.Add(filter);
                _conditions.Clear();
            }
        }

        AddCondition(field, @operator, value);
        return this;
    }

    internal void AddCondition(string field, string @operator, object? value)
    {
        _lastCondition = CreateCondition(field, @operator, value);
        _conditions.Add(_lastCondition);
    }

    internal Filter ToFilter()
    {
        return new Filter
        {
            logicalOperator = _logicalOperator,
            filters = _conditions.Concat(_filters.Select(x => x.ToFilter())).ToArray()
        };
    }

    private static Filter CreateCondition(string field, string @operator, object? value)
    {
        return new Filter
        {
            field = field,
            @operator = @operator,
            value = ServiceQuery.ConvertValue(value)
        };
    }
}
