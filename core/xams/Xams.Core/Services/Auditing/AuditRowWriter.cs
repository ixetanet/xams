using System.Collections;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Xams.Core.Base;
using Xams.Core.Entities;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Inserts audit rows without the change tracker, in the DbContext's current transaction. Small writes use
/// multi-row INSERT statements; large writes use the provider's bulk load (binary COPY on PostgreSQL, SqlBulkCopy
/// on SQL Server, a prepared single-row INSERT on SQLite) when one is available.
/// </summary>
/// <remarks>
/// COPY and SqlBulkCopy are bound by reflection to the provider's own connection, so Xams.Core does not reference
/// the provider packages. If a provider's API cannot be bound, the INSERT statements are used instead.
/// </remarks>
internal static class AuditRowWriter
{
    /// <summary>
    /// Rows from which a bulk load is used. Below it, INSERT statements need fewer round trips.
    /// </summary>
    internal const int BulkLoadThreshold = 500;

    // SQL Server allows at most 1,000 rows in a VALUES list
    private const int MaxRowsPerStatement = 1000;

    // Keeps a command's values well inside every provider's packet limits
    private const int MaxCommandChars = 4_000_000;

    private static readonly ConcurrentDictionary<IEntityType, RowTable> Tables = new();

    /// <summary>
    /// Inserts the histories and then their details.
    /// </summary>
    public static async Task WriteAsync(IXamsDbContext db, IReadOnlyList<AuditHistory> histories,
        IReadOnlyList<AuditHistoryDetail> details, bool async, CancellationToken cancellationToken)
    {
        var context = (DbContext)db;
        List<(RowTable Table, IReadOnlyList<object> Rows)> sets =
        [
            (GetTable(context, typeof(AuditHistory)), histories),
            (GetTable(context, typeof(AuditHistoryDetail)), details)
        ];

        if (histories.Count + details.Count >= BulkLoadThreshold && BulkLoader.Get(db) is { } loader)
        {
            if (async)
            {
                await context.Database.OpenConnectionAsync(cancellationToken);
            }
            else
            {
                context.Database.OpenConnection();
            }

            try
            {
                foreach (var (table, rows) in sets.Where(x => x.Rows.Count > 0))
                {
                    await loader.LoadAsync(context, table, rows, async, cancellationToken);
                }
            }
            finally
            {
                if (async)
                {
                    await context.Database.CloseConnectionAsync();
                }
                else
                {
                    context.Database.CloseConnection();
                }
            }

            return;
        }

        await InsertAsync(context, sets, async, cancellationToken);
    }

    /// <summary>
    /// The most parameters one command may carry.
    /// </summary>
    internal static int MaxParameters(string? providerName)
    {
        return providerName switch
        {
            // PostgreSQL allows 65,535
            "Npgsql.EntityFrameworkCore.PostgreSQL" => 30_000,
            // Microsoft.Data.Sqlite finds each named parameter by searching, so binding time grows with the square
            // of the count: 30,000 parameters took seconds per command. 999 is also older SQLite builds' limit.
            "Microsoft.EntityFrameworkCore.Sqlite" => 999,
            // SQL Server allows 2,100; unknown providers get the same conservative limit
            _ => 2_000
        };
    }

    /// <summary>
    /// Sends the rows as multi-row INSERT statements, as many to a command as the parameter limit allows.
    /// </summary>
    private static async Task InsertAsync(DbContext context, List<(RowTable Table, IReadOnlyList<object> Rows)> sets,
        bool async, CancellationToken cancellationToken)
    {
        var sql = context.GetService<ISqlGenerationHelper>();
        var maxParameters = MaxParameters(context.Database.ProviderName);
        using var parameterFactory = context.Database.GetDbConnection().CreateCommand();
        var text = new StringBuilder();
        var parameters = new List<object>();
        var chars = 0;

        async Task Flush()
        {
            if (text.Length > 0 && async)
            {
                await context.Database.ExecuteSqlRawAsync(text.ToString(), parameters, cancellationToken);
            }
            else if (text.Length > 0)
            {
                context.Database.ExecuteSqlRaw(text.ToString(), parameters);
            }

            text.Clear();
            parameters = new List<object>();
            chars = 0;
        }

        foreach (var (table, rows) in sets)
        {
            var next = 0;
            while (next < rows.Count)
            {
                var capacity = Math.Min(MaxRowsPerStatement, (maxParameters - parameters.Count) / table.Columns.Count);
                if (capacity == 0 || chars >= MaxCommandChars)
                {
                    await Flush();
                    continue;
                }

                text.Append(table.InsertHeader);
                for (var count = 0; count < capacity && next < rows.Count && chars < MaxCommandChars; count++, next++)
                {
                    text.Append(count == 0 ? "(" : ", (");
                    for (var column = 0; column < table.Columns.Count; column++)
                    {
                        var name = "p" + parameters.Count;
                        var value = table.Columns[column].Getter.GetClrValue(rows[next]);
                        text.Append(column == 0 ? "" : ", ").Append(sql.GenerateParameterNamePlaceholder(name));
                        parameters.Add(table.Columns[column].Mapping.CreateParameter(parameterFactory,
                            sql.GenerateParameterName(name), value, table.Columns[column].Property.IsNullable));
                        chars += name.Length + ((value as string)?.Length ?? 0);
                    }

                    text.Append(')');
                }

                text.Append(sql.StatementTerminator).AppendLine();
            }
        }

        await Flush();
    }

    private static RowTable GetTable(DbContext context, Type entityClrType)
    {
        var entityType = context.Model.FindEntityType(entityClrType)!;
        return Tables.GetOrAdd(entityType, _ =>
        {
            var sql = context.GetService<ISqlGenerationHelper>();
            var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
            var columns = entityType.GetProperties()
                .Where(x => !x.IsShadowProperty() && x.GetComputedColumnSql(table) == null)
                .Select(x => (Property: x, Name: x.GetColumnName(table)))
                .Where(x => x.Name != null)
                .Select(x => new RowColumn(x.Name!, x.Property, x.Property.GetRelationalTypeMapping(),
                    x.Property.GetGetter()))
                .ToList();
            var name = sql.DelimitIdentifier(table.Name, table.Schema);
            return new RowTable(name, columns,
                $"INSERT INTO {name} ({string.Join(", ", columns.Select(x => sql.DelimitIdentifier(x.Name)))}) VALUES ");
        });
    }

    internal sealed record RowTable(string Name, List<RowColumn> Columns, string InsertHeader);

    internal sealed record RowColumn(string Name, IProperty Property, RelationalTypeMapping Mapping,
        IClrPropertyGetter Getter)
    {
        /// <summary>
        /// The column's database type without its facets, such as "character varying" for
        /// "character varying(250)".
        /// </summary>
        public string StoreTypeName { get; } = Regex.Replace(Mapping.StoreType, @"\s*\([^)]*\)", "");

        /// <summary>
        /// The value as the provider stores it, after any value converter.
        /// </summary>
        public object? GetProviderValue(object row)
        {
            var value = Getter.GetClrValue(row);
            return value == null || Mapping.Converter == null ? value : Mapping.Converter.ConvertToProvider(value);
        }

        public Type ProviderType
        {
            get
            {
                var type = Mapping.Converter?.ProviderClrType ?? Mapping.ClrType;
                return Nullable.GetUnderlyingType(type) ?? type;
            }
        }
    }

    /// <summary>
    /// A provider's bulk load, bound to its API by reflection.
    /// </summary>
    private abstract class BulkLoader
    {
        private static readonly ConcurrentDictionary<Type, BulkLoader?> Loaders = new();

        public static BulkLoader? Get(IXamsDbContext db)
        {
            var connectionType = ((DbContext)db).Database.GetDbConnection().GetType();
            return Loaders.GetOrAdd(connectionType, type =>
            {
                try
                {
                    return type.FullName switch
                    {
                        "Npgsql.NpgsqlConnection" => new NpgsqlCopy(type),
                        "Microsoft.Data.SqlClient.SqlConnection" => new SqlServerBulkCopy(type),
                        "Microsoft.Data.Sqlite.SqliteConnection" => new PreparedInsert(),
                        _ => null
                    };
                }
                catch (Exception e)
                {
                    AuditLogic.Log(db, e, $"Audit rows are written with INSERT statements: the bulk load of " +
                                          $"{type.FullName} could not be used ({e.Message}).");
                    return null;
                }
            });
        }

        public abstract Task LoadAsync(DbContext context, RowTable table, IReadOnlyList<object> rows, bool async,
            CancellationToken cancellationToken);

        protected static MethodInfo Method(Type type, string name, params Type[] parameters)
        {
            return type.GetMethod(name, parameters) ??
                   throw new MissingMethodException(type.FullName, name);
        }

        /// <summary>
        /// Compiles a call to an instance method into a delegate that takes the instance as its first argument.
        /// A delegate returning Task accepts methods returning Task, Task&lt;T&gt;, ValueTask or ValueTask&lt;T&gt;;
        /// one returning void discards the method's result.
        /// </summary>
        protected static TDelegate Compile<TDelegate>(Type instanceType, MethodInfo method) where TDelegate : Delegate
        {
            var invoke = typeof(TDelegate).GetMethod("Invoke")!;
            var delegateParameters = invoke.GetParameters().Select(x => Expression.Parameter(x.ParameterType))
                .ToList();
            var arguments = method.GetParameters().Select((x, i) => delegateParameters[i + 1].Type == x.ParameterType
                ? (Expression)delegateParameters[i + 1]
                : Expression.Convert(delegateParameters[i + 1], x.ParameterType));
            Expression call = Expression.Call(Expression.Convert(delegateParameters[0], instanceType), method,
                arguments);
            if ((call.Type.IsGenericType && call.Type.GetGenericTypeDefinition() == typeof(ValueTask<>)) ||
                call.Type == typeof(ValueTask))
            {
                call = Expression.Call(call, call.Type.GetMethod(nameof(ValueTask.AsTask))!);
            }

            call = invoke.ReturnType == typeof(void)
                ? Expression.Block(typeof(void), call)
                : Expression.Convert(call, invoke.ReturnType);
            return Expression.Lambda<TDelegate>(call, delegateParameters).Compile();
        }
    }

    /// <summary>
    /// PostgreSQL binary COPY through NpgsqlConnection.BeginBinaryImport(Async).
    /// </summary>
    private sealed class NpgsqlCopy : BulkLoader
    {
        private readonly Func<object, string, CancellationToken, Task> _begin;
        private readonly Func<Task, object> _importer;
        private readonly PropertyInfo _timeout;
        private readonly Func<object, CancellationToken, Task> _startRow;
        private readonly Func<object, object, string, CancellationToken, Task> _write;
        private readonly Func<object, CancellationToken, Task> _writeNull;
        private readonly Func<object, CancellationToken, Task> _complete;
        private readonly Func<object, string, object> _beginSync;
        private readonly Action<object> _startRowSync;
        private readonly Action<object, object, string> _writeSync;
        private readonly Action<object> _writeNullSync;
        private readonly Action<object> _completeSync;

        public NpgsqlCopy(Type connectionType)
        {
            var begin = Method(connectionType, "BeginBinaryImportAsync", typeof(string), typeof(CancellationToken));
            var importerType = begin.ReturnType.GetGenericArguments().Single();
            // The overload taking the PostgreSQL type, so each value is encoded as its column's type
            var write = importerType.GetMethods().Single(x =>
                    x.Name == "WriteAsync" && x.IsGenericMethodDefinition &&
                    x.GetParameters() is [_, { ParameterType: var type }, { ParameterType: var token }] &&
                    type == typeof(string) && token == typeof(CancellationToken))
                .MakeGenericMethod(typeof(object));
            _timeout = importerType.GetProperty("Timeout") is { CanWrite: true } timeout &&
                       timeout.PropertyType == typeof(TimeSpan)
                ? timeout
                : throw new MissingMemberException(importerType.FullName, "Timeout");

            _begin = Compile<Func<object, string, CancellationToken, Task>>(connectionType, begin);
            var task = Expression.Parameter(typeof(Task));
            _importer = Expression.Lambda<Func<Task, object>>(
                Expression.Convert(Expression.Property(Expression.Convert(task, begin.ReturnType), "Result"),
                    typeof(object)), task).Compile();
            _startRow = Compile<Func<object, CancellationToken, Task>>(importerType,
                Method(importerType, "StartRowAsync", typeof(CancellationToken)));
            _write = Compile<Func<object, object, string, CancellationToken, Task>>(importerType, write);
            _writeNull = Compile<Func<object, CancellationToken, Task>>(importerType,
                Method(importerType, "WriteNullAsync", typeof(CancellationToken)));
            _complete = Compile<Func<object, CancellationToken, Task>>(importerType,
                Method(importerType, "CompleteAsync", typeof(CancellationToken)));

            _beginSync = Compile<Func<object, string, object>>(connectionType,
                Method(connectionType, "BeginBinaryImport", typeof(string)));
            _startRowSync = Compile<Action<object>>(importerType, Method(importerType, "StartRow"));
            _writeSync = Compile<Action<object, object, string>>(importerType, importerType.GetMethods().Single(x =>
                    x.Name == "Write" && x.IsGenericMethodDefinition &&
                    x.GetParameters() is [_, { ParameterType: var type }] && type == typeof(string))
                .MakeGenericMethod(typeof(object)));
            _writeNullSync = Compile<Action<object>>(importerType, Method(importerType, "WriteNull"));
            _completeSync = Compile<Action<object>>(importerType, Method(importerType, "Complete"));
        }

        public override async Task LoadAsync(DbContext context, RowTable table, IReadOnlyList<object> rows,
            bool async, CancellationToken cancellationToken)
        {
            var sql = context.GetService<ISqlGenerationHelper>();
            var copy = $"COPY {table.Name} ({string.Join(", ", table.Columns.Select(x => sql.DelimitIdentifier(x.Name)))}) " +
                       "FROM STDIN (FORMAT BINARY)";
            object importer;
            if (async)
            {
                var begin = _begin(context.Database.GetDbConnection(), copy, cancellationToken);
                await begin;
                importer = _importer(begin);
            }
            else
            {
                importer = _beginSync(context.Database.GetDbConnection(), copy);
            }

            // Disposing an import that was not completed cancels it
            try
            {
                // The context's command timeout, as INSERT statements get it; otherwise the connection's applies
                if (context.Database.GetCommandTimeout() is { } seconds)
                {
                    _timeout.SetValue(importer, TimeSpan.FromSeconds(seconds), BindingFlags.DoNotWrapExceptions,
                        null, null, null);
                }

                foreach (var row in rows)
                {
                    if (async)
                    {
                        await _startRow(importer, cancellationToken);
                    }
                    else
                    {
                        _startRowSync(importer);
                    }

                    foreach (var column in table.Columns)
                    {
                        var value = column.GetProviderValue(row);
                        if (!async)
                        {
                            if (value == null)
                            {
                                _writeNullSync(importer);
                            }
                            else
                            {
                                _writeSync(importer, value, column.StoreTypeName);
                            }
                        }
                        else
                        {
                            await (value == null
                                ? _writeNull(importer, cancellationToken)
                                : _write(importer, value, column.StoreTypeName, cancellationToken));
                        }
                    }
                }

                if (async)
                {
                    await _complete(importer, cancellationToken);
                }
                else
                {
                    _completeSync(importer);
                }
            }
            finally
            {
                if (async)
                {
                    await ((IAsyncDisposable)importer).DisposeAsync();
                }
                else
                {
                    ((IDisposable)importer).Dispose();
                }
            }
        }
    }

    /// <summary>
    /// SQL Server's SqlBulkCopy, in the DbContext's transaction.
    /// </summary>
    /// <remarks>
    /// The rows are sent as one batch. Loading them together lets SQL Server lock the new pages rather than each
    /// row: 40,000 detail rows take about 1,100 page locks this way, against about 72,000 row locks in batches of
    /// 4,000.
    /// </remarks>
    private sealed class SqlServerBulkCopy : BulkLoader
    {
        private readonly ConstructorInfo _constructor;
        private readonly object _options;
        private readonly PropertyInfo _destinationTableName;
        private readonly PropertyInfo _bulkCopyTimeout;
        private readonly PropertyInfo _columnMappings;
        private readonly MethodInfo _addColumnMapping;
        private readonly MethodInfo _writeToServer;
        private readonly MethodInfo _writeToServerSync;

        public SqlServerBulkCopy(Type connectionType)
        {
            Type SqlClientType(string name) => connectionType.Assembly.GetType($"{connectionType.Namespace}.{name}",
                throwOnError: true)!;

            var bulkCopyType = SqlClientType("SqlBulkCopy");
            var optionsType = SqlClientType("SqlBulkCopyOptions");
            // Like INSERT statements: check foreign keys (skipping them leaves the constraints untrusted), keep
            // nulls instead of column defaults and fire triggers
            _options = Enum.Parse(optionsType, "CheckConstraints, KeepNulls, FireTriggers");
            _constructor = bulkCopyType.GetConstructor([connectionType, optionsType, SqlClientType("SqlTransaction")])
                           ?? throw new MissingMethodException(bulkCopyType.FullName, ".ctor");
            PropertyInfo Property(string name) => bulkCopyType.GetProperty(name) ??
                                                  throw new MissingMemberException(bulkCopyType.FullName, name);
            _destinationTableName = Property("DestinationTableName");
            _bulkCopyTimeout = Property("BulkCopyTimeout");
            _columnMappings = Property("ColumnMappings");
            _addColumnMapping = Method(_columnMappings.PropertyType, "Add", typeof(int), typeof(string));
            _writeToServer = Method(bulkCopyType, "WriteToServerAsync", typeof(DbDataReader), typeof(CancellationToken));
            _writeToServerSync = Method(bulkCopyType, "WriteToServer", typeof(DbDataReader));
        }

        public override async Task LoadAsync(DbContext context, RowTable table, IReadOnlyList<object> rows,
            bool async, CancellationToken cancellationToken)
        {
            // DoNotWrapExceptions: SqlClient's own exceptions surface rather than TargetInvocationException
            const BindingFlags flags = BindingFlags.DoNotWrapExceptions;
            var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            using var bulkCopy = (IDisposable)_constructor.Invoke(flags, null,
                [context.Database.GetDbConnection(), _options, transaction], null);
            _destinationTableName.SetValue(bulkCopy, table.Name, flags, null, null, null);
            _bulkCopyTimeout.SetValue(bulkCopy, context.Database.GetCommandTimeout() ?? 30, flags, null, null, null);
            var mappings = _columnMappings.GetValue(bulkCopy);
            for (var i = 0; i < table.Columns.Count; i++)
            {
                _addColumnMapping.Invoke(mappings, flags, null, [i, table.Columns[i].Name], null);
            }

            using var reader = new RowReader(table, rows);
            if (async)
            {
                await (Task)_writeToServer.Invoke(bulkCopy, flags, null, [reader, cancellationToken], null)!;
            }
            else
            {
                _writeToServerSync.Invoke(bulkCopy, flags, null, [reader], null);
            }
        }
    }

    /// <summary>
    /// One single-row INSERT, prepared once and executed for each row. SQLite runs in-process, so multi-row
    /// statements save no round trips there, only add binding work.
    /// </summary>
    private sealed class PreparedInsert : BulkLoader
    {
        public override async Task LoadAsync(DbContext context, RowTable table, IReadOnlyList<object> rows,
            bool async, CancellationToken cancellationToken)
        {
            var sql = context.GetService<ISqlGenerationHelper>();
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.CommandTimeout = context.Database.GetCommandTimeout() ?? command.CommandTimeout;
            var names = table.Columns.Select((_, i) => "p" + i).ToList();
            command.CommandText = table.InsertHeader +
                                  $"({string.Join(", ", names.Select(sql.GenerateParameterNamePlaceholder))})";
            foreach (var row in rows)
            {
                // New parameters from the type mappings for each row; the prepared statement is kept
                command.Parameters.Clear();
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    var column = table.Columns[i];
                    command.Parameters.Add(column.Mapping.CreateParameter(command, sql.GenerateParameterName(names[i]),
                        column.Getter.GetClrValue(row), column.Property.IsNullable));
                }

                if (async)
                {
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
                else
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    /// <summary>
    /// Presents rows to a bulk load as a forward-only data reader of provider values.
    /// </summary>
    private sealed class RowReader(RowTable table, IReadOnlyList<object> rows) : DbDataReader
    {
        private int _index = -1;
        private bool _closed;

        public override int FieldCount => table.Columns.Count;
        public override int RecordsAffected => -1;
        public override bool HasRows => rows.Count > 0;
        public override bool IsClosed => _closed;
        public override int Depth => 0;
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));

        public override bool Read() => ++_index < rows.Count;
        public override bool NextResult() => false;
        public override void Close() => _closed = true;

        public override object GetValue(int ordinal) =>
            table.Columns[ordinal].GetProviderValue(rows[_index]) ?? DBNull.Value;

        public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;
        public override string GetName(int ordinal) => table.Columns[ordinal].Name;
        public override int GetOrdinal(string name) => table.Columns.FindIndex(x => x.Name == name);
        public override Type GetFieldType(int ordinal) => table.Columns[ordinal].ProviderType;
        public override string GetDataTypeName(int ordinal) => table.Columns[ordinal].Mapping.StoreType;

        public override int GetValues(object[] values)
        {
            var count = Math.Min(values.Length, FieldCount);
            for (var i = 0; i < count; i++)
            {
                values[i] = GetValue(i);
            }

            return count;
        }

        public override bool GetBoolean(int ordinal) => (bool)GetValue(ordinal);
        public override byte GetByte(int ordinal) => (byte)GetValue(ordinal);
        public override char GetChar(int ordinal) => (char)GetValue(ordinal);
        public override DateTime GetDateTime(int ordinal) => (DateTime)GetValue(ordinal);
        public override decimal GetDecimal(int ordinal) => (decimal)GetValue(ordinal);
        public override double GetDouble(int ordinal) => (double)GetValue(ordinal);
        public override float GetFloat(int ordinal) => (float)GetValue(ordinal);
        public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
        public override short GetInt16(int ordinal) => (short)GetValue(ordinal);
        public override int GetInt32(int ordinal) => (int)GetValue(ordinal);
        public override long GetInt64(int ordinal) => (long)GetValue(ordinal);
        public override string GetString(int ordinal) => (string)GetValue(ordinal);

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
            throw new NotSupportedException();

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
            throw new NotSupportedException();

        public override IEnumerator GetEnumerator() => new DbEnumerator(this);
    }
}
