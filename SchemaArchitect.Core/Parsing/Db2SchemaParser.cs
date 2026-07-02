using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses IBM Db2 schema scripts into Schema Architect schema models.
/// </summary>
public sealed class Db2SchemaParser : ISchemaParser
{
    private readonly CreateTableSchemaParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="Db2SchemaParser"/> class.
    /// </summary>
    public Db2SchemaParser()
        : this(new Db2TypeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Db2SchemaParser"/> class.
    /// </summary>
    /// <param name="typeMapper">The IBM Db2 type mapper.</param>
    public Db2SchemaParser(ISqlTypeMapper typeMapper)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);

        parser = new CreateTableSchemaParser(new CreateTableSchemaParserOptions
        {
            Dialect = SqlDialect.IbmDb2,
            DefaultSchema = "default",
            TypeMapper = typeMapper,
            IdentityRegex = new Regex(
                @"\bGENERATED\s+(?:ALWAYS|BY\s+DEFAULT)\s+AS\s+IDENTITY\b|\bIDENTITY\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        });
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(string schemaSql, CancellationToken cancellationToken = default)
    {
        return parser.ParseAsync(schemaSql, cancellationToken);
    }
}
