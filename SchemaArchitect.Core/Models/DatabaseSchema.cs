namespace SchemaArchitect.Core.Models;

/// <summary>
/// Represents the database described by an uploaded SQL schema script.
/// </summary>
public sealed record DatabaseSchema
{
    /// <summary>
    /// Gets the SQL dialect used to parse the schema.
    /// </summary>
    public SqlDialect Dialect { get; init; } = SqlDialect.SqlServer;

    /// <summary>
    /// Gets the database name, when one is declared by the schema script.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the tables contained in the database schema.
    /// </summary>
    public IReadOnlyList<TableSchema> Tables { get; init; } = [];
}
