using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Interfaces;

/// <summary>
/// Defines a component that converts a SQL schema script into a database model.
/// </summary>
public interface ISchemaParser
{
    /// <summary>
    /// Parses a SQL schema script asynchronously.
    /// </summary>
    /// <param name="schemaSql">The SQL schema script to parse.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The parsed database schema.</returns>
    Task<DatabaseSchema> ParseAsync(
        string schemaSql,
        CancellationToken cancellationToken = default);
}
