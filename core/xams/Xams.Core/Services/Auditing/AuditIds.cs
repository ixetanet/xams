using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Xams.Core.Base;

namespace Xams.Core.Services.Auditing;

/// <summary>
/// Time-ordered keys for audit rows, so new rows go to the end of the key indexes rather than onto random pages.
/// Which byte order sorts by time depends on the provider: SQL Server compares uniqueidentifier values from their
/// last bytes, while PostgreSQL, and SQLite and MySQL as text, compare them in written order, as UUIDv7 expects.
/// </summary>
internal static class AuditIds
{
    // Unix milliseconds shifted left 12 bits, plus a sequence for keys within the same millisecond
    private static long _last;

    public static Func<Guid> For(IXamsDbContext db)
    {
        return ((DbContext)db).Database.ProviderName == "Microsoft.EntityFrameworkCore.SqlServer"
            ? NewSqlServerId
            : NewVersion7;
    }

    /// <summary>
    /// A stamp greater than any given out before: the current millisecond, or one more than the last stamp when
    /// the clock has not moved on.
    /// </summary>
    private static long NextStamp()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() << 12;
        while (true)
        {
            var last = Interlocked.Read(ref _last);
            var next = Math.Max(now, last + 1);
            if (Interlocked.CompareExchange(ref _last, next, last) == last)
            {
                return next;
            }
        }
    }

    /// <summary>
    /// An RFC 9562 version 7 UUID: 48 bits of Unix milliseconds, the version, a 12-bit sequence, the variant and
    /// 62 random bits.
    /// </summary>
    public static Guid NewVersion7()
    {
        var stamp = NextStamp();
        var milliseconds = stamp >> 12;
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes[8..]);
        for (var i = 0; i < 6; i++)
        {
            bytes[i] = (byte)(milliseconds >> (40 - 8 * i));
        }

        bytes[6] = (byte)(0x70 | ((stamp >> 8) & 0x0F));
        bytes[7] = (byte)stamp;
        bytes[8] = (byte)(0x80 | (bytes[8] & 0x3F));
        return new Guid(bytes, bigEndian: true);
    }

    /// <summary>
    /// SQL Server compares uniqueidentifier bytes 10–15 first, then 8–9 (in Guid.ToByteArray order), so the
    /// milliseconds go in bytes 10–15, the sequence in 8–9 and random bytes in the rest.
    /// </summary>
    public static Guid NewSqlServerId()
    {
        var stamp = NextStamp();
        var milliseconds = stamp >> 12;
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes[..8]);
        bytes[8] = (byte)((stamp >> 8) & 0x0F);
        bytes[9] = (byte)stamp;
        for (var i = 0; i < 6; i++)
        {
            bytes[10 + i] = (byte)(milliseconds >> (40 - 8 * i));
        }

        return new Guid(bytes);
    }
}
