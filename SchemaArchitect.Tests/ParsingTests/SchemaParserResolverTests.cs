using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Core.Parsing;
using SchemaArchitect.Core.Services;

namespace SchemaArchitect.Tests.ParsingTests;

/// <summary>
/// Verifies SQL dialect parser resolution.
/// </summary>
public sealed class SchemaParserResolverTests
{
    /// <summary>
    /// Verifies SQL Server resolves to the working SQL Server parser.
    /// </summary>
    [Fact]
    public async Task Resolve_WhenSqlServerSelected_ReturnsSqlServerParser()
    {
        var resolver = CreateResolver();

        var parser = resolver.Resolve(SqlDialect.SqlServer);
        var schema = await parser.ParseAsync("""
            CREATE TABLE [dbo].[Users]
            (
                [Id] int NOT NULL,
                CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
            );
            """);

        Assert.IsType<SqlServerSchemaParser>(parser);
        Assert.Equal(SqlDialect.SqlServer, schema.Dialect);
        Assert.Single(schema.Tables);
    }

    /// <summary>
    /// Verifies every selectable SQL dialect resolves to a working parser.
    /// </summary>
    [Theory]
    [InlineData(SqlDialect.MySql, typeof(MySqlSchemaParser), "CREATE TABLE `Users` (`Id` int NOT NULL, PRIMARY KEY (`Id`));")]
    [InlineData(SqlDialect.SQLite, typeof(SqliteSchemaParser), "CREATE TABLE Users (Id INTEGER PRIMARY KEY AUTOINCREMENT);")]
    [InlineData(SqlDialect.IbmDb2, typeof(Db2SchemaParser), "CREATE TABLE Users (Id INTEGER NOT NULL, CONSTRAINT PK_Users PRIMARY KEY (Id));")]
    [InlineData(SqlDialect.Oracle, typeof(OracleSchemaParser), "CREATE TABLE Users (Id NUMBER(10) NOT NULL, CONSTRAINT PK_Users PRIMARY KEY (Id));")]
    [InlineData(SqlDialect.PostgreSql, typeof(PostgreSqlSchemaParser), "CREATE TABLE public.\"Users\" (\"Id\" serial PRIMARY KEY);")]
    public async Task Resolve_WhenDialectSelected_ReturnsWorkingParser(
        SqlDialect dialect,
        Type parserType,
        string sql)
    {
        var resolver = CreateResolver();

        var parser = resolver.Resolve(dialect);
        var schema = await parser.ParseAsync(sql);

        Assert.IsType(parserType, parser);
        Assert.Equal(dialect, schema.Dialect);
        Assert.Single(schema.Tables);
    }

    private static ISchemaParserResolver CreateResolver()
    {
        return new SchemaParserResolver(
            new SqlServerSchemaParser(new SqlServerTypeMapper()),
            new MySqlSchemaParser(new MySqlTypeMapper()),
            new SqliteSchemaParser(new SqliteTypeMapper()),
            new Db2SchemaParser(new Db2TypeMapper()),
            new OracleSchemaParser(new OracleTypeMapper()),
            new PostgreSqlSchemaParser(new PostgreSqlTypeMapper()));
    }
}
