namespace SchemaArchitect.Core.Models;

/// <summary>
/// Describes the UI label and structural conventions for a generation template.
/// </summary>
/// <param name="Template">The template identifier.</param>
/// <param name="DisplayName">The human-readable template name.</param>
/// <param name="Description">A short explanation of the generated project structure.</param>
/// <param name="EntityPath">The folder where entity files are generated.</param>
/// <param name="EntityNamespace">The namespace suffix used for generated entities.</param>
/// <param name="DbContextPath">The folder where the DbContext is generated.</param>
/// <param name="DbContextNamespace">The namespace suffix used for the generated DbContext.</param>
/// <param name="ConfigurationPath">The folder where Fluent API configurations are generated.</param>
/// <param name="ConfigurationNamespace">The namespace suffix used for Fluent API configurations.</param>
/// <param name="DtoPath">The folder where DTOs are generated.</param>
/// <param name="DtoNamespace">The namespace suffix used for DTOs.</param>
/// <param name="ApiPath">The folder where API artifacts are generated.</param>
/// <param name="ApiNamespace">The namespace suffix used for API artifacts.</param>
/// <param name="GeneratesMinimalEndpoints">A value indicating whether API artifacts should be minimal API endpoints.</param>
public sealed record GenerationTemplateDescriptor(
    GenerationTemplate Template,
    string DisplayName,
    string Description,
    string EntityPath,
    string EntityNamespace,
    string DbContextPath,
    string DbContextNamespace,
    string ConfigurationPath,
    string ConfigurationNamespace,
    string DtoPath,
    string DtoNamespace,
    string ApiPath,
    string ApiNamespace,
    bool GeneratesMinimalEndpoints);
