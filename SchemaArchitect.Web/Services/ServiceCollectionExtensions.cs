using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Provides dependency-injection registration for the web application.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Razor Pages and the Schema Architect application services.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection so that calls can be chained.</returns>
    public static IServiceCollection AddSchemaArchitectWeb(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRazorPages();
        services.AddSchemaArchitectCore();
        services.AddSingleton<IGenerationSessionStore, InMemoryGenerationSessionStore>();
        services.AddSingleton<IGeneratedFileArchiveService, GeneratedFileArchiveService>();
        services.AddSingleton<ISchemaHealthAnalyzer, SchemaHealthAnalyzer>();

        return services;
    }
}
