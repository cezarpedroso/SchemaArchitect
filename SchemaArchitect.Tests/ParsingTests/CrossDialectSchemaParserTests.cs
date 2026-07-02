using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Parsing;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Tests.ParsingTests;

/// <summary>
/// Verifies CREATE TABLE parsing across supported SQL dialects.
/// </summary>
public sealed class CrossDialectSchemaParserTests
{
    /// <summary>
    /// Gets the supported dialect parsers.
    /// </summary>
    public static IEnumerable<object[]> Parsers()
    {
        yield return [new SqlServerSchemaParser(new SqlServerTypeMapper())];
        yield return [new MySqlSchemaParser(new MySqlTypeMapper())];
        yield return [new PostgreSqlSchemaParser(new PostgreSqlTypeMapper())];
        yield return [new SqliteSchemaParser(new SqliteTypeMapper())];
        yield return [new OracleSchemaParser(new OracleTypeMapper())];
        yield return [new Db2SchemaParser(new Db2TypeMapper())];
    }

    /// <summary>
    /// Verifies common cross-dialect type aliases do not block parsing or generation metadata.
    /// </summary>
    /// <param name="parser">The parser under test.</param>
    [Theory]
    [MemberData(nameof(Parsers))]
    public async Task ParseAsync_WhenSchemaUsesCommonCrossDialectAliases_ParsesSchema(ISchemaParser parser)
    {
        const string sql = """
            CREATE TABLE Users (
                Id int NOT NULL,
                Name nvarchar(120) NOT NULL,
                ExternalId uniqueidentifier NULL,
                Document varbinary(256) NULL,
                CONSTRAINT PK_Users PRIMARY KEY (Id)
            );
            """;

        var schema = await parser.ParseAsync(sql);

        var table = Assert.Single(schema.Tables);
        AssertColumn(table, "Name", "nvarchar", "string", maxLength: 120, isNullable: false);
        AssertColumn(table, "ExternalId", "uniqueidentifier", "Guid?", isNullable: true);
        AssertColumn(table, "Document", "varbinary", "byte[]", maxLength: 256, isNullable: true);
    }

    /// <summary>
    /// Verifies MySQL table, column, key, and relationship parsing.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenMySqlSchemaProvided_ParsesSchema()
    {
        var parser = new MySqlSchemaParser(new MySqlTypeMapper());

        const string sql = """
            CREATE TABLE `shop`.`customers` (
                `customer_id` bigint NOT NULL AUTO_INCREMENT,
                `email` varchar(255) NOT NULL,
                `credit_limit` decimal(12,2) NULL,
                `created_at` datetime NOT NULL,
                PRIMARY KEY (`customer_id`)
            );

            CREATE TABLE `shop`.`orders` (
                `order_id` int NOT NULL AUTO_INCREMENT,
                `customer_id` bigint NOT NULL,
                `total` decimal(12,2) NOT NULL,
                CONSTRAINT `fk_orders_customers`
                    FOREIGN KEY (`customer_id`) REFERENCES `shop`.`customers` (`customer_id`)
            );
            """;

        var schema = await parser.ParseAsync(sql);

        Assert.Equal(SqlDialect.MySql, schema.Dialect);
        Assert.Equal(2, schema.Tables.Count);

        var customers = GetTable(schema, "customers");
        Assert.Equal("shop", customers.Schema);
        AssertColumn(customers, "customer_id", "bigint", "long", isPrimaryKey: true, isIdentity: true, isNullable: false);
        AssertColumn(customers, "email", "varchar", "string", maxLength: 255, isNullable: false);
        AssertColumn(customers, "credit_limit", "decimal", "decimal?", precision: 12, scale: 2, isNullable: true);

        var orders = GetTable(schema, "orders");
        AssertColumn(orders, "order_id", "int", "int", isIdentity: true, isNullable: false);
        var foreignKey = Assert.Single(orders.ForeignKeys);
        Assert.Equal("fk_orders_customers", foreignKey.Name);
        Assert.Equal("customers", foreignKey.PrincipalTable);
        Assert.Equal("customer_id", Assert.Single(foreignKey.Columns));
    }

    /// <summary>
    /// Verifies standalone and table-level indexes are attached to parsed tables.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenSchemaContainsIndexes_AttachesIndexesToTables()
    {
        var parser = new MySqlSchemaParser(new MySqlTypeMapper());

        const string sql = """
            CREATE TABLE `shop`.`customers` (
                `customer_id` bigint NOT NULL AUTO_INCREMENT,
                `email` varchar(255) NOT NULL,
                `status` varchar(32) NOT NULL,
                PRIMARY KEY (`customer_id`),
                UNIQUE KEY `UX_Customers_Email` (`email`)
            );

            CREATE INDEX `IX_Customers_Status` ON `shop`.`customers` (`status`);
            """;

        var schema = await parser.ParseAsync(sql);

        var customers = GetTable(schema, "customers");
        Assert.Equal(2, customers.Indexes.Count);

        var uniqueIndex = Assert.Single(customers.Indexes, index => index.Name == "UX_Customers_Email");
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal("email", Assert.Single(uniqueIndex.Columns));

        var statusIndex = Assert.Single(customers.Indexes, index => index.Name == "IX_Customers_Status");
        Assert.False(statusIndex.IsUnique);
        Assert.Equal("status", Assert.Single(statusIndex.Columns));
    }

    /// <summary>
    /// Verifies PostgreSQL table, column, key, and relationship parsing.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenPostgreSqlSchemaProvided_ParsesSchema()
    {
        var parser = new PostgreSqlSchemaParser(new PostgreSqlTypeMapper());

        const string sql = """
            CREATE TABLE IF NOT EXISTS public.customers (
                id serial PRIMARY KEY,
                email character varying(255) NOT NULL,
                credit_limit numeric(12,2),
                created_at timestamp with time zone NOT NULL
            );

            CREATE TABLE public.orders (
                id bigserial PRIMARY KEY,
                customer_id integer NOT NULL REFERENCES public.customers(id),
                total numeric(12,2) NOT NULL
            );
            """;

        var schema = await parser.ParseAsync(sql);

        Assert.Equal(SqlDialect.PostgreSql, schema.Dialect);
        Assert.Equal(2, schema.Tables.Count);

        var customers = GetTable(schema, "customers");
        Assert.Equal("public", customers.Schema);
        AssertColumn(customers, "id", "serial", "int", isPrimaryKey: true, isIdentity: true, isNullable: false);
        AssertColumn(customers, "email", "character varying", "string", maxLength: 255, isNullable: false);
        AssertColumn(customers, "credit_limit", "numeric", "decimal?", precision: 12, scale: 2, isNullable: true);
        AssertColumn(customers, "created_at", "timestamp with time zone", "DateTime", isNullable: false);

        var orders = GetTable(schema, "orders");
        AssertColumn(orders, "id", "bigserial", "long", isPrimaryKey: true, isIdentity: true, isNullable: false);
        var foreignKey = Assert.Single(orders.ForeignKeys);
        Assert.Equal("FK_orders_customer_id", foreignKey.Name);
        Assert.Equal("customers", foreignKey.PrincipalTable);
        Assert.Equal("id", Assert.Single(foreignKey.PrincipalColumns));
    }

    /// <summary>
    /// Verifies SQLite table, column, key, and relationship parsing.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenSqliteSchemaProvided_ParsesSchema()
    {
        var parser = new SqliteSchemaParser(new SqliteTypeMapper());

        const string sql = """
            CREATE TABLE customers (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL,
                credit_limit NUMERIC,
                created_at TEXT
            );

            CREATE TABLE orders (
                id INTEGER PRIMARY KEY,
                customer_id INTEGER NOT NULL,
                total REAL NOT NULL,
                FOREIGN KEY (customer_id) REFERENCES customers(id)
            );
            """;

        var schema = await parser.ParseAsync(sql);

        Assert.Equal(SqlDialect.SQLite, schema.Dialect);
        Assert.Equal(2, schema.Tables.Count);

        var customers = GetTable(schema, "customers");
        Assert.Equal("main", customers.Schema);
        AssertColumn(customers, "id", "integer", "long", isPrimaryKey: true, isIdentity: true, isNullable: false);
        AssertColumn(customers, "email", "text", "string", isNullable: false);
        AssertColumn(customers, "credit_limit", "numeric", "decimal?", isNullable: true);

        var orders = GetTable(schema, "orders");
        AssertColumn(orders, "total", "real", "double", isNullable: false);
        var foreignKey = Assert.Single(orders.ForeignKeys);
        Assert.Equal("customers", foreignKey.PrincipalTable);
    }

    /// <summary>
    /// Verifies Oracle table, column, key, and relationship parsing.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenOracleSchemaProvided_ParsesSchema()
    {
        var parser = new OracleSchemaParser(new OracleTypeMapper());

        const string sql = """
            CREATE TABLE HR.CUSTOMERS (
                CUSTOMER_ID NUMBER(10,0) GENERATED BY DEFAULT AS IDENTITY NOT NULL,
                EMAIL VARCHAR2(255 BYTE) NOT NULL,
                CREDIT_LIMIT NUMBER(12,2),
                CREATED_AT TIMESTAMP(6) NOT NULL,
                CONSTRAINT PK_CUSTOMERS PRIMARY KEY (CUSTOMER_ID)
            );

            CREATE TABLE HR.ORDERS (
                ORDER_ID NUMBER(10,0) GENERATED ALWAYS AS IDENTITY NOT NULL,
                CUSTOMER_ID NUMBER(10,0) NOT NULL,
                TOTAL NUMBER(12,2) NOT NULL,
                CONSTRAINT PK_ORDERS PRIMARY KEY (ORDER_ID),
                CONSTRAINT FK_ORDERS_CUSTOMERS
                    FOREIGN KEY (CUSTOMER_ID) REFERENCES HR.CUSTOMERS (CUSTOMER_ID)
            );
            """;

        var schema = await parser.ParseAsync(sql);

        Assert.Equal(SqlDialect.Oracle, schema.Dialect);
        Assert.Equal(2, schema.Tables.Count);

        var customers = GetTable(schema, "CUSTOMERS");
        Assert.Equal("HR", customers.Schema);
        AssertColumn(customers, "CUSTOMER_ID", "number", "long", precision: 10, scale: 0, isPrimaryKey: true, isIdentity: true, isNullable: false);
        AssertColumn(customers, "EMAIL", "varchar2", "string", maxLength: 255, isNullable: false);
        AssertColumn(customers, "CREDIT_LIMIT", "number", "decimal?", precision: 12, scale: 2, isNullable: true);

        var orders = GetTable(schema, "ORDERS");
        var foreignKey = Assert.Single(orders.ForeignKeys);
        Assert.Equal("FK_ORDERS_CUSTOMERS", foreignKey.Name);
        Assert.Equal("CUSTOMERS", foreignKey.PrincipalTable);
    }

    /// <summary>
    /// Verifies IBM Db2 table, column, key, and relationship parsing.
    /// </summary>
    [Fact]
    public async Task ParseAsync_WhenDb2SchemaProvided_ParsesSchema()
    {
        var parser = new Db2SchemaParser(new Db2TypeMapper());

        const string sql = """
            CREATE TABLE APP.CUSTOMERS (
                CUSTOMER_ID INTEGER NOT NULL GENERATED ALWAYS AS IDENTITY,
                EMAIL VARCHAR(255) NOT NULL,
                CREDIT_LIMIT DECIMAL(12,2),
                CREATED_AT TIMESTAMP NOT NULL,
                CONSTRAINT PK_CUSTOMERS PRIMARY KEY (CUSTOMER_ID)
            );

            CREATE TABLE APP.ORDERS (
                ORDER_ID INTEGER NOT NULL GENERATED BY DEFAULT AS IDENTITY,
                CUSTOMER_ID INTEGER NOT NULL,
                TOTAL DECIMAL(12,2) NOT NULL,
                CONSTRAINT PK_ORDERS PRIMARY KEY (ORDER_ID),
                CONSTRAINT FK_ORDERS_CUSTOMERS
                    FOREIGN KEY (CUSTOMER_ID) REFERENCES APP.CUSTOMERS (CUSTOMER_ID)
            );
            """;

        var schema = await parser.ParseAsync(sql);

        Assert.Equal(SqlDialect.IbmDb2, schema.Dialect);
        Assert.Equal(2, schema.Tables.Count);

        var customers = GetTable(schema, "CUSTOMERS");
        Assert.Equal("APP", customers.Schema);
        AssertColumn(customers, "CUSTOMER_ID", "integer", "int", isPrimaryKey: true, isIdentity: true, isNullable: false);
        AssertColumn(customers, "EMAIL", "varchar", "string", maxLength: 255, isNullable: false);
        AssertColumn(customers, "CREDIT_LIMIT", "decimal", "decimal?", precision: 12, scale: 2, isNullable: true);

        var orders = GetTable(schema, "ORDERS");
        var foreignKey = Assert.Single(orders.ForeignKeys);
        Assert.Equal("FK_ORDERS_CUSTOMERS", foreignKey.Name);
        Assert.Equal("CUSTOMERS", foreignKey.PrincipalTable);
    }

    private static TableSchema GetTable(DatabaseSchema schema, string name)
    {
        return Assert.Single(
            schema.Tables,
            table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertColumn(
        TableSchema table,
        string name,
        string storeType,
        string csharpType,
        int? maxLength = null,
        byte? precision = null,
        byte? scale = null,
        bool isNullable = false,
        bool isPrimaryKey = false,
        bool isIdentity = false)
    {
        var column = Assert.Single(
            table.Columns,
            candidate => string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(storeType, column.StoreType);
        Assert.Equal(csharpType, column.CSharpType);
        Assert.Equal(maxLength, column.MaxLength);
        Assert.Equal(precision, column.Precision);
        Assert.Equal(scale, column.Scale);
        Assert.Equal(isNullable, column.IsNullable);
        Assert.Equal(isPrimaryKey, column.IsPrimaryKey);
        Assert.Equal(isIdentity, column.IsIdentity);
    }
}
