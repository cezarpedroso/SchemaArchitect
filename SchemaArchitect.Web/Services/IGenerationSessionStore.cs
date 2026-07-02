using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Stores temporary schema parsing and code generation output for the web flow.
/// </summary>
public interface IGenerationSessionStore
{
    /// <summary>
    /// Creates a temporary generation session.
    /// </summary>
    /// <param name="originalFileName">The uploaded file name.</param>
    /// <param name="schema">The parsed database schema.</param>
    /// <param name="dialect">The SQL dialect selected for parsing.</param>
    /// <returns>The created session.</returns>
    GenerationSession Create(string originalFileName, DatabaseSchema schema, SqlDialect dialect);

    /// <summary>
    /// Attempts to retrieve an existing temporary generation session.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="session">The matching session when found.</param>
    /// <returns><see langword="true"/> when the session exists; otherwise, <see langword="false"/>.</returns>
    bool TryGet(string id, out GenerationSession? session);

    /// <summary>
    /// Saves generated files for an existing temporary generation session.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="options">The generation options used.</param>
    /// <param name="files">The generated files.</param>
    void SaveGeneratedFiles(string id, GenerationOptions options, IReadOnlyList<GeneratedFile> files);

    /// <summary>
    /// Saves edited content for a generated file in an existing temporary session.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="fileIndex">The zero-based generated file index.</param>
    /// <param name="content">The edited file content.</param>
    /// <returns><see langword="true"/> when the file was updated; otherwise, <see langword="false"/>.</returns>
    bool TryUpdateGeneratedFileContent(string id, int fileIndex, string content);

    /// <summary>
    /// Restores a generated file to its original generated content.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="fileIndex">The zero-based generated file index.</param>
    /// <returns><see langword="true"/> when the file was restored; otherwise, <see langword="false"/>.</returns>
    bool TryRestoreGeneratedFile(string id, int fileIndex);
}
