using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses Oracle Database schema scripts into Schema Architect schema models.
/// </summary>
public sealed class OracleSchemaParser : ISchemaParser
{
    private readonly CreateTableSchemaParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleSchemaParser"/> class.
    /// </summary>
    public OracleSchemaParser()
        : this(new OracleTypeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OracleSchemaParser"/> class.
    /// </summary>
    /// <param name="typeMapper">The Oracle type mapper.</param>
    public OracleSchemaParser(ISqlTypeMapper typeMapper)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);

        parser = new CreateTableSchemaParser(new CreateTableSchemaParserOptions
        {
            Dialect = SqlDialect.Oracle,
            DefaultSchema = "default",
            TypeMapper = typeMapper,
            IdentityRegex = new Regex(
                @"\bGENERATED\s+(?:ALWAYS|BY\s+DEFAULT(?:\s+ON\s+NULL)?)\s+AS\s+IDENTITY\b|\bIDENTITY\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        });
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(string schemaSql, CancellationToken cancellationToken = default)
    {
        return parser.ParseAsync(schemaSql, cancellationToken);
    }
}
