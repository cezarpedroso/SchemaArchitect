using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Creates downloadable archives from generated files.
/// </summary>
public interface IGeneratedFileArchiveService
{
    /// <summary>
    /// Creates a ZIP archive containing generated files.
    /// </summary>
    /// <param name="files">The generated files to archive.</param>
    /// <returns>The ZIP archive bytes.</returns>
    byte[] CreateZipArchive(IReadOnlyList<GeneratedFile> files);
}
