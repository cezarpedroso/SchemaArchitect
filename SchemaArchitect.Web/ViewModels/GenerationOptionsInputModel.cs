using System.ComponentModel.DataAnnotations;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.ViewModels;

/// <summary>
/// Represents generation options submitted from the schema preview page.
/// </summary>
public sealed class GenerationOptionsInputModel
{
    /// <summary>
    /// Gets or sets the root namespace for generated C# files.
    /// </summary>
    [Required]
    [StringLength(120)]
    [RegularExpression(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)*$", ErrorMessage = "Use a valid C# namespace.")]
    public string RootNamespace { get; set; } = "SchemaArchitect.Generated";

    /// <summary>
    /// Gets or sets the generated DbContext class name.
    /// </summary>
    [Required]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-z_][A-Za-z0-9_]*$", ErrorMessage = "Use a valid C# class name.")]
    public string DbContextName { get; set; } = "ApplicationDbContext";

    /// <summary>
    /// Gets or sets a value indicating whether entities should be generated.
    /// </summary>
    public bool GenerateEntities { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether DbContext should be generated.
    /// </summary>
    public bool GenerateDbContext { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether Fluent API configurations should be generated.
    /// </summary>
    public bool GenerateConfigurations { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether DTOs should be generated.
    /// </summary>
    public bool GenerateDtos { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether controllers should be generated.
    /// </summary>
    public bool GenerateControllers { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether migration instructions should be generated.
    /// </summary>
    public bool GenerateMigrationInstructions { get; set; } = true;

    /// <summary>
    /// Converts the input model to core generation options.
    /// </summary>
    /// <returns>The generation options.</returns>
    public GenerationOptions ToGenerationOptions()
    {
        return new GenerationOptions
        {
            RootNamespace = RootNamespace,
            DbContextName = DbContextName,
            GenerateEntities = GenerateEntities,
            GenerateDbContext = GenerateDbContext,
            GenerateConfigurations = GenerateConfigurations,
            GenerateDtos = GenerateDtos,
            GenerateControllers = GenerateControllers,
            GenerateMigrationInstructions = GenerateMigrationInstructions,
        };
    }

    /// <summary>
    /// Creates an input model from core generation options.
    /// </summary>
    /// <param name="options">The source generation options.</param>
    /// <returns>The created input model.</returns>
    public static GenerationOptionsInputModel FromGenerationOptions(GenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new GenerationOptionsInputModel
        {
            RootNamespace = options.RootNamespace,
            DbContextName = options.DbContextName,
            GenerateEntities = options.GenerateEntities,
            GenerateDbContext = options.GenerateDbContext,
            GenerateConfigurations = options.GenerateConfigurations,
            GenerateDtos = options.GenerateDtos,
            GenerateControllers = options.GenerateControllers,
            GenerateMigrationInstructions = options.GenerateMigrationInstructions,
        };
    }
}
