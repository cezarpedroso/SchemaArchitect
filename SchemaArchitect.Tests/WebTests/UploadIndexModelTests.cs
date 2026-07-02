using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Pages.Upload;
using SchemaArchitect.Web.Services;

namespace SchemaArchitect.Tests.WebTests;

/// <summary>
/// Unit tests for the Upload Index page model.
/// </summary>
public sealed class UploadIndexModelTests
{
    [Fact]
    public async Task OnPostAsync_WhenNoFileProvided_ReturnsPageWithModelError()
    {
        var resolver = new FakeResolver(new FakeParser(new DatabaseSchema()));
        var store = new InMemoryGenerationSessionStore();
        var model = new IndexModel(resolver, store);

        // No SchemaFile set -> validation should fail
        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(nameof(model.SchemaFile)));
    }

    [Fact]
    public async Task OnPostAsync_WhenWrongExtension_ReturnsPageWithModelError()
    {
        var resolver = new FakeResolver(new FakeParser(new DatabaseSchema { Tables = new[] { new TableSchema { Name = "T" } } }));
        var store = new InMemoryGenerationSessionStore();
        var model = new IndexModel(resolver, store);

        var content = "CREATE TABLE X (Id INT);";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "schema.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        model.SchemaFile = formFile;

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(nameof(model.SchemaFile)));
    }

    [Fact]
    public async Task OnPostAsync_WhenParserReturnsNoTables_AddsModelErrorAndReturnsPage()
    {
        var resolver = new FakeResolver(new FakeParser(new DatabaseSchema()));
        var store = new InMemoryGenerationSessionStore();
        var model = new IndexModel(resolver, store);

        var content = "-- no create table statements";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "schema.sql")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/sql"
        };

        model.SchemaFile = formFile;

        var result = await model.OnPostAsync(CancellationToken.None);

        Assert.IsType<PageResult>(result);
        Assert.False(model.ModelState.IsValid);
        Assert.True(model.ModelState.ContainsKey(nameof(model.SchemaFile)));
    }

    [Fact]
    public async Task OnPostAsync_WhenValidSchema_RedirectsToPreview()
    {
        var schema = new DatabaseSchema { Tables = new[] { new TableSchema { Name = "Customers" } } };
        var resolver = new FakeResolver(new FakeParser(schema));
        var store = new InMemoryGenerationSessionStore();
        var model = new IndexModel(resolver, store);

        var content = "CREATE TABLE Customers (CustomerId INT);";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var formFile = new FormFile(stream, 0, stream.Length, "file", "schema.sql")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/sql"
        };

        model.SchemaFile = formFile;

        var result = await model.OnPostAsync(CancellationToken.None);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Preview/Index", redirect.PageName);
        Assert.True(redirect.RouteValues.ContainsKey("id"));
    }

    private sealed class FakeResolver : ISchemaParserResolver
    {
        private readonly ISchemaParser parser;

        public FakeResolver(ISchemaParser parser)
        {
            this.parser = parser;
        }

        public ISchemaParser Resolve(SqlDialect dialect) => parser;
    }

    private sealed class FakeParser : ISchemaParser
    {
        private readonly DatabaseSchema schema;

        public FakeParser(DatabaseSchema schema)
        {
            this.schema = schema;
        }

        public Task<DatabaseSchema> ParseAsync(string schemaSql, CancellationToken cancellationToken = default)
            => Task.FromResult(schema);
    }
}
