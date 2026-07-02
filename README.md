# Schema Architect

Schema Architect is an ASP.NET Core Razor Pages application that generates an Entity Framework Core project foundation from SQL schema files.

Instead of manually recreating entity models, `DbContext`, Fluent API configurations, DTOs, and controller scaffolding, Schema Architect analyzes your database schema and generates a clean, reviewable starting point for your application.

The generated code is intended to accelerate development—not replace developer review or customization.

---

## Why Schema Architect?

Building an EF Core application from an existing database often involves repeating the same setup work:

- Creating entity classes
- Mapping SQL data types to C#
- Configuring primary and foreign keys
- Writing Fluent API configurations
- Creating DTOs
- Scaffolding CRUD controllers

Schema Architect automates these repetitive tasks so you can spend less time on boilerplate and more time building your application.

---

## Features

- Upload and validate `.sql` schema files
- Support for SQL Server, PostgreSQL, MySQL, SQLite, Oracle, and IBM Db2
- Parse `CREATE TABLE` statements
- Detect:
  - Tables and schemas
  - Columns and SQL data types
  - Nullable and required fields
  - Primary keys
  - Identity columns
  - Foreign key relationships
  - String and binary length constraints
  - Decimal precision and scale
- Automatically map SQL types to C# types
- Preview the parsed schema before generation
- Generate:
  - EF Core entity classes
  - `DbContext`
  - Fluent API (`IEntityTypeConfiguration<T>`) classes
  - Create, Read, and Update DTOs
  - Basic CRUD controller scaffolding
  - EF Core migration instructions
- Preview generated source code in the browser
- Download the generated project as a ZIP archive

---

## Tech Stack

- .NET 9 / .NET 10
- ASP.NET Core Razor Pages
- Entity Framework Core
- Bootstrap
- xUnit
- GitHub Actions

---

## Screenshots

> Screenshots will be added before the first public release.

- Home page
- Upload page
- Schema preview
- Generated code preview

---

## Project Structure

```text
SchemaArchitect
├── SchemaArchitect.Core
│   ├── Generation
│   ├── Interfaces
│   ├── Models
│   ├── Parsing
│   └── Services
├── SchemaArchitect.Web
│   ├── Pages
│   ├── Services
│   └── ViewModels
├── SchemaArchitect.Tests
└── samples
```

---

## Getting Started

### Prerequisites

- .NET 9 SDK or .NET 10 SDK

### Run locally

```powershell
dotnet restore SchemaArchitect.sln
dotnet build SchemaArchitect.sln
dotnet test SchemaArchitect.sln
dotnet run --project SchemaArchitect.Web/SchemaArchitect.Web.csproj
```

Once the application is running, open the local URL displayed in the terminal and upload one of the sample SQL files from the `samples` directory.

---

## Example Input

```sql
CREATE TABLE [dbo].[Customers]
(
    [CustomerId] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] varchar(320) NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
);
```

## Example Output

```csharp
namespace SchemaArchitect.Generated.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }

    public required string Name { get; set; }

    public string? Email { get; set; }
}
```

---

## Sample Schemas

The repository includes several sample schemas for testing and experimentation:

- `samples/simple-two-table-schema.sql`
- `samples/foreign-key-schema.sql`
- `samples/nullable-columns-schema.sql`
- `samples/precision-and-length-schema.sql`
- `samples/complex-commerce-schema.sql`

---

## Roadmap

- Improve support for advanced SQL syntax across supported database engines
- Handle additional DDL edge cases
- Improve naming and pluralization logic
- Generate optional solution and project templates
- Verify generated code compiles successfully
- Add persistent job storage
- Improve syntax highlighting in generated code previews
- Provide downloadable sample output projects

---

## Limitations

- Focuses on parsing `CREATE TABLE` definitions.
- It is not intended to be a complete SQL parser.
- Generated projects require Entity Framework Core packages to be installed.
- Uploaded schemas and generated files are processed temporarily and are not persisted.

---

## Contributing

Contributions, bug reports, and feature requests are welcome.

If you'd like to improve SQL dialect support, code generation, or the user experience, feel free to open an issue or submit a pull request.

---

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
