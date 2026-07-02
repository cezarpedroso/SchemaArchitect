using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Interfaces;

/// <summary>
/// Resolves the schema parser for a selected SQL dialect.
/// </summary>
public interface ISchemaParserResolver
{
    /// <summary>
    /// Resolves a parser for the selected SQL dialect.
    /// </summary>
    /// <param name="dialect">The selected SQL dialect.</param>
    /// <returns>The matching schema parser.</returns>
    ISchemaParser Resolve(SqlDialect dialect);
}
