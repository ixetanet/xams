using System.Globalization;

namespace Xams.Core.Utils;

/// <summary>
/// Decides whether a submitted value differs from the stored one. Both values are already typed: the stored value
/// comes from the database, the submitted one from the API converter.
/// </summary>
internal static class AggregateValueComparer
{
    internal static bool AreEqual(Type propertyType, object? stored, object? submitted)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // Inputs often turn a null string into an empty one
        if (type == typeof(string))
        {
            return string.Equals(stored as string ?? string.Empty, submitted as string ?? string.Empty,
                StringComparison.Ordinal);
        }

        if (stored == null || submitted == null)
        {
            return stored == null && submitted == null;
        }

        if (type == typeof(DateTime) && stored is DateTime storedDate && submitted is DateTime submittedDate)
        {
            return DatesEqual(storedDate, submittedDate);
        }

        if (type == typeof(double) || type == typeof(float))
        {
            var a = Convert.ToDouble(stored, CultureInfo.InvariantCulture);
            var b = Convert.ToDouble(submitted, CultureInfo.InvariantCulture);
            return a.Equals(b);
        }

        if (stored.GetType() != submitted.GetType() && stored is IConvertible && submitted is IConvertible)
        {
            try
            {
                return Equals(Convert.ChangeType(stored, type, CultureInfo.InvariantCulture),
                    Convert.ChangeType(submitted, type, CultureInfo.InvariantCulture));
            }
            catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
            {
                return false;
            }
        }

        // decimal equality ignores scale, so 1.50m equals 1.5m
        return stored.Equals(submitted);
    }

    /// <summary>
    /// Compares in UTC at millisecond precision: JavaScript dates keep milliseconds, databases may keep more.
    /// </summary>
    internal static bool DatesEqual(DateTime a, DateTime b) => TruncateToMilliseconds(a) == TruncateToMilliseconds(b);

    internal static DateTime TruncateToMilliseconds(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };
        return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
    }
}
