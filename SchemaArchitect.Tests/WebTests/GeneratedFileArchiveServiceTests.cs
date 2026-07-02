using System.IO.Compression;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Services;

namespace SchemaArchitect.Tests.WebTests;

/// <summary>
/// Verifies generated file ZIP export behavior.
/// </summary>
public sealed class GeneratedFileArchiveServiceTests
{
    /// <summary>
    /// Verifies generated files are written to a ZIP archive using safe entry paths.
    /// </summary>
    [Fact]
    public void CreateZipArchive_WhenFilesAreProvided_CreatesExpectedEntries()
    {
        var service = new GeneratedFileArchiveService();
        var files = new[]
        {
            new GeneratedFile
            {
                RelativePath = "Domain/Entities/Customer.cs",
                Content = "public class Customer { }",
            },
            new GeneratedFile
            {
                RelativePath = "../README_MIGRATIONS.md",
                Content = "# Migrations",
            },
            new GeneratedFile
            {
                RelativePath = "README_MIGRATIONS.md",
                Content = "# Duplicate migrations",
            },
        };

        var archiveBytes = service.CreateZipArchive(files);

        using var stream = new MemoryStream(archiveBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entityEntry = archive.GetEntry("Domain/Entities/Customer.cs");
        var readmeEntry = archive.GetEntry("README_MIGRATIONS.md");
        var duplicateReadmeEntry = archive.GetEntry("README_MIGRATIONS-2.md");

        Assert.NotNull(entityEntry);
        Assert.NotNull(readmeEntry);
        Assert.NotNull(duplicateReadmeEntry);
        Assert.Null(archive.GetEntry("../README_MIGRATIONS.md"));
        Assert.Equal("public class Customer { }", ReadEntry(entityEntry!));
        Assert.Equal("# Migrations", ReadEntry(readmeEntry!));
        Assert.Equal("# Duplicate migrations", ReadEntry(duplicateReadmeEntry!));
    }

    private static string ReadEntry(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd();
    }
}
