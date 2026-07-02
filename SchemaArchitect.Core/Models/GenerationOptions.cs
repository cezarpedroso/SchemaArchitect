namespace SchemaArchitect.Core.Models;

/// <summary>
/// Defines which artifacts a code-generation operation should produce.
/// </summary>
public sealed record GenerationOptions
{
    /// <summary>
    /// Gets the root namespace used by generated C# files.
    /// </summary>
    public string RootNamespace { get; init; } = "SchemaArchitect.Generated";

    /// <summary>
    /// Gets the name assigned to the generated database context.
    /// </summary>
    public string DbContextName { get; init; } = "ApplicationDbContext";

    /// <summary>
    /// Gets a value indicating whether entity classes should be generated.
    /// </summary>
    public bool GenerateEntities { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether a database context should be generated.
    /// </summary>
    public bool GenerateDbContext { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether Fluent API configurations should be generated.
    /// </summary>
    public bool GenerateConfigurations { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether data transfer objects should be generated.
    /// </summary>
    public bool GenerateDtos { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether API controllers should be generated.
    /// </summary>
    public bool GenerateControllers { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether EF Core migration instructions should be generated.
    /// </summary>
    public bool GenerateMigrationInstructions { get; init; } = true;
}
