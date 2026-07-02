namespace SchemaArchitect.Core.Models;

/// <summary>
/// Represents one source file produced by code generation.
/// </summary>
public sealed record GeneratedFile
{
    /// <summary>
    /// Gets the output path relative to the generation root.
    /// </summary>
    public required string RelativePath { get; init; }

    /// <summary>
    /// Gets the generated source text.
    /// </summary>
    public required string Content { get; init; }
}
