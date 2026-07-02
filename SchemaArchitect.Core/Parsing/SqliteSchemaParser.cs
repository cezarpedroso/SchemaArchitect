using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses SQLite schema scripts into Schema Architect schema models.
/// </summary>
public sealed class SqliteSchemaParser : ISchemaParser
{
    private readonly CreateTableSchemaParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSchemaParser"/> class.
    /// </summary>
    public SqliteSchemaParser()
        : this(new SqliteTypeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteSchemaParser"/> class.
    /// </summary>
    /// <param name="typeMapper">The SQLite type mapper.</param>
    public SqliteSchemaParser(ISqlTypeMapper typeMapper)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);

        parser = new CreateTableSchemaParser(new CreateTableSchemaParserOptions
        {
            Dialect = SqlDialect.SQLite,
            DefaultSchema = "main",
            TypeMapper = typeMapper,
            IdentityRegex = new Regex(
                @"\bAUTOINCREMENT\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        });
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(string schemaSql, CancellationToken cancellationToken = default)
    {
        return parser.ParseAsync(schemaSql, cancellationToken);
    }
}
