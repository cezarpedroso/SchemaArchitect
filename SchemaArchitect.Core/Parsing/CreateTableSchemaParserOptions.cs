using System.Text.RegularExpressions;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Parsing;

/// <summary>
/// Configures dialect-specific behavior for the shared CREATE TABLE parser.
/// </summary>
internal sealed record CreateTableSchemaParserOptions
{
    /// <summary>
    /// Gets the SQL dialect represented by the parser.
    /// </summary>
    public required SqlDialect Dialect { get; init; }

    /// <summary>
    /// Gets the default schema name used when a table name is not schema-qualified.
    /// </summary>
    public required string DefaultSchema { get; init; }

    /// <summary>
    /// Gets the SQL type mapper used to populate C# type metadata.
    /// </summary>
    public required ISqlTypeMapper TypeMapper { get; init; }

    /// <summary>
    /// Gets a value indicating whether hash line comments should be removed.
    /// </summary>
    public bool SupportsHashLineComments { get; init; }

    /// <summary>
    /// Gets the regular expression used to detect identity/autoincrement columns.
    /// </summary>
    public Regex IdentityRegex { get; init; } = new(
        @"\bIDENTITY\b|\bAUTO_INCREMENT\b|\bAUTOINCREMENT\b|\bSERIAL\b|\bGENERATED\s+(?:ALWAYS|BY\s+DEFAULT)?\s+AS\s+IDENTITY\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Gets SQL type names that use a length facet.
    /// </summary>
    public IReadOnlySet<string> LengthTypes { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "char",
            "character",
            "nchar",
            "varchar",
            "character varying",
            "varying character",
            "nvarchar",
            "national character",
            "national character varying",
            "varchar2",
            "nvarchar2",
            "raw",
            "binary",
            "varbinary",
            "bit",
        };

    /// <summary>
    /// Gets SQL type names that use precision and optional scale facets.
    /// </summary>
    public IReadOnlySet<string> PrecisionScaleTypes { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "decimal",
            "dec",
            "numeric",
            "number",
        };
}
