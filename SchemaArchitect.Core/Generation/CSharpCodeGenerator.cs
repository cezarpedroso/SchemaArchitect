using System.Globalization;
using System.Text;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Core.Generation;

/// <summary>
/// Generates EF Core-ready C# source files from parsed database schema models.
/// </summary>
public sealed class CSharpCodeGenerator : ICodeGenerator
{
    /// <inheritdoc />
    public Task<IReadOnlyList<GeneratedFile>> GenerateAsync(
        DatabaseSchema schema,
        GenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(options);

        cancellationToken.ThrowIfCancellationRequested();

        var effectiveOptions = NormalizeOptions(options);
        var template = GenerationTemplateCatalog.GetTemplate(effectiveOptions.Template);
        var model = BuildGenerationModel(schema);
        var files = new List<GeneratedFile>();

        if (effectiveOptions.GenerateEntities)
        {
            files.AddRange(model.Tables.Select(table => new GeneratedFile
            {
                RelativePath = CombinePath(template.EntityPath, $"{table.EntityName}.cs"),
                Content = GenerateEntity(table, model, effectiveOptions, template),
            }));
        }

        if (effectiveOptions.GenerateDbContext)
        {
            files.Add(new GeneratedFile
            {
                RelativePath = CombinePath(template.DbContextPath, $"{effectiveOptions.DbContextName}.cs"),
                Content = GenerateDbContext(model, effectiveOptions, template),
            });
        }

        if (effectiveOptions.GenerateConfigurations)
        {
            files.AddRange(model.Tables.Select(table => new GeneratedFile
            {
                RelativePath = CombinePath(template.ConfigurationPath, $"{table.EntityName}Configuration.cs"),
                Content = GenerateConfiguration(table, model, effectiveOptions, template),
            }));
        }

        if (effectiveOptions.GenerateDtos)
        {
            foreach (var table in model.Tables)
            {
                files.Add(new GeneratedFile
                {
                    RelativePath = CombinePath(template.DtoPath, $"{table.EntityName}Dto.cs"),
                    Content = GenerateDto(table, effectiveOptions, template, $"{table.EntityName}Dto", table.Table.Columns),
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = CombinePath(template.DtoPath, $"Create{table.EntityName}Dto.cs"),
                    Content = GenerateDto(table, effectiveOptions, template, $"Create{table.EntityName}Dto", GetMutableDtoColumns(table)),
                });

                files.Add(new GeneratedFile
                {
                    RelativePath = CombinePath(template.DtoPath, $"Update{table.EntityName}Dto.cs"),
                    Content = GenerateDto(table, effectiveOptions, template, $"Update{table.EntityName}Dto", GetMutableDtoColumns(table)),
                });
            }
        }

        if (effectiveOptions.GenerateControllers)
        {
            files.AddRange(model.Tables.Select(table => new GeneratedFile
            {
                RelativePath = CombinePath(
                    template.ApiPath,
                    template.GeneratesMinimalEndpoints ? $"{table.EntityName}Endpoints.cs" : $"{table.ControllerName}.cs"),
                Content = template.GeneratesMinimalEndpoints
                    ? GenerateEndpoint(table, effectiveOptions, template)
                    : GenerateController(table, effectiveOptions, template),
            }));
        }

        if (effectiveOptions.GenerateMigrationInstructions)
        {
            files.Add(new GeneratedFile
            {
                RelativePath = "README_MIGRATIONS.md",
                Content = GenerateMigrationsReadme(effectiveOptions),
            });
        }

        return Task.FromResult<IReadOnlyList<GeneratedFile>>(files);
    }

    private static GenerationOptions NormalizeOptions(GenerationOptions options)
    {
        return options with
        {
            GenerateEntities = options.GenerateEntities ||
                options.GenerateDbContext ||
                options.GenerateConfigurations ||
                options.GenerateControllers,
            GenerateDbContext = options.GenerateDbContext ||
                options.GenerateControllers,
            GenerateDtos = options.GenerateDtos ||
                options.GenerateControllers,
        };
    }

    private static GenerationModel BuildGenerationModel(DatabaseSchema schema)
    {
        var tables = schema.Tables
            .Select(table => new TableGenerationModel(
                table,
                ToEntityName(table.Name),
                ToDbSetName(table.Name),
                $"{ToDbSetName(table.Name)}Controller"))
            .ToArray();

        return new GenerationModel(tables);
    }

    private static string GenerateEntity(
        TableGenerationModel table,
        GenerationModel model,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var incomingRelationships = model.GetIncomingForeignKeys(table);
        var hasCollections = incomingRelationships.Count > 0;
        var hasSystemTypes = RequiresSystemNamespace(table.Table.Columns);
        var builder = new StringBuilder();

        if (hasSystemTypes)
        {
            builder.AppendLine("using System;");
        }

        if (hasCollections)
        {
            builder.AppendLine("using System.Collections.Generic;");
        }

        if (hasSystemTypes || hasCollections)
        {
            builder.AppendLine();
        }

        builder.AppendLine($"namespace {GetNamespace(options, template.EntityNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public class {table.EntityName}");
        builder.AppendLine("{");

        foreach (var column in table.Table.Columns)
        {
            builder.AppendLine($"    public {GetPropertyDeclaration(column)}");
        }

        foreach (var foreignKey in table.Table.ForeignKeys)
        {
            var principalTable = model.ResolvePrincipalTable(foreignKey);
            var navigationName = GetReferenceNavigationName(foreignKey, principalTable);
            var navigationType = principalTable?.EntityName ?? ToEntityName(foreignKey.PrincipalTable);
            var isRequired = IsRequiredForeignKey(table.Table, foreignKey);
            var nullableAnnotation = isRequired ? string.Empty : "?";
            var initializer = isRequired ? " = null!;" : string.Empty;

            builder.AppendLine();
            builder.AppendLine($"    public {navigationType}{nullableAnnotation} {navigationName} {{ get; set; }}{initializer}");
        }

        foreach (var (dependentTable, foreignKey) in incomingRelationships)
        {
            var collectionName = GetCollectionNavigationName(dependentTable, foreignKey);

            builder.AppendLine();
            builder.AppendLine($"    public ICollection<{dependentTable.EntityName}> {collectionName} {{ get; set; }} = [];");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateDbContext(
        GenerationModel model,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine($"using {GetNamespace(options, template.EntityNamespace)};");
        if (options.GenerateConfigurations)
        {
            builder.AppendLine($"using {GetNamespace(options, template.ConfigurationNamespace)};");
        }

        builder.AppendLine();
        builder.AppendLine($"namespace {GetNamespace(options, template.DbContextNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public class {options.DbContextName} : DbContext");
        builder.AppendLine("{");
        builder.AppendLine($"    public {options.DbContextName}(DbContextOptions<{options.DbContextName}> options)");
        builder.AppendLine("        : base(options)");
        builder.AppendLine("    {");
        builder.AppendLine("    }");
        builder.AppendLine();

        foreach (var table in model.Tables)
        {
            builder.AppendLine($"    public DbSet<{table.EntityName}> {table.DbSetName} => Set<{table.EntityName}>();");
        }

        builder.AppendLine();
        builder.AppendLine("    protected override void OnModelCreating(ModelBuilder modelBuilder)");
        builder.AppendLine("    {");
        builder.AppendLine("        base.OnModelCreating(modelBuilder);");
        builder.AppendLine();

        if (options.GenerateConfigurations)
        {
            foreach (var table in model.Tables)
            {
                builder.AppendLine($"        modelBuilder.ApplyConfiguration(new {table.EntityName}Configuration());");
            }
        }

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateConfiguration(
        TableGenerationModel table,
        GenerationModel model,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine("using Microsoft.EntityFrameworkCore.Metadata.Builders;");
        builder.AppendLine($"using {GetNamespace(options, template.EntityNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"namespace {GetNamespace(options, template.ConfigurationNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public class {table.EntityName}Configuration : IEntityTypeConfiguration<{table.EntityName}>");
        builder.AppendLine("{");
        builder.AppendLine($"    public void Configure(EntityTypeBuilder<{table.EntityName}> builder)");
        builder.AppendLine("    {");
        builder.AppendLine($"        builder.ToTable({ToLiteral(table.Table.Name)}, {ToLiteral(table.Table.Schema)});");
        builder.AppendLine();

        AppendPrimaryKeyConfiguration(builder, table);
        AppendPropertyConfigurations(builder, table);
        AppendRelationshipConfigurations(builder, table, model);

        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static void AppendPrimaryKeyConfiguration(StringBuilder builder, TableGenerationModel table)
    {
        var primaryKey = table.Table.PrimaryKey;
        if (primaryKey.Count == 0)
        {
            return;
        }

        var keyExpression = primaryKey.Count == 1
            ? $"entity.{ToPropertyName(primaryKey[0].Name)}"
            : $"new {{ {string.Join(", ", primaryKey.Select(column => $"entity.{ToPropertyName(column.Name)}"))} }}";

        builder.AppendLine($"        builder.HasKey(entity => {keyExpression});");
        builder.AppendLine();
    }

    private static void AppendPropertyConfigurations(StringBuilder builder, TableGenerationModel table)
    {
        foreach (var column in table.Table.Columns)
        {
            var propertyName = ToPropertyName(column.Name);
            var chainedCalls = new List<string>
            {
                $"HasColumnName({ToLiteral(column.Name)})",
            };

            if (column.MaxLength is > 0)
            {
                chainedCalls.Add($"HasMaxLength({column.MaxLength.Value.ToString(CultureInfo.InvariantCulture)})");
            }

            if (column.Precision.HasValue && column.Scale.HasValue)
            {
                chainedCalls.Add($"HasPrecision({column.Precision.Value.ToString(CultureInfo.InvariantCulture)}, {column.Scale.Value.ToString(CultureInfo.InvariantCulture)})");
            }
            else if (column.Precision.HasValue)
            {
                chainedCalls.Add($"HasPrecision({column.Precision.Value.ToString(CultureInfo.InvariantCulture)})");
            }

            if (!column.IsNullable)
            {
                chainedCalls.Add("IsRequired()");
            }

            if (column.IsIdentity)
            {
                chainedCalls.Add("ValueGeneratedOnAdd()");
            }

            builder.Append($"        builder.Property(entity => entity.{propertyName})");

            foreach (var chainedCall in chainedCalls)
            {
                builder.AppendLine();
                builder.Append($"            .{chainedCall}");
            }

            builder.AppendLine(";");
            builder.AppendLine();
        }
    }

    private static void AppendRelationshipConfigurations(
        StringBuilder builder,
        TableGenerationModel table,
        GenerationModel model)
    {
        foreach (var foreignKey in table.Table.ForeignKeys)
        {
            var principalTable = model.ResolvePrincipalTable(foreignKey);
            var navigationName = GetReferenceNavigationName(foreignKey, principalTable);
            var collectionName = principalTable is null
                ? string.Empty
                : GetCollectionNavigationName(table, foreignKey);
            var foreignKeyExpression = foreignKey.Columns.Count == 1
                ? $"entity.{ToPropertyName(foreignKey.Columns[0])}"
                : $"new {{ {string.Join(", ", foreignKey.Columns.Select(column => $"entity.{ToPropertyName(column)}"))} }}";
            var withManyCall = principalTable is null
                ? "WithMany()"
                : $"WithMany(entity => entity.{collectionName})";
            var requiredCall = IsRequiredForeignKey(table.Table, foreignKey)
                ? "IsRequired()"
                : "IsRequired(false)";

            builder.AppendLine($"        builder.HasOne(entity => entity.{navigationName})");
            builder.AppendLine($"            .{withManyCall}");
            builder.AppendLine($"            .HasForeignKey(entity => {foreignKeyExpression})");
            builder.AppendLine($"            .{requiredCall};");
            builder.AppendLine();
        }
    }

    private static string GenerateDto(
        TableGenerationModel table,
        GenerationOptions options,
        GenerationTemplateDescriptor template,
        string dtoName,
        IEnumerable<ColumnSchema> columns)
    {
        var builder = new StringBuilder();

        if (RequiresSystemNamespace(columns))
        {
            builder.AppendLine("using System;");
            builder.AppendLine();
        }

        builder.AppendLine($"namespace {GetNamespace(options, template.DtoNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public class {dtoName}");
        builder.AppendLine("{");

        foreach (var column in columns)
        {
            builder.AppendLine($"    public {GetPropertyDeclaration(column)}");
        }

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateController(
        TableGenerationModel table,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var keyColumn = table.Table.PrimaryKey.FirstOrDefault() ?? table.Table.Columns.FirstOrDefault();
        if (keyColumn is null)
        {
            return GenerateEmptyController(table, options, template);
        }

        var keyPropertyName = ToPropertyName(keyColumn.Name);
        var keyParameterName = ToParameterName(keyPropertyName);
        var keyType = GetPropertyType(keyColumn);
        var mutableColumns = GetMutableDtoColumns(table).ToArray();
        var builder = new StringBuilder();

        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using Microsoft.AspNetCore.Mvc;");
        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine($"using {GetNamespace(options, template.DtoNamespace)};");
        builder.AppendLine($"using {GetNamespace(options, template.EntityNamespace)};");
        builder.AppendLine($"using {GetNamespace(options, template.DbContextNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"namespace {GetNamespace(options, template.ApiNamespace)};");
        builder.AppendLine();
        builder.AppendLine("[ApiController]");
        builder.AppendLine($"[Route(\"api/[controller]\")]");
        builder.AppendLine($"public class {table.ControllerName} : ControllerBase");
        builder.AppendLine("{");
        builder.AppendLine($"    private readonly {options.DbContextName} context;");
        builder.AppendLine();
        builder.AppendLine($"    public {table.ControllerName}({options.DbContextName} context)");
        builder.AppendLine("    {");
        builder.AppendLine("        this.context = context;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    [HttpGet]");
        builder.AppendLine($"    public async Task<ActionResult<IEnumerable<{table.EntityName}Dto>>> Get(CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine($"        var entities = await context.{table.DbSetName}");
        builder.AppendLine("            .AsNoTracking()");
        builder.AppendLine("            .ToListAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("        return Ok(entities.Select(MapToDto));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    [HttpGet(\"{id}\")]");
        builder.AppendLine($"    public async Task<ActionResult<{table.EntityName}Dto>> Get({keyType} {keyParameterName}, CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine($"        var entity = await context.{table.DbSetName}");
        builder.AppendLine("            .AsNoTracking()");
        builder.AppendLine($"            .FirstOrDefaultAsync(entity => entity.{keyPropertyName} == {keyParameterName}, cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("        return entity is null");
        builder.AppendLine("            ? NotFound()");
        builder.AppendLine("            : Ok(MapToDto(entity));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    [HttpPost]");
        builder.AppendLine($"    public async Task<ActionResult<{table.EntityName}Dto>> Post(Create{table.EntityName}Dto dto, CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine($"        var entity = new {table.EntityName}");
        builder.AppendLine("        {");

        foreach (var column in mutableColumns)
        {
            var propertyName = ToPropertyName(column.Name);
            builder.AppendLine($"            {propertyName} = dto.{propertyName},");
        }

        builder.AppendLine("        };");
        builder.AppendLine();
        builder.AppendLine($"        context.{table.DbSetName}.Add(entity);");
        builder.AppendLine("        await context.SaveChangesAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine($"        return CreatedAtAction(nameof(Get), new {{ id = entity.{keyPropertyName} }}, MapToDto(entity));");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    [HttpPut(\"{id}\")]");
        builder.AppendLine($"    public async Task<IActionResult> Put({keyType} {keyParameterName}, Update{table.EntityName}Dto dto, CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine($"        var entity = await context.{table.DbSetName}");
        builder.AppendLine($"            .FirstOrDefaultAsync(entity => entity.{keyPropertyName} == {keyParameterName}, cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("        if (entity is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return NotFound();");
        builder.AppendLine("        }");
        builder.AppendLine();

        foreach (var column in mutableColumns)
        {
            var propertyName = ToPropertyName(column.Name);
            builder.AppendLine($"        entity.{propertyName} = dto.{propertyName};");
        }

        builder.AppendLine();
        builder.AppendLine("        await context.SaveChangesAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("        return NoContent();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    [HttpDelete(\"{id}\")]");
        builder.AppendLine($"    public async Task<IActionResult> Delete({keyType} {keyParameterName}, CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine($"        var entity = await context.{table.DbSetName}");
        builder.AppendLine($"            .FirstOrDefaultAsync(entity => entity.{keyPropertyName} == {keyParameterName}, cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("        if (entity is null)");
        builder.AppendLine("        {");
        builder.AppendLine("            return NotFound();");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine($"        context.{table.DbSetName}.Remove(entity);");
        builder.AppendLine("        await context.SaveChangesAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("        return NoContent();");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    private static {table.EntityName}Dto MapToDto({table.EntityName} entity)");
        builder.AppendLine("    {");
        builder.AppendLine($"        return new {table.EntityName}Dto");
        builder.AppendLine("        {");

        foreach (var column in table.Table.Columns)
        {
            var propertyName = ToPropertyName(column.Name);
            builder.AppendLine($"            {propertyName} = entity.{propertyName},");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateEndpoint(
        TableGenerationModel table,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var keyColumn = table.Table.PrimaryKey.FirstOrDefault() ?? table.Table.Columns.FirstOrDefault();
        if (keyColumn is null)
        {
            return GenerateEmptyEndpoint(table, options, template);
        }

        var keyPropertyName = ToPropertyName(keyColumn.Name);
        var keyParameterName = ToParameterName(keyPropertyName);
        var keyType = GetPropertyType(keyColumn);
        var mutableColumns = GetMutableDtoColumns(table).ToArray();
        var route = ToKebabCase(table.DbSetName);
        var builder = new StringBuilder();

        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using System.Threading;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using Microsoft.AspNetCore.Builder;");
        builder.AppendLine("using Microsoft.AspNetCore.Http;");
        builder.AppendLine("using Microsoft.AspNetCore.Routing;");
        builder.AppendLine("using Microsoft.EntityFrameworkCore;");
        builder.AppendLine($"using {GetNamespace(options, template.DtoNamespace)};");
        builder.AppendLine($"using {GetNamespace(options, template.EntityNamespace)};");
        builder.AppendLine($"using {GetNamespace(options, template.DbContextNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"namespace {GetNamespace(options, template.ApiNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public static class {table.EntityName}Endpoints");
        builder.AppendLine("{");
        builder.AppendLine($"    public static IEndpointRouteBuilder Map{table.EntityName}Endpoints(this IEndpointRouteBuilder endpoints)");
        builder.AppendLine("    {");
        builder.AppendLine($"        var group = endpoints.MapGroup(\"/api/{route}\").WithTags({ToLiteral(table.DbSetName)});");
        builder.AppendLine();
        builder.AppendLine($"        group.MapGet(\"/\", async ({options.DbContextName} context, CancellationToken cancellationToken) =>");
        builder.AppendLine("        {");
        builder.AppendLine($"            var entities = await context.{table.DbSetName}");
        builder.AppendLine("                .AsNoTracking()");
        builder.AppendLine("                .ToListAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("            return Results.Ok(entities.Select(MapToDto));");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine($"        group.MapGet(\"/{{id}}\", async ({keyType} {keyParameterName}, {options.DbContextName} context, CancellationToken cancellationToken) =>");
        builder.AppendLine("        {");
        builder.AppendLine($"            var entity = await context.{table.DbSetName}");
        builder.AppendLine("                .AsNoTracking()");
        builder.AppendLine($"                .FirstOrDefaultAsync(entity => entity.{keyPropertyName} == {keyParameterName}, cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("            return entity is null");
        builder.AppendLine("                ? Results.NotFound()");
        builder.AppendLine("                : Results.Ok(MapToDto(entity));");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine($"        group.MapPost(\"/\", async (Create{table.EntityName}Dto dto, {options.DbContextName} context, CancellationToken cancellationToken) =>");
        builder.AppendLine("        {");
        builder.AppendLine($"            var entity = new {table.EntityName}");
        builder.AppendLine("            {");

        foreach (var column in mutableColumns)
        {
            var propertyName = ToPropertyName(column.Name);
            builder.AppendLine($"                {propertyName} = dto.{propertyName},");
        }

        builder.AppendLine("            };");
        builder.AppendLine();
        builder.AppendLine($"            context.{table.DbSetName}.Add(entity);");
        builder.AppendLine("            await context.SaveChangesAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine($"            return Results.Created($\"/api/{route}/{{entity.{keyPropertyName}}}\", MapToDto(entity));");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine($"        group.MapPut(\"/{{id}}\", async ({keyType} {keyParameterName}, Update{table.EntityName}Dto dto, {options.DbContextName} context, CancellationToken cancellationToken) =>");
        builder.AppendLine("        {");
        builder.AppendLine($"            var entity = await context.{table.DbSetName}");
        builder.AppendLine($"                .FirstOrDefaultAsync(entity => entity.{keyPropertyName} == {keyParameterName}, cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("            if (entity is null)");
        builder.AppendLine("            {");
        builder.AppendLine("                return Results.NotFound();");
        builder.AppendLine("            }");
        builder.AppendLine();

        foreach (var column in mutableColumns)
        {
            var propertyName = ToPropertyName(column.Name);
            builder.AppendLine($"            entity.{propertyName} = dto.{propertyName};");
        }

        builder.AppendLine();
        builder.AppendLine("            await context.SaveChangesAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("            return Results.NoContent();");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine($"        group.MapDelete(\"/{{id}}\", async ({keyType} {keyParameterName}, {options.DbContextName} context, CancellationToken cancellationToken) =>");
        builder.AppendLine("        {");
        builder.AppendLine($"            var entity = await context.{table.DbSetName}");
        builder.AppendLine($"                .FirstOrDefaultAsync(entity => entity.{keyPropertyName} == {keyParameterName}, cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("            if (entity is null)");
        builder.AppendLine("            {");
        builder.AppendLine("                return Results.NotFound();");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine($"            context.{table.DbSetName}.Remove(entity);");
        builder.AppendLine("            await context.SaveChangesAsync(cancellationToken);");
        builder.AppendLine();
        builder.AppendLine("            return Results.NoContent();");
        builder.AppendLine("        });");
        builder.AppendLine();
        builder.AppendLine("        return endpoints;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine($"    private static {table.EntityName}Dto MapToDto({table.EntityName} entity)");
        builder.AppendLine("    {");
        builder.AppendLine($"        return new {table.EntityName}Dto");
        builder.AppendLine("        {");

        foreach (var column in table.Table.Columns)
        {
            var propertyName = ToPropertyName(column.Name);
            builder.AppendLine($"            {propertyName} = entity.{propertyName},");
        }

        builder.AppendLine("        };");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateEmptyEndpoint(
        TableGenerationModel table,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var builder = new StringBuilder();

        builder.AppendLine("using Microsoft.AspNetCore.Routing;");
        builder.AppendLine();
        builder.AppendLine($"namespace {GetNamespace(options, template.ApiNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public static class {table.EntityName}Endpoints");
        builder.AppendLine("{");
        builder.AppendLine($"    public static IEndpointRouteBuilder Map{table.EntityName}Endpoints(this IEndpointRouteBuilder endpoints)");
        builder.AppendLine("    {");
        builder.AppendLine("        return endpoints;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateEmptyController(
        TableGenerationModel table,
        GenerationOptions options,
        GenerationTemplateDescriptor template)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"namespace {GetNamespace(options, template.ApiNamespace)};");
        builder.AppendLine();
        builder.AppendLine($"public class {table.ControllerName}");
        builder.AppendLine("{");
        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GenerateMigrationsReadme(GenerationOptions options)
    {
        return $"""
            # EF Core migrations

            Generated files are EF Core-ready, but Schema Architect does not create database connections or install packages.

            After copying the generated files into your ASP.NET Core solution and configuring a provider/connection string, run:

            ```powershell
            dotnet ef migrations add InitialCreate --project <InfrastructureProject> --startup-project <ApiOrWebProject> --context {options.DbContextName}
            dotnet ef database update --project <InfrastructureProject> --startup-project <ApiOrWebProject> --context {options.DbContextName}
            ```

            If your application uses a single project, omit the `--project` and `--startup-project` arguments.
            """;
    }

    private static IEnumerable<ColumnSchema> GetMutableDtoColumns(TableGenerationModel table)
    {
        return table.Table.Columns.Where(static column => !(column.IsPrimaryKey && column.IsIdentity));
    }

    private static string GetPropertyDeclaration(ColumnSchema column)
    {
        var type = GetPropertyType(column);
        var propertyName = ToPropertyName(column.Name);
        var requiredKeyword = IsRequiredReference(column) ? "required " : string.Empty;

        return $"{requiredKeyword}{type} {propertyName} {{ get; set; }}";
    }

    private static string GetPropertyType(ColumnSchema column)
    {
        var csharpType = string.IsNullOrWhiteSpace(column.CSharpType)
            ? "object"
            : column.CSharpType;

        if (IsReferenceType(csharpType) && column.IsNullable && !csharpType.EndsWith("?", StringComparison.Ordinal))
        {
            return $"{csharpType}?";
        }

        if (IsKnownValueType(csharpType) && column.IsNullable && !csharpType.EndsWith("?", StringComparison.Ordinal))
        {
            return $"{csharpType}?";
        }

        return csharpType;
    }

    private static bool IsRequiredReference(ColumnSchema column)
    {
        return !column.IsNullable && IsReferenceType(GetPropertyType(column));
    }

    private static bool IsReferenceType(string csharpType)
    {
        var nonNullableType = csharpType.EndsWith("?", StringComparison.Ordinal)
            ? csharpType[..^1]
            : csharpType;

        return string.Equals(nonNullableType, "string", StringComparison.Ordinal) ||
            string.Equals(nonNullableType, "byte[]", StringComparison.Ordinal) ||
            !IsKnownValueType(nonNullableType);
    }

    private static bool IsKnownValueType(string csharpType)
    {
        return csharpType is "bool" or "byte" or "short" or "int" or "long" or "decimal" or
            "double" or "float" or "DateTime" or "DateTimeOffset" or "TimeSpan" or "Guid";
    }

    private static bool IsRequiredForeignKey(TableSchema table, ForeignKeySchema foreignKey)
    {
        return foreignKey.Columns
            .Select(columnName => table.Columns.FirstOrDefault(column =>
                string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            .Where(column => column is not null)
            .All(column => column is { IsNullable: false });
    }

    private static bool RequiresSystemNamespace(IEnumerable<ColumnSchema> columns)
    {
        return columns.Any(static column =>
        {
            var propertyType = GetPropertyType(column);
            var nonNullableType = propertyType.EndsWith("?", StringComparison.Ordinal)
                ? propertyType[..^1]
                : propertyType;

            return nonNullableType is "DateTime" or "DateTimeOffset" or "TimeSpan" or "Guid";
        });
    }

    private static string GetReferenceNavigationName(
        ForeignKeySchema foreignKey,
        TableGenerationModel? principalTable)
    {
        return principalTable?.EntityName ?? ToEntityName(foreignKey.PrincipalTable);
    }

    private static string GetCollectionNavigationName(
        TableGenerationModel dependentTable,
        ForeignKeySchema foreignKey)
    {
        _ = foreignKey;
        return dependentTable.DbSetName;
    }

    private static string ToEntityName(string name)
    {
        return MakeSingular(ToPascalCase(name));
    }

    private static string ToDbSetName(string name)
    {
        var pascalName = ToPascalCase(name);

        return pascalName.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            ? pascalName
            : $"{pascalName}s";
    }

    private static string ToPropertyName(string name)
    {
        return ToPascalCase(name);
    }

    private static string ToParameterName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "value";
        }

        return $"{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}";
    }

    private static string ToPascalCase(string name)
    {
        var builder = new StringBuilder();
        var capitalizeNext = true;

        foreach (var character in name)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext
                ? char.ToUpperInvariant(character)
                : character);
            capitalizeNext = false;
        }

        if (builder.Length == 0)
        {
            return "Value";
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, "Value");
        }

        return builder.ToString();
    }

    private static string ToKebabCase(string name)
    {
        var pascalName = ToPascalCase(name);
        var builder = new StringBuilder();

        for (var index = 0; index < pascalName.Length; index++)
        {
            var character = pascalName[index];

            if (index > 0 && char.IsUpper(character))
            {
                builder.Append('-');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.Length == 0 ? "values" : builder.ToString();
    }

    private static string MakeSingular(string name)
    {
        if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name.Length > 3)
        {
            return $"{name[..^3]}y";
        }

        if (name.EndsWith("ses", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("zes", StringComparison.OrdinalIgnoreCase))
        {
            return name[..^2];
        }

        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("ss", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("us", StringComparison.OrdinalIgnoreCase) &&
            name.Length > 1)
        {
            return name[..^1];
        }

        return name;
    }

    private static string ToLiteral(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static string GetNamespace(GenerationOptions options, string suffix)
    {
        return $"{options.RootNamespace}.{suffix}";
    }

    private static string CombinePath(string folder, string fileName)
    {
        return $"{folder.TrimEnd('/')}/{fileName}";
    }

    private sealed record GenerationModel(IReadOnlyList<TableGenerationModel> Tables)
    {
        public TableGenerationModel? ResolvePrincipalTable(ForeignKeySchema foreignKey)
        {
            return Tables.FirstOrDefault(table =>
                    string.Equals(table.Table.Schema, foreignKey.PrincipalSchema, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(table.Table.Name, foreignKey.PrincipalTable, StringComparison.OrdinalIgnoreCase)) ??
                Tables.FirstOrDefault(table =>
                    string.Equals(table.Table.Name, foreignKey.PrincipalTable, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<(TableGenerationModel DependentTable, ForeignKeySchema ForeignKey)> GetIncomingForeignKeys(
            TableGenerationModel principalTable)
        {
            return Tables
                .SelectMany(dependentTable => dependentTable.Table.ForeignKeys
                    .Where(foreignKey =>
                    {
                        var resolvedPrincipal = ResolvePrincipalTable(foreignKey);

                        return resolvedPrincipal is not null &&
                            ReferenceEquals(resolvedPrincipal, principalTable);
                    })
                    .Select(foreignKey => (dependentTable, foreignKey)))
                .ToArray();
        }
    }

    private sealed record TableGenerationModel(
        TableSchema Table,
        string EntityName,
        string DbSetName,
        string ControllerName);
}
