namespace SchemaArchitect.Web.Services;

/// <summary>
/// Represents one schema health finding.
/// </summary>
public sealed record SchemaHealthItem
{
    /// <summary>
    /// Gets the severity of the finding.
    /// </summary>
    public SchemaHealthSeverity Severity { get; init; }

    /// <summary>
    /// Gets the finding title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets a short explanation for the finding.
    /// </summary>
    public required string Explanation { get; init; }

    /// <summary>
    /// Gets the affected table name.
    /// </summary>
    public string? TableName { get; init; }

    /// <summary>
    /// Gets the affected column name, when applicable.
    /// </summary>
    public string? ColumnName { get; init; }
}
