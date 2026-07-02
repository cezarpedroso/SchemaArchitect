using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Interfaces;

/// <summary>
/// Defines a component that produces source files from a database schema.
/// </summary>
public interface ICodeGenerator
{
    /// <summary>
    /// Generates source files asynchronously for the supplied schema and options.
    /// </summary>
    /// <param name="schema">The database schema to transform.</param>
    /// <param name="options">The generation options to apply.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The generated source files.</returns>
    Task<IReadOnlyList<GeneratedFile>> GenerateAsync(
        DatabaseSchema schema,
        GenerationOptions options,
        CancellationToken cancellationToken = default);
}
