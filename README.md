# Schema Architect

Schema Architect is an ASP.NET Core Razor Pages application that turns relational `.sql` schema files into EF Core-ready C# project foundations.

The app accepts a schema upload, parses `CREATE TABLE` statements, previews the discovered tables/columns/keys/relationships, generates source files, previews the generated output, and downloads everything as a ZIP.

## Problem solved

Starting a new EF Core API from an existing database schema is repetitive:

- translate SQL types to C# types;
- recreate entity classes;
- wire up primary keys, identity columns, max lengths, precision, and relationships;
- create DbContext and Fluent API configuration classes;
- build DTOs and controller scaffolding.

Schema Architect automates the boring first pass so developers can start with clean, reviewable code instead of a blank project.

## Features

- Upload and validate `.sql` schema files.
- Choose SQL Server, PostgreSQL, MySQL, SQLite, Oracle, or IBM Db2.
- Parse `CREATE TABLE` DDL.
- Detect:
  - schemas and table names;
  - columns and SQL types;
  - nullable vs required columns;
  - primary keys;
  - identity columns;
  - inline and table-level foreign keys;
  - string/binary max lengths;
  - decimal/numeric precision and scale.
- Map database-specific SQL types to C# types.
- Preview parsed schema before generation.
- Generate:
  - EF Core entity classes;
  - `DbContext`;
  - `IEntityTypeConfiguration<T>` Fluent API classes;
  - read/create/update DTOs;
  - basic CRUD controllers;
  - EF Core migration instructions.
- Preview generated files in the browser.
- Download generated output as a ZIP.
- No authentication, direct database connections, or SQL client dependencies.

## Tech stack

- .NET 9 / .NET 10
- ASP.NET Core Razor Pages
- Bootstrap
- xUnit
- GitHub Actions

## Screenshots

> Add screenshots before publishing the public repository.

- Home page: `docs/screenshots/home.png`
- Upload page: `docs/screenshots/upload.png`
- Schema preview: `docs/screenshots/schema-preview.png`
- Generated code preview: `docs/screenshots/code-preview.png`

## Solution structure

```text
Schema Architect
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

## How to run locally

Prerequisites:

- .NET 9 SDK or .NET 10 SDK

From the repository root:

```powershell
dotnet restore SchemaArchitect.sln
dotnet build SchemaArchitect.sln
dotnet test SchemaArchitect.sln
dotnet run --project SchemaArchitect.Web/SchemaArchitect.Web.csproj
```

Then open the local URL printed by ASP.NET Core and upload one of the sample files from the `samples` folder.

## Sample input

```sql
CREATE TABLE [dbo].[Customers]
(
    [CustomerId] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Email] varchar(320) NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
);
```

## Sample generated output

```csharp
namespace SchemaArchitect.Generated.Domain.Entities;

public class Customer
{
    public int CustomerId { get; set; }

    public required string Name { get; set; }

    public string? Email { get; set; }
}
```

## Sample SQL files

- `samples/simple-two-table-schema.sql`
- `samples/foreign-key-schema.sql`
- `samples/nullable-columns-schema.sql`
- `samples/precision-and-length-schema.sql`
- `samples/complex-commerce-schema.sql`

## Roadmap

- Expand support for advanced DDL patterns and edge cases across supported dialects.
- Generate cleaner pluralization/singularization using a dedicated naming service.
- Add optional generated project templates.
- Add generated output compile verification.
- Add persisted job storage for longer sessions.
- Add richer code highlighting.
- Add downloadable sample output packages.

## Scope notes

- The SQL parser is focused on table-definition DDL and is not intended to replace a full database-specific SQL compiler.
- Generated EF Core code assumes the target project has EF Core packages installed.
- Uploaded schema and generated output are stored temporarily in memory.

## License

Schema Architect is released under the MIT License. See [LICENSE](LICENSE).
