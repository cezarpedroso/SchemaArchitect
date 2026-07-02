using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Analyzes parsed schemas for generation readiness warnings.
/// </summary>
public interface ISchemaHealthAnalyzer
{
    /// <summary>
    /// Analyzes the parsed schema.
    /// </summary>
    /// <param name="schema">The parsed schema.</param>
    /// <returns>The schema health report.</returns>
    SchemaHealthReport Analyze(DatabaseSchema schema);
}
