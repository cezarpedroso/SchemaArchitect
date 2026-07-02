using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Resolves schema parsers by SQL dialect.
/// </summary>
public sealed class SchemaParserResolver : ISchemaParserResolver
{
    private readonly SqlServerSchemaParser sqlServerParser;
    private readonly MySqlSchemaParser mySqlParser;
    private readonly SqliteSchemaParser sqliteParser;
    private readonly Db2SchemaParser db2Parser;
    private readonly OracleSchemaParser oracleParser;
    private readonly PostgreSqlSchemaParser postgreSqlParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaParserResolver"/> class.
    /// </summary>
    /// <param name="sqlServerParser">The SQL Server schema parser.</param>
    /// <param name="mySqlParser">The MySQL schema parser.</param>
    /// <param name="sqliteParser">The SQLite schema parser.</param>
    /// <param name="db2Parser">The IBM Db2 schema parser.</param>
    /// <param name="oracleParser">The Oracle schema parser.</param>
    /// <param name="postgreSqlParser">The PostgreSQL schema parser.</param>
    public SchemaParserResolver(
        SqlServerSchemaParser sqlServerParser,
        MySqlSchemaParser mySqlParser,
        SqliteSchemaParser sqliteParser,
        Db2SchemaParser db2Parser,
        OracleSchemaParser oracleParser,
        PostgreSqlSchemaParser postgreSqlParser)
    {
        this.sqlServerParser = sqlServerParser ?? throw new ArgumentNullException(nameof(sqlServerParser));
        this.mySqlParser = mySqlParser ?? throw new ArgumentNullException(nameof(mySqlParser));
        this.sqliteParser = sqliteParser ?? throw new ArgumentNullException(nameof(sqliteParser));
        this.db2Parser = db2Parser ?? throw new ArgumentNullException(nameof(db2Parser));
        this.oracleParser = oracleParser ?? throw new ArgumentNullException(nameof(oracleParser));
        this.postgreSqlParser = postgreSqlParser ?? throw new ArgumentNullException(nameof(postgreSqlParser));
    }

    /// <inheritdoc />
    public ISchemaParser Resolve(SqlDialect dialect)
    {
        return dialect switch
        {
            SqlDialect.SqlServer => sqlServerParser,
            SqlDialect.MySql => mySqlParser,
            SqlDialect.SQLite => sqliteParser,
            SqlDialect.IbmDb2 => db2Parser,
            SqlDialect.Oracle => oracleParser,
            SqlDialect.PostgreSql => postgreSqlParser,
            _ => throw new ArgumentOutOfRangeException(nameof(dialect), dialect, "Unknown SQL dialect."),
        };
    }
}
