namespace SchemaArchitect.Core.Models;

/// <summary>
/// Represents a foreign-key relationship declared by a table.
/// </summary>
public sealed record ForeignKeySchema
{
    /// <summary>
    /// Gets the foreign-key constraint name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the columns on the dependent table.
    /// </summary>
    public IReadOnlyList<string> Columns { get; init; } = [];

    /// <summary>
    /// Gets the schema containing the referenced table.
    /// </summary>
    public string PrincipalSchema { get; init; } = "dbo";

    /// <summary>
    /// Gets the referenced table name.
    /// </summary>
    public required string PrincipalTable { get; init; }

    /// <summary>
    /// Gets the referenced columns in constraint order.
    /// </summary>
    public IReadOnlyList<string> PrincipalColumns { get; init; } = [];

    /// <summary>
    /// Gets the schema containing the referenced table.
    /// </summary>
    public string ReferencedSchema => PrincipalSchema;

    /// <summary>
    /// Gets the referenced table name.
    /// </summary>
    public string ReferencedTable => PrincipalTable;

    /// <summary>
    /// Gets the referenced columns in constraint order.
    /// </summary>
    public IReadOnlyList<string> ReferencedColumns => PrincipalColumns;

    /// <summary>
    /// Gets the dependent column for single-column foreign keys.
    /// </summary>
    public string? Column => Columns.Count == 1 ? Columns[0] : null;

    /// <summary>
    /// Gets the referenced column for single-column foreign keys.
    /// </summary>
    public string? ReferencedColumn => ReferencedColumns.Count == 1 ? ReferencedColumns[0] : null;
}
