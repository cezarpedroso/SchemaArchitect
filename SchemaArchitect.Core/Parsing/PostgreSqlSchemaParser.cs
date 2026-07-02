using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses PostgreSQL schema scripts into Schema Architect schema models.
/// </summary>
public sealed class PostgreSqlSchemaParser : ISchemaParser
{
    private readonly CreateTableSchemaParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSchemaParser"/> class.
    /// </summary>
    public PostgreSqlSchemaParser()
        : this(new PostgreSqlTypeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgreSqlSchemaParser"/> class.
    /// </summary>
    /// <param name="typeMapper">The PostgreSQL type mapper.</param>
    public PostgreSqlSchemaParser(ISqlTypeMapper typeMapper)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);

        parser = new CreateTableSchemaParser(new CreateTableSchemaParserOptions
        {
            Dialect = SqlDialect.PostgreSql,
            DefaultSchema = "public",
            TypeMapper = typeMapper,
            IdentityRegex = new Regex(
                @"\bGENERATED\s+(?:ALWAYS|BY\s+DEFAULT)\s+AS\s+IDENTITY\b|\b(?:SMALLSERIAL|SERIAL2|SERIAL|SERIAL4|BIGSERIAL|SERIAL8)\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        });
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(string schemaSql, CancellationToken cancellationToken = default)
    {
        return parser.ParseAsync(schemaSql, cancellationToken);
    }
}
