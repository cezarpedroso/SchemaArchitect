using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Parsing;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Tests.ParsingTests;

/// <summary>
/// Verifies SQL Server CREATE TABLE parsing behavior.
/// </summary>
public sealed class SqlServerSchemaParserTests
{
    /// <summary>
    /// Verifies schema-qualified table names and common column facets are parsed.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenTableContainsCommonColumnFacets_ParsesColumns()
    {
        var parser = CreateParser();

        const string sql = """
            CREATE TABLE [sales].[Customers]
            (
                [CustomerId] [int] IDENTITY(1,1) NOT NULL,
                [Name] [nvarchar](200) NOT NULL,
                [Email] varchar(320) NULL,
                [CreditLimit] decimal(18, 2) NOT NULL,
                [CreatedUtc] datetime2(7) NOT NULL,
                [RowVersion] varbinary(max) NULL,
                CONSTRAINT [PK_Customers] PRIMARY KEY CLUSTERED
                (
                    [CustomerId] ASC
                )
            );
            """;

        var schema = await parser.ParseAsync(sql);
        var table = Assert.Single(schema.Tables);

        Assert.Equal("sales", table.Schema);
        Assert.Equal("Customers", table.Name);
        Assert.Equal(6, table.Columns.Count);

        var idColumn = GetColumn(table, "CustomerId");
        Assert.Equal("int", idColumn.StoreType);
        Assert.Equal("int", idColumn.CSharpType);
        Assert.True(idColumn.IsIdentity);
        Assert.True(idColumn.IsPrimaryKey);
        Assert.False(idColumn.IsNullable);

        var nameColumn = GetColumn(table, "Name");
        Assert.Equal("nvarchar", nameColumn.StoreType);
        Assert.Equal("string", nameColumn.CSharpType);
        Assert.Equal(200, nameColumn.MaxLength);
        Assert.False(nameColumn.IsNullable);

        var emailColumn = GetColumn(table, "Email");
        Assert.Equal("varchar", emailColumn.StoreType);
        Assert.Equal("string", emailColumn.CSharpType);
        Assert.Equal(320, emailColumn.MaxLength);
        Assert.True(emailColumn.IsNullable);

        var creditLimitColumn = GetColumn(table, "CreditLimit");
        Assert.Equal("decimal", creditLimitColumn.StoreType);
        Assert.Equal("decimal", creditLimitColumn.CSharpType);
        Assert.Equal((byte)18, creditLimitColumn.Precision);
        Assert.Equal((byte)2, creditLimitColumn.Scale);

        var createdUtcColumn = GetColumn(table, "CreatedUtc");
        Assert.Equal("datetime2", createdUtcColumn.StoreType);
        Assert.Equal("DateTime", createdUtcColumn.CSharpType);
        Assert.Equal((byte)7, createdUtcColumn.Precision);

        var rowVersionColumn = GetColumn(table, "RowVersion");
        Assert.Equal("varbinary", rowVersionColumn.StoreType);
        Assert.Equal("byte[]", rowVersionColumn.CSharpType);
        Assert.Equal(-1, rowVersionColumn.MaxLength);
        Assert.True(rowVersionColumn.IsNullable);

        var primaryKey = Assert.Single(table.PrimaryKey);
        Assert.Equal("CustomerId", primaryKey.Name);
    }

    /// <summary>
    /// Verifies unqualified table names default to the dbo schema.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenTableOmitsSchema_DefaultsToDbo()
    {
        var parser = CreateParser();

        const string sql = """
            CREATE TABLE Products
            (
                ProductId bigint NOT NULL PRIMARY KEY,
                Description ntext NULL,
                IsActive bit NOT NULL
            );
            """;

        var schema = await parser.ParseAsync(sql);
        var table = Assert.Single(schema.Tables);

        Assert.Equal("dbo", table.Schema);
        Assert.Equal("Products", table.Name);

        var productIdColumn = GetColumn(table, "ProductId");
        Assert.Equal("long", productIdColumn.CSharpType);
        Assert.True(productIdColumn.IsPrimaryKey);

        var descriptionColumn = GetColumn(table, "Description");
        Assert.Equal("string", descriptionColumn.CSharpType);
        Assert.True(descriptionColumn.IsNullable);

        var isActiveColumn = GetColumn(table, "IsActive");
        Assert.Equal("bool", isActiveColumn.CSharpType);
        Assert.False(isActiveColumn.IsNullable);
    }

    /// <summary>
    /// Verifies inline REFERENCES clauses are parsed as foreign keys.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenTableContainsInlineForeignKey_ParsesRelationship()
    {
        var parser = CreateParser();

        const string sql = """
            CREATE TABLE [dbo].[Orders]
            (
                [OrderId] int NOT NULL CONSTRAINT [PK_Orders] PRIMARY KEY,
                [CustomerId] int NOT NULL CONSTRAINT [FK_Orders_Customers] REFERENCES [sales].[Customers] ([CustomerId]),
                [OrderedAt] datetime NOT NULL
            );
            """;

        var schema = await parser.ParseAsync(sql);
        var table = Assert.Single(schema.Tables);
        var foreignKey = Assert.Single(table.ForeignKeys);
        var foreignKeyColumn = Assert.Single(table.ForeignKeyColumns);

        Assert.Equal("FK_Orders_Customers", foreignKey.Name);
        Assert.Equal("CustomerId", foreignKey.Column);
        Assert.Equal("sales", foreignKey.ReferencedSchema);
        Assert.Equal("Customers", foreignKey.ReferencedTable);
        Assert.Equal("CustomerId", foreignKey.ReferencedColumn);
        Assert.Equal("CustomerId", foreignKeyColumn.Name);
    }

    /// <summary>
    /// Verifies table-level FOREIGN KEY constraints are parsed.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenTableContainsTableLevelForeignKeys_ParsesRelationships()
    {
        var parser = CreateParser();

        const string sql = """
            CREATE TABLE [sales].[OrderLines]
            (
                [OrderLineId] int IDENTITY(1,1) NOT NULL,
                [OrderId] int NOT NULL,
                [ProductId] bigint NULL,
                [Quantity] smallint NOT NULL,
                CONSTRAINT [PK_OrderLines] PRIMARY KEY ([OrderLineId]),
                CONSTRAINT [FK_OrderLines_Orders] FOREIGN KEY ([OrderId]) REFERENCES [dbo].[Orders] ([OrderId]),
                CONSTRAINT [FK_OrderLines_Products] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([ProductId])
            );
            """;

        var schema = await parser.ParseAsync(sql);
        var table = Assert.Single(schema.Tables);

        Assert.Equal("sales", table.Schema);
        Assert.Equal("OrderLines", table.Name);
        Assert.Equal(2, table.ForeignKeys.Count);

        var productIdColumn = GetColumn(table, "ProductId");
        Assert.Equal("long?", productIdColumn.CSharpType);
        Assert.True(productIdColumn.IsNullable);

        var orderForeignKey = table.ForeignKeys.Single(foreignKey => foreignKey.Name == "FK_OrderLines_Orders");
        Assert.Equal("OrderId", orderForeignKey.Column);
        Assert.Equal("dbo", orderForeignKey.ReferencedSchema);
        Assert.Equal("Orders", orderForeignKey.ReferencedTable);
        Assert.Equal("OrderId", orderForeignKey.ReferencedColumn);

        var productForeignKey = table.ForeignKeys.Single(foreignKey => foreignKey.Name == "FK_OrderLines_Products");
        Assert.Equal("ProductId", productForeignKey.Column);
        Assert.Equal("Products", productForeignKey.ReferencedTable);

        var primaryKey = Assert.Single(table.PrimaryKey);
        Assert.Equal("OrderLineId", primaryKey.Name);
    }

    /// <summary>
    /// Verifies composite table-level primary keys are parsed.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenTableContainsCompositePrimaryKey_ParsesPrimaryKeyColumns()
    {
        var parser = CreateParser();

        const string sql = """
            CREATE TABLE [inventory].[Stock]
            (
                [WarehouseId] int NOT NULL,
                [Sku] varchar(32) NOT NULL,
                [Quantity] int NULL,
                CONSTRAINT [PK_Stock] PRIMARY KEY ([WarehouseId], [Sku])
            );
            """;

        var schema = await parser.ParseAsync(sql);
        var table = Assert.Single(schema.Tables);

        Assert.Equal(["WarehouseId", "Sku"], table.PrimaryKey.Select(static column => column.Name));

        var quantityColumn = GetColumn(table, "Quantity");
        Assert.Equal("int?", quantityColumn.CSharpType);
        Assert.True(quantityColumn.IsNullable);
    }

    /// <summary>
    /// Verifies sample SQL files remain parseable.
    /// </summary>
    /// <param name="fileName">The sample SQL file name.</param>
    /// <param name="expectedTableCount">The expected number of parsed tables.</param>
    [Theory]
    [InlineData("simple-two-table-schema.sql", 2)]
    [InlineData("foreign-key-schema.sql", 2)]
    [InlineData("nullable-columns-schema.sql", 1)]
    [InlineData("precision-and-length-schema.sql", 1)]
    [InlineData("complex-commerce-schema.sql", 7)]
    public async Task ParseAsync_WhenReadingSampleSqlFiles_ParsesExpectedTables(
        string fileName,
        int expectedTableCount)
    {
        var parser = CreateParser();
        var sql = await File.ReadAllTextAsync(GetSamplePath(fileName));

        var schema = await parser.ParseAsync(sql);

        Assert.Equal(expectedTableCount, schema.Tables.Count);
        Assert.All(schema.Tables, table => Assert.NotEmpty(table.Columns));
    }

    private static SqlServerSchemaParser CreateParser()
    {
        return new SqlServerSchemaParser(new SqlServerTypeMapper());
    }

    private static ColumnSchema GetColumn(TableSchema table, string columnName)
    {
        return table.Columns.Single(column => string.Equals(
            column.Name,
            columnName,
            StringComparison.OrdinalIgnoreCase));
    }

    private static string GetSamplePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var samplePath = Path.Combine(directory.FullName, "samples", fileName);
            if (File.Exists(samplePath))
            {
                return samplePath;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate sample SQL file '{fileName}'.");
    }
}
