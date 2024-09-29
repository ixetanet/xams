using Xams.Core.Dtos.Data;
using Xams.Core.Utils;

namespace Xams.Core.Extensions;

public static class ExtensionMethods
{
    public static string[] GetTables(this ReadInput? readInput)
    {
        // Get all tables
        return QueryUtil.GetTables(readInput);
    }
}