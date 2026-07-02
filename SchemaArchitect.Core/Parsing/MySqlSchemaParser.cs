using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Parses MySQL schema scripts into Schema Architect schema models.
/// </summary>
public sealed class MySqlSchemaParser : ISchemaParser
{
    private readonly CreateTableSchemaParser parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlSchemaParser"/> class.
    /// </summary>
    public MySqlSchemaParser()
        : this(new MySqlTypeMapper())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MySqlSchemaParser"/> class.
    /// </summary>
    /// <param name="typeMapper">The MySQL type mapper.</param>
    public MySqlSchemaParser(ISqlTypeMapper typeMapper)
    {
        ArgumentNullException.ThrowIfNull(typeMapper);

        parser = new CreateTableSchemaParser(new CreateTableSchemaParserOptions
        {
            Dialect = SqlDialect.MySql,
            DefaultSchema = "default",
            TypeMapper = typeMapper,
            SupportsHashLineComments = true,
            IdentityRegex = new Regex(
                @"\bAUTO_INCREMENT\b",
                RegexOptions.IgnoreCase | RegexOptions.Compiled),
        });
    }

    /// <inheritdoc />
    public Task<DatabaseSchema> ParseAsync(string schemaSql, CancellationToken cancellationToken = default)
    {
        return parser.ParseAsync(schemaSql, cancellationToken);
    }
}
