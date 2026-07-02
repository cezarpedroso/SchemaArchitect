using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Tests.ParsingTests;

/// <summary>
/// Verifies schema model metadata and helper behavior.
/// </summary>
public sealed class SchemaModelTests
{
    /// <summary>
    /// Verifies decimal precision and scale are retained by column metadata.
    /// </summary>
    [Fact]
    public void ColumnSchema_WhenDecimalFacetsAreProvided_StoresPrecisionAndScale()
    {
        var column = new ColumnSchema
        {
            Name = "Amount",
            StoreType = "decimal",
            CSharpType = "decimal",
            Precision = 18,
            Scale = 2,
        };

        Assert.Equal((byte)18, column.Precision);
        Assert.Equal((byte)2, column.Scale);
    }

    /// <summary>
    /// Verifies character maximum length is retained by column metadata.
    /// </summary>
    [Fact]
    public void ColumnSchema_WhenMaxLengthIsProvided_StoresMaxLength()
    {
        var column = new ColumnSchema
        {
            Name = "Name",
            StoreType = "nvarchar",
            CSharpType = "string",
            MaxLength = 200,
        };

        Assert.Equal(200, column.MaxLength);
        Assert.True(column.IsString);
        Assert.False(column.IsValueType);
    }

    /// <summary>
    /// Verifies primary key columns can be discovered from a table.
    /// </summary>
    [Fact]
    public void PrimaryKey_WhenTableHasPrimaryKeyColumn_ReturnsPrimaryKeyColumns()
    {
        var idColumn = new ColumnSchema
        {
            Name = "Id",
            StoreType = "int",
            CSharpType = "int",
            IsPrimaryKey = true,
            IsIdentity = true,
        };

        var nameColumn = new ColumnSchema
        {
            Name = "Name",
            StoreType = "nvarchar",
            CSharpType = "string",
        };

        var table = new TableSchema
        {
            Name = "Users",
            Columns = [idColumn, nameColumn],
        };

        var primaryKey = Assert.Single(table.PrimaryKey);
        Assert.Equal("Id", primaryKey.Name);
        Assert.True(primaryKey.IsIdentity);
        Assert.True(primaryKey.IsValueType);
    }

    /// <summary>
    /// Verifies foreign key columns and referenced table metadata can be discovered.
    /// </summary>
    [Fact]
    public void ForeignKeyColumns_WhenTableHasForeignKey_ReturnsForeignKeyColumns()
    {
        var customerIdColumn = new ColumnSchema
        {
            Name = "CustomerId",
            StoreType = "int",
            CSharpType = "int",
        };

        var orderNumberColumn = new ColumnSchema
        {
            Name = "OrderNumber",
            StoreType = "nvarchar",
            CSharpType = "string",
            MaxLength = 50,
        };

        var foreignKey = new ForeignKeySchema
        {
            Name = "FK_Orders_Customers_CustomerId",
            Columns = ["CustomerId"],
            PrincipalTable = "Customers",
            PrincipalColumns = ["Id"],
        };

        var table = new TableSchema
        {
            Name = "Orders",
            Columns = [customerIdColumn, orderNumberColumn],
            ForeignKeys = [foreignKey],
        };

        var foreignKeyColumn = Assert.Single(table.ForeignKeyColumns);
        Assert.Equal("CustomerId", foreignKeyColumn.Name);
        Assert.Equal("Customers", foreignKey.ReferencedTable);
        Assert.Equal("Id", foreignKey.ReferencedColumn);
    }
}
