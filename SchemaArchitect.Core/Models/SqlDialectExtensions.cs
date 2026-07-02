namespace SchemaArchitect.Core.Models;

/// <summary>
/// Provides display helpers for SQL dialect values.
/// </summary>
public static class SqlDialectExtensions
{
    /// <summary>
    /// Gets the user-facing display name for a SQL dialect.
    /// </summary>
    /// <param name="dialect">The SQL dialect.</param>
    /// <returns>The display name.</returns>
    public static string GetDisplayName(this SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.SqlServer => "SQL Server",
            SqlDialect.MySql => "MySQL",
            SqlDialect.SQLite => "SQLite",
            SqlDialect.IbmDb2 => "IBM Db2",
            SqlDialect.Oracle => "Oracle",
            SqlDialect.PostgreSql => "PostgreSQL",
            _ => dialect.ToString(),
        };
    }
}
