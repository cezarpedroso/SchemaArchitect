using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Represents one temporary upload, parse, and generation workflow.
/// </summary>
public sealed class GenerationSession
{
    /// <summary>
    /// Gets the opaque session identifier used by the browser flow.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the original uploaded file name.
    /// </summary>
    public required string OriginalFileName { get; init; }

    /// <summary>
    /// Gets the SQL dialect selected for the uploaded schema.
    /// </summary>
    public SqlDialect Dialect { get; init; } = SqlDialect.SqlServer;

    /// <summary>
    /// Gets the parsed database schema.
    /// </summary>
    public required DatabaseSchema Schema { get; init; }

    /// <summary>
    /// Gets or sets the generation options last selected by the user.
    /// </summary>
    public GenerationOptions Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the generated files for the session.
    /// </summary>
    public IReadOnlyList<GeneratedFile> GeneratedFiles { get; set; } = [];

    /// <summary>
    /// Gets or sets the original generated files before user edits.
    /// </summary>
    public IReadOnlyList<GeneratedFile> OriginalGeneratedFiles { get; set; } = [];

    /// <summary>
    /// Gets the time at which the session was created.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the time at which the session was last accessed.
    /// </summary>
    public DateTimeOffset LastAccessedUtc { get; set; } = DateTimeOffset.UtcNow;
}
