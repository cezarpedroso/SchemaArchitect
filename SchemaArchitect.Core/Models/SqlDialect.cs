namespace SchemaArchitect.Core.Models;

/// <summary>
/// Identifies the SQL dialect or database engine used by an uploaded schema script.
/// </summary>
public enum SqlDialect
{
    /// <summary>
    /// Microsoft SQL Server Transact-SQL schema syntax.
    /// </summary>
    SqlServer = 0,

    /// <summary>
    /// MySQL schema syntax.
    /// </summary>
    MySql = 1,

    /// <summary>
    /// SQLite schema syntax.
    /// </summary>
    SQLite = 2,

    /// <summary>
    /// IBM Db2 schema syntax.
    /// </summary>
    IbmDb2 = 3,

    /// <summary>
    /// Oracle Database schema syntax.
    /// </summary>
    Oracle = 4,

    /// <summary>
    /// PostgreSQL schema syntax.
    /// </summary>
    PostgreSql = 5,
}
