namespace SchemaArchitect.Core.Models;

/// <summary>
/// Represents a table discovered in a database schema.
/// </summary>
public sealed record TableSchema
{
    /// <summary>
    /// Gets the database schema that owns the table.
    /// </summary>
    public string Schema { get; init; } = "dbo";

    /// <summary>
    /// Gets the table name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the columns declared by the table.
    /// </summary>
    public IReadOnlyList<ColumnSchema> Columns { get; init; } = [];

    /// <summary>
    /// Gets the foreign keys declared by the table.
    /// </summary>
    public IReadOnlyList<ForeignKeySchema> ForeignKeys { get; init; } = [];

    /// <summary>
    /// Gets the indexes declared for the table.
    /// </summary>
    public IReadOnlyList<IndexSchema> Indexes { get; init; } = [];

    /// <summary>
    /// Gets the columns that participate in the table primary key.
    /// </summary>
    public IReadOnlyList<ColumnSchema> PrimaryKey => Columns
        .Where(static column => column.IsPrimaryKey)
        .ToArray();

    /// <summary>
    /// Gets the columns that participate in one or more foreign-key relationships.
    /// </summary>
    public IReadOnlyList<ColumnSchema> ForeignKeyColumns
    {
        get
        {
            var foreignKeyColumnNames = ForeignKeys
                .SelectMany(static foreignKey => foreignKey.Columns)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return Columns
                .Where(column => foreignKeyColumnNames.Contains(column.Name))
                .ToArray();
        }
    }
}
