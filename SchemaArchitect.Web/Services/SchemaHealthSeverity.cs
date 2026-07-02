namespace SchemaArchitect.Web.Services;

/// <summary>
/// Represents the severity of a schema health finding.
/// </summary>
public enum SchemaHealthSeverity
{
    /// <summary>
    /// Indicates the schema item is ready for generation.
    /// </summary>
    Ready,

    /// <summary>
    /// Indicates the schema item may need review but does not block generation.
    /// </summary>
    Warning,

    /// <summary>
    /// Indicates the schema item is likely to produce incomplete or risky generated code.
    /// </summary>
    Issue,
}
