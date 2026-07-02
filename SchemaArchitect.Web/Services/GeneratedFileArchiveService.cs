using System.IO.Compression;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Creates ZIP archives for generated source files.
/// </summary>
public sealed class GeneratedFileArchiveService : IGeneratedFileArchiveService
{
    /// <inheritdoc />
    public byte[] CreateZipArchive(IReadOnlyList<GeneratedFile> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        using var archiveStream = new MemoryStream();
        var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entryName = GetUniqueEntryName(SanitizeZipEntryName(file.RelativePath), usedEntryNames);
                var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);

                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream);
                writer.Write(file.Content);
            }
        }

        return archiveStream.ToArray();
    }

    private static string GetUniqueEntryName(string entryName, ISet<string> usedEntryNames)
    {
        if (usedEntryNames.Add(entryName))
        {
            return entryName;
        }

        var directory = Path.GetDirectoryName(entryName)?.Replace('\\', '/');
        var fileName = Path.GetFileNameWithoutExtension(entryName);
        var extension = Path.GetExtension(entryName);

        for (var index = 2; ; index++)
        {
            var candidateFileName = $"{fileName}-{index}{extension}";
            var candidate = string.IsNullOrWhiteSpace(directory)
                ? candidateFileName
                : $"{directory}/{candidateFileName}";

            if (usedEntryNames.Add(candidate))
            {
                return candidate;
            }
        }
    }

    private static string SanitizeZipEntryName(string relativePath)
    {
        var normalizedPath = relativePath
            .Replace('\\', '/')
            .TrimStart('/');

        var safeSegments = normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(static segment => segment is not "." and not "..")
            .Select(Path.GetFileName)
            .Where(static segment => !string.IsNullOrWhiteSpace(segment));

        var safePath = string.Join('/', safeSegments);

        return string.IsNullOrWhiteSpace(safePath)
            ? "generated-file.txt"
            : safePath;
    }
}
