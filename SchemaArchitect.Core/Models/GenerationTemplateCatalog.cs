namespace SchemaArchitect.Core.Models;

/// <summary>
/// Provides metadata for supported generation templates.
/// </summary>
public static class GenerationTemplateCatalog
{
    private static readonly IReadOnlyList<GenerationTemplateDescriptor> Descriptors =
    [
        new(
            GenerationTemplate.StandardEfCore,
            "Standard EF Core",
            "Generates the current Schema Architect structure: Domain/Entities, Infrastructure/Data, Infrastructure/Configurations, Application/DTOs, and API/Controllers.",
            "Domain/Entities",
            "Domain.Entities",
            "Infrastructure/Data",
            "Infrastructure.Data",
            "Infrastructure/Configurations",
            "Infrastructure.Configurations",
            "Application/DTOs",
            "Application.DTOs",
            "API/Controllers",
            "API.Controllers",
            false),
        new(
            GenerationTemplate.CleanArchitecture,
            "Clean Architecture",
            "Organizes output into Domain, Application, Infrastructure, and API layers with EF Core infrastructure isolated from generated domain entities.",
            "Domain/Entities",
            "Domain.Entities",
            "Infrastructure/Data",
            "Infrastructure.Data",
            "Infrastructure/Configurations",
            "Infrastructure.Configurations",
            "Application/DTOs",
            "Application.DTOs",
            "API/Controllers",
            "API.Controllers",
            false),
        new(
            GenerationTemplate.Ddd,
            "DDD",
            "Groups domain models under Domain/Aggregates while keeping DTOs, EF Core infrastructure, and API entry points separate.",
            "Domain/Aggregates",
            "Domain.Aggregates",
            "Infrastructure/Persistence",
            "Infrastructure.Persistence",
            "Infrastructure/Persistence/Configurations",
            "Infrastructure.Persistence.Configurations",
            "Application/Contracts",
            "Application.Contracts",
            "API/Controllers",
            "API.Controllers",
            false),
        new(
            GenerationTemplate.MinimalApi,
            "Minimal API",
            "Generates EF Core entities, infrastructure, DTO contracts, and endpoint mapping classes under API/Endpoints instead of controllers.",
            "Domain/Entities",
            "Domain.Entities",
            "Infrastructure/Data",
            "Infrastructure.Data",
            "Infrastructure/Configurations",
            "Infrastructure.Configurations",
            "Application/DTOs",
            "Application.DTOs",
            "API/Endpoints",
            "API.Endpoints",
            true),
        new(
            GenerationTemplate.MvcApi,
            "MVC API",
            "Generates a traditional controller-based Web API structure with models, DTOs, EF Core infrastructure, and API controllers.",
            "Domain/Entities",
            "Domain.Entities",
            "Infrastructure/Data",
            "Infrastructure.Data",
            "Infrastructure/Configurations",
            "Infrastructure.Configurations",
            "Application/DTOs",
            "Application.DTOs",
            "API/Controllers",
            "API.Controllers",
            false),
    ];

    /// <summary>
    /// Gets all supported generation template descriptors.
    /// </summary>
    /// <returns>The available descriptors.</returns>
    public static IReadOnlyList<GenerationTemplateDescriptor> GetTemplates()
    {
        return Descriptors;
    }

    /// <summary>
    /// Gets the descriptor for the specified template.
    /// </summary>
    /// <param name="template">The selected generation template.</param>
    /// <returns>The matching template descriptor.</returns>
    public static GenerationTemplateDescriptor GetTemplate(GenerationTemplate template)
    {
        return Descriptors.FirstOrDefault(descriptor => descriptor.Template == template) ??
            Descriptors[0];
    }
}
