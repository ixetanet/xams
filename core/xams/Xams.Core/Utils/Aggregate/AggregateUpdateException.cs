namespace Xams.Core.Utils;

/// <summary>
/// What kind of problem stopped an <see cref="AggregateUpdater"/> save.
/// </summary>
public enum AggregateErrorKind
{
    /// <summary>The query or the updater options can't be used for a save.</summary>
    Setup,

    /// <summary>The submitted data is malformed or contradicts itself.</summary>
    Invalid,

    /// <summary>The submitted data would write outside the rows the query selects.</summary>
    OutOfScope,

    /// <summary>Someone else changed the data after it was loaded.</summary>
    Conflict,

    /// <summary>The database rejected the save.</summary>
    Database
}

/// <summary>
/// One problem found while diffing or saving an aggregate.
/// </summary>
/// <param name="Kind">What kind of problem it is.</param>
/// <param name="TableName">The table the problem is in.</param>
/// <param name="Id">The record's primary key, when the problem is about one record.</param>
/// <param name="Field">The field, when the problem is about one field.</param>
/// <param name="Message">A readable description.</param>
public sealed record AggregateError(
    AggregateErrorKind Kind,
    string TableName,
    Guid? Id,
    string? Field,
    string Message);

/// <summary>
/// Thrown by <see cref="AggregateUpdater"/> when the diff or the save fails. Nothing has been written when the
/// diff fails; when the save fails, the caller's transaction must roll back (an action does this automatically).
/// </summary>
public sealed class AggregateUpdateException : Exception
{
    private const int MessageErrorLimit = 5;

    /// <summary>Every problem found.</summary>
    public IReadOnlyList<AggregateError> Errors { get; }

    internal AggregateUpdateException(IReadOnlyList<AggregateError> errors, Exception? innerException = null)
        : base(BuildMessage(errors), innerException)
    {
        Errors = errors;
    }

    internal AggregateUpdateException(AggregateError error, Exception? innerException = null)
        : this([error], innerException)
    {
    }

    private static string BuildMessage(IReadOnlyList<AggregateError> errors)
    {
        var messages = errors.Take(MessageErrorLimit).Select(x => x.Message).ToList();
        if (errors.Count > MessageErrorLimit)
        {
            messages.Add($"and {errors.Count - MessageErrorLimit} more");
        }

        return string.Join(" ", messages);
    }
}
