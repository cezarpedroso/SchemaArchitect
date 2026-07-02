using SchemaArchitect.Core.Generation;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Tests.GeneratorTests;

/// <summary>
/// Verifies C# source generation from parsed schema models.
/// </summary>
public sealed class CSharpCodeGeneratorTests
{
    /// <summary>
    /// Verifies generated files are organized into the expected clean output folders.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenInvoked_ReturnsExpectedFilePaths()
    {
        var files = await GenerateFilesAsync();

        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Customer.cs");
        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Order.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Data/AppDbContext.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Configurations/CustomerConfiguration.cs");
        Assert.Contains(files, file => file.RelativePath == "Application/DTOs/CustomerDto.cs");
        Assert.Contains(files, file => file.RelativePath == "Application/DTOs/CreateCustomerDto.cs");
        Assert.Contains(files, file => file.RelativePath == "Application/DTOs/UpdateCustomerDto.cs");
        Assert.Contains(files, file => file.RelativePath == "API/Controllers/CustomersController.cs");
        Assert.Contains(files, file => file.RelativePath == "README_MIGRATIONS.md");
    }

    /// <summary>
    /// Verifies Standard EF Core remains the existing output contract.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenStandardTemplateIsSelected_PreservesCurrentOutputStructure()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            Template = GenerationTemplate.StandardEfCore,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);

        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Customer.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Data/AppDbContext.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Configurations/CustomerConfiguration.cs");
        Assert.Contains(files, file => file.RelativePath == "Application/DTOs/CustomerDto.cs");
        Assert.Contains(files, file => file.RelativePath == "API/Controllers/CustomersController.cs");
    }

    /// <summary>
    /// Verifies Minimal API generates endpoint artifacts instead of controller artifacts.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenMinimalApiTemplateIsSelected_GeneratesEndpointFiles()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            Template = GenerationTemplate.MinimalApi,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);
        var endpoint = GetFile(files, "API/Endpoints/CustomerEndpoints.cs");

        Assert.Contains("namespace Acme.Generated.API.Endpoints;", endpoint.Content);
        Assert.Contains("public static class CustomerEndpoints", endpoint.Content);
        Assert.Contains("MapCustomerEndpoints", endpoint.Content);
        Assert.DoesNotContain(files, file => file.RelativePath == "API/Controllers/CustomersController.cs");
    }

    /// <summary>
    /// Verifies DDD uses aggregate-oriented domain paths and namespaces.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenDddTemplateIsSelected_UsesAggregateStructure()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            Template = GenerationTemplate.Ddd,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);
        var entity = GetFile(files, "Domain/Aggregates/Customer.cs");
        var dbContext = GetFile(files, "Infrastructure/Persistence/AppDbContext.cs");

        Assert.Contains("namespace Acme.Generated.Domain.Aggregates;", entity.Content);
        Assert.Contains("using Acme.Generated.Domain.Aggregates;", dbContext.Content);
        Assert.Contains(files, file => file.RelativePath == "Application/Contracts/CustomerDto.cs");
    }

    /// <summary>
    /// Verifies entity generation includes scalar properties and navigation properties.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenGeneratingEntities_IncludesPropertiesAndNavigations()
    {
        var files = await GenerateFilesAsync();

        var customerEntity = GetFile(files, "Domain/Entities/Customer.cs");
        Assert.Contains("namespace Acme.Generated.Domain.Entities;", customerEntity.Content);
        Assert.Contains("public class Customer", customerEntity.Content);
        Assert.Contains("public int CustomerId { get; set; }", customerEntity.Content);
        Assert.Contains("public required string Name { get; set; }", customerEntity.Content);
        Assert.Contains("public string? Email { get; set; }", customerEntity.Content);
        Assert.Contains("public decimal CreditLimit { get; set; }", customerEntity.Content);
        Assert.Contains("public ICollection<Order> Orders { get; set; } = [];", customerEntity.Content);

        var orderEntity = GetFile(files, "Domain/Entities/Order.cs");
        Assert.Contains("public Customer Customer { get; set; } = null!;", orderEntity.Content);
        Assert.Contains("public decimal? DiscountAmount { get; set; }", orderEntity.Content);
    }

    /// <summary>
    /// Verifies DbContext generation includes DbSets and configuration registration.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenGeneratingDbContext_IncludesDbSetsAndConfigurations()
    {
        var files = await GenerateFilesAsync();

        var dbContext = GetFile(files, "Infrastructure/Data/AppDbContext.cs");

        Assert.Contains("public class AppDbContext : DbContext", dbContext.Content);
        Assert.Contains("public DbSet<Customer> Customers => Set<Customer>();", dbContext.Content);
        Assert.Contains("public DbSet<Order> Orders => Set<Order>();", dbContext.Content);
        Assert.Contains("modelBuilder.ApplyConfiguration(new CustomerConfiguration());", dbContext.Content);
        Assert.Contains("modelBuilder.ApplyConfiguration(new OrderConfiguration());", dbContext.Content);
    }

    /// <summary>
    /// Verifies Fluent API configuration generation includes keys, facets, and relationships.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenGeneratingConfigurations_IncludesKeysFacetsAndRelationships()
    {
        var files = await GenerateFilesAsync();

        var customerConfiguration = GetFile(files, "Infrastructure/Configurations/CustomerConfiguration.cs");
        Assert.Contains("builder.ToTable(\"Customers\", \"sales\");", customerConfiguration.Content);
        Assert.Contains("builder.HasKey(entity => entity.CustomerId);", customerConfiguration.Content);
        Assert.Contains(".HasMaxLength(200)", customerConfiguration.Content);
        Assert.Contains(".HasPrecision(18, 2)", customerConfiguration.Content);
        Assert.Contains(".ValueGeneratedOnAdd()", customerConfiguration.Content);

        var orderConfiguration = GetFile(files, "Infrastructure/Configurations/OrderConfiguration.cs");
        Assert.Contains("builder.HasOne(entity => entity.Customer)", orderConfiguration.Content);
        Assert.Contains(".WithMany(entity => entity.Orders)", orderConfiguration.Content);
        Assert.Contains(".HasForeignKey(entity => entity.CustomerId)", orderConfiguration.Content);
        Assert.Contains(".IsRequired();", orderConfiguration.Content);
    }

    /// <summary>
    /// Verifies DTO generation creates read/create/update DTO shapes.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenGeneratingDtos_CreatesReadCreateAndUpdateDtos()
    {
        var files = await GenerateFilesAsync();

        var readDto = GetFile(files, "Application/DTOs/CustomerDto.cs");
        Assert.Contains("public class CustomerDto", readDto.Content);
        Assert.Contains("public int CustomerId { get; set; }", readDto.Content);

        var createDto = GetFile(files, "Application/DTOs/CreateCustomerDto.cs");
        Assert.Contains("public class CreateCustomerDto", createDto.Content);
        Assert.DoesNotContain("CustomerId", createDto.Content);
        Assert.Contains("public required string Name { get; set; }", createDto.Content);

        var updateDto = GetFile(files, "Application/DTOs/UpdateCustomerDto.cs");
        Assert.Contains("public class UpdateCustomerDto", updateDto.Content);
        Assert.DoesNotContain("CustomerId", updateDto.Content);
        Assert.Contains("public string? Email { get; set; }", updateDto.Content);
    }

    /// <summary>
    /// Verifies CRUD controller generation includes basic REST actions and DTO mapping.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenGeneratingControllers_CreatesCrudController()
    {
        var files = await GenerateFilesAsync();

        var controller = GetFile(files, "API/Controllers/CustomersController.cs");

        Assert.Contains("[ApiController]", controller.Content);
        Assert.Contains("public class CustomersController : ControllerBase", controller.Content);
        Assert.Contains("public async Task<ActionResult<IEnumerable<CustomerDto>>> Get", controller.Content);
        Assert.Contains("public async Task<ActionResult<CustomerDto>> Get(int customerId", controller.Content);
        Assert.Contains("public async Task<ActionResult<CustomerDto>> Post(CreateCustomerDto dto", controller.Content);
        Assert.Contains("public async Task<IActionResult> Put(int customerId, UpdateCustomerDto dto", controller.Content);
        Assert.Contains("public async Task<IActionResult> Delete(int customerId", controller.Content);
        Assert.Contains("return Ok(entities.Select(MapToDto));", controller.Content);
        Assert.Contains("private static CustomerDto MapToDto(Customer entity)", controller.Content);
    }

    /// <summary>
    /// Verifies migration README generation includes EF Core migration commands.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenInvoked_CreatesMigrationsReadme()
    {
        var files = await GenerateFilesAsync();

        var readme = GetFile(files, "README_MIGRATIONS.md");

        Assert.Contains("dotnet ef migrations add InitialCreate", readme.Content);
        Assert.Contains("--context AppDbContext", readme.Content);
        Assert.Contains("dotnet ef database update", readme.Content);
    }

    /// <summary>
    /// Verifies generation options can suppress selected output groups.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenOptionsDisableOutputs_ExcludesThoseFiles()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            GenerateControllers = false,
            GenerateMigrationInstructions = false,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);

        Assert.DoesNotContain(files, file => file.RelativePath.StartsWith("API/Controllers/", StringComparison.Ordinal));
        Assert.DoesNotContain(files, file => file.RelativePath == "README_MIGRATIONS.md");
        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Customer.cs");
    }

    /// <summary>
    /// Verifies controller generation includes the dependent files needed to compile.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenOnlyControllersAreSelected_IncludesControllerDependencies()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            GenerateEntities = false,
            GenerateDbContext = false,
            GenerateConfigurations = false,
            GenerateDtos = false,
            GenerateControllers = true,
            GenerateMigrationInstructions = false,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);

        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Customer.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Data/AppDbContext.cs");
        Assert.Contains(files, file => file.RelativePath == "Application/DTOs/CustomerDto.cs");
        Assert.Contains(files, file => file.RelativePath == "API/Controllers/CustomersController.cs");
        Assert.DoesNotContain(files, file => file.RelativePath == "Infrastructure/Configurations/CustomerConfiguration.cs");
    }

    /// <summary>
    /// Verifies DbContext generation does not reference configuration classes when they are disabled.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenConfigurationsAreDisabled_DbContextDoesNotReferenceConfigurations()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            GenerateConfigurations = false,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);
        var dbContext = GetFile(files, "Infrastructure/Data/AppDbContext.cs");

        Assert.DoesNotContain("Infrastructure.Configurations", dbContext.Content);
        Assert.DoesNotContain("ApplyConfiguration", dbContext.Content);
    }

    /// <summary>
    /// Verifies infrastructure outputs include entity dependencies when entities were not explicitly selected.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenInfrastructureOutputsAreSelected_IncludesEntityDependencies()
    {
        var generator = new CSharpCodeGenerator();
        var options = CreateOptions() with
        {
            GenerateEntities = false,
            GenerateDbContext = true,
            GenerateConfigurations = true,
            GenerateDtos = false,
            GenerateControllers = false,
            GenerateMigrationInstructions = false,
        };

        var files = await generator.GenerateAsync(CreateSchema(), options);

        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Customer.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Data/AppDbContext.cs");
        Assert.Contains(files, file => file.RelativePath == "Infrastructure/Configurations/CustomerConfiguration.cs");
    }

    /// <summary>
    /// Verifies nullable value-type columns remain nullable even when the C# type metadata is not pre-suffixed.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenNullableValueTypeLacksNullableSuffix_EmitsNullableProperty()
    {
        var generator = new CSharpCodeGenerator();
        var schema = new DatabaseSchema
        {
            Tables =
            [
                new TableSchema
                {
                    Name = "Metrics",
                    Columns =
                    [
                        new ColumnSchema
                        {
                            Name = "MetricId",
                            StoreType = "int",
                            CSharpType = "int",
                            IsPrimaryKey = true,
                        },
                        new ColumnSchema
                        {
                            Name = "RetryCount",
                            StoreType = "int",
                            CSharpType = "int",
                            IsNullable = true,
                        },
                    ],
                },
            ],
        };

        var files = await generator.GenerateAsync(schema, CreateOptions());
        var entity = GetFile(files, "Domain/Entities/Metric.cs");

        Assert.Contains("public int? RetryCount { get; set; }", entity.Content);
    }

    /// <summary>
    /// Verifies singular table names ending in "us" are not mangled.
    /// </summary>
    [Fact]
    public async Task GenerateAsync_WhenTableNameEndsInUs_DoesNotStripTrailingS()
    {
        var generator = new CSharpCodeGenerator();
        var schema = new DatabaseSchema
        {
            Tables =
            [
                new TableSchema
                {
                    Name = "Status",
                    Columns =
                    [
                        new ColumnSchema
                        {
                            Name = "StatusId",
                            StoreType = "int",
                            CSharpType = "int",
                            IsPrimaryKey = true,
                        },
                    ],
                },
            ],
        };

        var files = await generator.GenerateAsync(schema, CreateOptions());

        Assert.Contains(files, file => file.RelativePath == "Domain/Entities/Status.cs");
        Assert.DoesNotContain(files, file => file.RelativePath == "Domain/Entities/Statu.cs");
    }

    private static async Task<IReadOnlyList<GeneratedFile>> GenerateFilesAsync()
    {
        var generator = new CSharpCodeGenerator();

        return await generator.GenerateAsync(CreateSchema(), CreateOptions());
    }

    private static GeneratedFile GetFile(IReadOnlyList<GeneratedFile> files, string relativePath)
    {
        return files.Single(file => file.RelativePath == relativePath);
    }

    private static GenerationOptions CreateOptions()
    {
        return new GenerationOptions
        {
            RootNamespace = "Acme.Generated",
            DbContextName = "AppDbContext",
        };
    }

    private static DatabaseSchema CreateSchema()
    {
        var customerId = new ColumnSchema
        {
            Name = "CustomerId",
            StoreType = "int",
            CSharpType = "int",
            IsPrimaryKey = true,
            IsIdentity = true,
        };

        var orderId = new ColumnSchema
        {
            Name = "OrderId",
            StoreType = "int",
            CSharpType = "int",
            IsPrimaryKey = true,
            IsIdentity = true,
        };

        return new DatabaseSchema
        {
            Tables =
            [
                new TableSchema
                {
                    Schema = "sales",
                    Name = "Customers",
                    Columns =
                    [
                        customerId,
                        new ColumnSchema
                        {
                            Name = "Name",
                            StoreType = "nvarchar",
                            CSharpType = "string",
                            MaxLength = 200,
                        },
                        new ColumnSchema
                        {
                            Name = "Email",
                            StoreType = "varchar",
                            CSharpType = "string",
                            IsNullable = true,
                            MaxLength = 320,
                        },
                        new ColumnSchema
                        {
                            Name = "CreditLimit",
                            StoreType = "decimal",
                            CSharpType = "decimal",
                            Precision = 18,
                            Scale = 2,
                        },
                    ],
                },
                new TableSchema
                {
                    Schema = "sales",
                    Name = "Orders",
                    Columns =
                    [
                        orderId,
                        new ColumnSchema
                        {
                            Name = "CustomerId",
                            StoreType = "int",
                            CSharpType = "int",
                        },
                        new ColumnSchema
                        {
                            Name = "OrderedAt",
                            StoreType = "datetime2",
                            CSharpType = "DateTime",
                        },
                        new ColumnSchema
                        {
                            Name = "DiscountAmount",
                            StoreType = "decimal",
                            CSharpType = "decimal?",
                            IsNullable = true,
                            Precision = 18,
                            Scale = 2,
                        },
                    ],
                    ForeignKeys =
                    [
                        new ForeignKeySchema
                        {
                            Name = "FK_Orders_Customers_CustomerId",
                            Columns = ["CustomerId"],
                            PrincipalSchema = "sales",
                            PrincipalTable = "Customers",
                            PrincipalColumns = ["CustomerId"],
                        },
                    ],
                },
            ],
        };
    }
}
