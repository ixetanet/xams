using System.Linq.Dynamic.Core;
using Xams.Core.Base;
using Xams.Core.Dtos.Data;

namespace Xams.Core.Utils;

public class QueryUtil
{
    public static string[] GetTables(ReadInput? readInput)
    {
        List<string> results = new();
        
        if (readInput == null) return results.ToArray();
            
        results.Add(readInput.tableName);
            
        if (readInput.joins != null)
        {
            foreach (var join in readInput.joins)
            {
                results.Add(join.toTable);
            }
        }

        if (readInput.except != null)
        {
            foreach (var exclude in readInput.except)
            {
                results.AddRange(GetTables(exclude.query));
            }
        }

        return results.ToArray();
    }
    
}