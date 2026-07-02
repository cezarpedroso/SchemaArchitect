namespace SchemaArchitect.Core.Models;

/// <summary>
/// Defines the high-level architecture used when organizing generated files.
/// </summary>
public enum GenerationTemplate
{
    /// <summary>
    /// Generates the current EF Core-oriented folder structure.
    /// </summary>
    StandardEfCore = 0,

    /// <summary>
    /// Organizes generated code around Domain, Application, Infrastructure, and API layers.
    /// </summary>
    CleanArchitecture = 1,

    /// <summary>
    /// Organizes generated code around domain-driven design concepts.
    /// </summary>
    Ddd = 2,

    /// <summary>
    /// Generates minimal API endpoint artifacts instead of controller artifacts.
    /// </summary>
    MinimalApi = 3,

    /// <summary>
    /// Generates a traditional controller-based Web API structure.
    /// </summary>
    MvcApi = 4,
}
