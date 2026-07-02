namespace SchemaArchitect.Core.Models;

/// <summary>
/// Represents an index discovered for a table.
/// </summary>
public sealed record IndexSchema
{
    /// <summary>
    /// Gets the index name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether the index is unique.
    /// </summary>
    public bool IsUnique { get; init; }

    /// <summary>
    /// Gets the indexed columns in declared order.
    /// </summary>
    public IReadOnlyList<string> Columns { get; init; } = [];
}
