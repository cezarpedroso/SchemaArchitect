using System.IO.Compression;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Services;

namespace SchemaArchitect.Tests.WebTests;

/// <summary>
/// Verifies temporary generation session editing behavior.
/// </summary>
public sealed class InMemoryGenerationSessionStoreTests
{
    /// <summary>
    /// Verifies edited generated files can be saved and restored to their original generated content.
    /// </summary>
    [Fact]
    public void GeneratedFileEditing_WhenSavedAndRestored_UpdatesCurrentFilesOnly()
    {
        var store = new InMemoryGenerationSessionStore();
        var session = store.Create("schema.sql", new DatabaseSchema(), SqlDialect.SqlServer);
        var originalContent = "public sealed class Customer { }";
        var editedContent = "public sealed class Customer { public int Id { get; set; } }";

        store.SaveGeneratedFiles(
            session.Id,
            new GenerationOptions(),
            [
                new GeneratedFile
                {
                    RelativePath = "Domain/Entities/Customer.cs",
                    Content = originalContent,
                },
            ]);

        var saveResult = store.TryUpdateGeneratedFileContent(session.Id, 0, editedContent);

        Assert.True(saveResult);
        Assert.True(store.TryGet(session.Id, out var editedSession));
        Assert.NotNull(editedSession);
        Assert.Equal(editedContent, editedSession.GeneratedFiles[0].Content);
        Assert.Equal(originalContent, editedSession.OriginalGeneratedFiles[0].Content);
        Assert.Equal(editedContent, ReadFirstZipEntry(new GeneratedFileArchiveService().CreateZipArchive(editedSession.GeneratedFiles)));

        var restoreResult = store.TryRestoreGeneratedFile(session.Id, 0);

        Assert.True(restoreResult);
        Assert.True(store.TryGet(session.Id, out var restoredSession));
        Assert.NotNull(restoredSession);
        Assert.Equal(originalContent, restoredSession.GeneratedFiles[0].Content);
    }

    private static string ReadFirstZipEntry(byte[] archiveBytes)
    {
        using var stream = new MemoryStream(archiveBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        using var entryStream = archive.Entries[0].Open();
        using var reader = new StreamReader(entryStream);

        return reader.ReadToEnd();
    }
}
