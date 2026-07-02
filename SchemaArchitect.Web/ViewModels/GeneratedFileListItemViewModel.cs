namespace SchemaArchitect.Web.ViewModels;

/// <summary>
/// Represents one generated file in the code preview list.
/// </summary>
public sealed class GeneratedFileListItemViewModel
{
    /// <summary>
    /// Gets or sets the zero-based file index in the generation result.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the generated file path.
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this file has user edits.
    /// </summary>
    public bool IsEdited { get; set; }
}
