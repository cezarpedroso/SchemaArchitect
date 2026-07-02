using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Services;

namespace SchemaArchitect.Tests.WebTests;

/// <summary>
/// Verifies schema health report behavior.
/// </summary>
public sealed class SchemaHealthAnalyzerTests
{
    /// <summary>
    /// Verifies common schema quality findings are reported.
    /// </summary>
    [Fact]
    public void Analyze_WhenSchemaHasRiskyModelingChoices_ReturnsWarningsAndIssues()
    {
        var analyzer = new SchemaHealthAnalyzer();
        var schema = new DatabaseSchema
        {
            Tables =
            [
                new TableSchema
                {
                    Schema = "dbo",
                    Name = "Customers",
                    Columns =
                    [
                        new ColumnSchema
                        {
                            Name = "CustomerId",
                            StoreType = "int",
                            CSharpType = "int",
                            IsPrimaryKey = true,
                            IsIdentity = true,
                        },
                    ],
                },
                new TableSchema
                {
                    Schema = "dbo",
                    Name = "Orders",
                    Columns =
                    [
                        new ColumnSchema
                        {
                            Name = "OrderId",
                            StoreType = "int",
                            CSharpType = "int",
                        },
                        new ColumnSchema
                        {
                            Name = "CustomerId",
                            StoreType = "int",
                            CSharpType = "int?",
                            IsNullable = true,
                        },
                    ],
                    ForeignKeys =
                    [
                        new ForeignKeySchema
                        {
                            Name = "FK_Orders_Customers",
                            Columns = ["CustomerId"],
                            PrincipalSchema = "dbo",
                            PrincipalTable = "Customers",
                            PrincipalColumns = ["CustomerId"],
                        },
                    ],
                },
            ],
        };

        var report = analyzer.Analyze(schema);

        Assert.Equal(SchemaHealthSeverity.Issue, report.OverallSeverity);
        Assert.Contains(report.Items, item => item.Title == "Missing primary key" && item.TableName == "dbo.Orders");
        Assert.Contains(report.Items, item => item.Title == "Nullable foreign key" && item.ColumnName == "CustomerId");
        Assert.Contains(report.Items, item => item.Title == "Identity column detected" && item.ColumnName == "CustomerId");
    }
}
