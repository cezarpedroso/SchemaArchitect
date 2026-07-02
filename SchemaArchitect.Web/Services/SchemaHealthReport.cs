namespace SchemaArchitect.Web.Services;

/// <summary>
/// Represents a complete schema health report for a parsed schema.
/// </summary>
public sealed record SchemaHealthReport
{
    /// <summary>
    /// Gets the number of parsed tables.
    /// </summary>
    public int TableCount { get; init; }

    /// <summary>
    /// Gets the number of parsed columns.
    /// </summary>
    public int ColumnCount { get; init; }

    /// <summary>
    /// Gets the number of primary-key columns.
    /// </summary>
    public int PrimaryKeyColumnCount { get; init; }

    /// <summary>
    /// Gets the number of foreign-key relationships.
    /// </summary>
    public int ForeignKeyCount { get; init; }

    /// <summary>
    /// Gets the number of parsed indexes.
    /// </summary>
    public int IndexCount { get; init; }

    /// <summary>
    /// Gets the number of required columns.
    /// </summary>
    public int RequiredColumnCount { get; init; }

    /// <summary>
    /// Gets the number of nullable columns.
    /// </summary>
    public int NullableColumnCount { get; init; }

    /// <summary>
    /// Gets the number of ready items.
    /// </summary>
    public int ReadyCount => Items.Count(item => item.Severity == SchemaHealthSeverity.Ready);

    /// <summary>
    /// Gets the number of warning items.
    /// </summary>
    public int WarningCount => Items.Count(item => item.Severity == SchemaHealthSeverity.Warning);

    /// <summary>
    /// Gets the number of issue items.
    /// </summary>
    public int IssueCount => Items.Count(item => item.Severity == SchemaHealthSeverity.Issue);

    /// <summary>
    /// Gets the overall report severity.
    /// </summary>
    public SchemaHealthSeverity OverallSeverity =>
        IssueCount > 0 ? SchemaHealthSeverity.Issue :
        WarningCount > 0 ? SchemaHealthSeverity.Warning :
        SchemaHealthSeverity.Ready;

    /// <summary>
    /// Gets health findings.
    /// </summary>
    public IReadOnlyList<SchemaHealthItem> Items { get; init; } = [];
}
