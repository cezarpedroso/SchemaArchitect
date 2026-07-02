using Microsoft.Extensions.DependencyInjection;
using SchemaArchitect.Core.Generation;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Parsing;

namespace SchemaArchitect.Core.Services;

/// <summary>
/// Provides dependency-injection registration for Schema Architect core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Schema Architect parsing and generation services.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The same service collection so that calls can be chained.</returns>
    public static IServiceCollection AddSchemaArchitectCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(_ => new SqlServerSchemaParser(new SqlServerTypeMapper()));
        services.AddSingleton(_ => new MySqlSchemaParser(new MySqlTypeMapper()));
        services.AddSingleton(_ => new SqliteSchemaParser(new SqliteTypeMapper()));
        services.AddSingleton(_ => new Db2SchemaParser(new Db2TypeMapper()));
        services.AddSingleton(_ => new OracleSchemaParser(new OracleTypeMapper()));
        services.AddSingleton(_ => new PostgreSqlSchemaParser(new PostgreSqlTypeMapper()));
        services.AddSingleton<ISchemaParser>(serviceProvider =>
            serviceProvider.GetRequiredService<SqlServerSchemaParser>());
        services.AddSingleton<ISchemaParserResolver, SchemaParserResolver>();
        services.AddSingleton<ICodeGenerator, CSharpCodeGenerator>();
        services.AddSingleton<ISqlTypeMapper, SqlServerTypeMapper>();

        return services;
    }
}
