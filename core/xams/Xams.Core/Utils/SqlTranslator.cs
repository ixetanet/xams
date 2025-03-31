using System.Text.RegularExpressions;
using Xams.Core.Base;

namespace Xams.Core.Utils;

public class SqlTranslator
{
    public static string Translate(string postgresqlQuery, DbProvider targetDb)
    {
        string translatedQuery = postgresqlQuery;

        // Replace double quotes with backticks for MySQL and SQLite, or with square brackets for SQL Server
        if (targetDb is DbProvider.MySql or DbProvider.SqLite)
        {
            translatedQuery = Regex.Replace(translatedQuery, "\"([^\"]+)\"", "`$1`");
        }
        else if (targetDb == DbProvider.SqlServer)
        {
            translatedQuery = Regex.Replace(translatedQuery, "\"([^\"]+)\"", "[$1]");
        }
        else
        {
            translatedQuery = postgresqlQuery;
        }

        return translatedQuery;
    }
}

