using System.Collections.Concurrent;
using SchemaArchitect.Core.Models;

namespace SchemaArchitect.Web.Services;

/// <summary>
/// Stores generation sessions in process memory for the web flow.
/// </summary>
public sealed class InMemoryGenerationSessionStore : IGenerationSessionStore
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(2);
    private readonly ConcurrentDictionary<string, GenerationSession> sessions = new();

    /// <inheritdoc />
    public GenerationSession Create(string originalFileName, DatabaseSchema schema, SqlDialect dialect)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentNullException.ThrowIfNull(schema);

        RemoveExpiredSessions();

        var id = Guid.NewGuid().ToString("N");
        var session = new GenerationSession
        {
            Id = id,
            OriginalFileName = Path.GetFileName(originalFileName),
            Dialect = dialect,
            Schema = schema,
        };

        sessions[id] = session;

        return session;
    }

    /// <inheritdoc />
    public bool TryGet(string id, out GenerationSession? session)
    {
        session = null;

        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        RemoveExpiredSessions();

        if (!sessions.TryGetValue(id, out var foundSession))
        {
            return false;
        }

        foundSession.LastAccessedUtc = DateTimeOffset.UtcNow;
        session = foundSession;

        return true;
    }

    /// <inheritdoc />
    public void SaveGeneratedFiles(string id, GenerationOptions options, IReadOnlyList<GeneratedFile> files)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(files);

        if (!TryGet(id, out var session) || session is null)
        {
            throw new InvalidOperationException("The generation session no longer exists.");
        }

        var generatedFiles = files.ToArray();

        session.Options = options;
        session.GeneratedFiles = generatedFiles;
        session.OriginalGeneratedFiles = generatedFiles;
        session.LastAccessedUtc = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public bool TryUpdateGeneratedFileContent(string id, int fileIndex, string content)
    {
        if (!TryGet(id, out var session) || session is null)
        {
            return false;
        }

        lock (session)
        {
            if (fileIndex < 0 || fileIndex >= session.GeneratedFiles.Count)
            {
                return false;
            }

            var files = session.GeneratedFiles.ToArray();
            files[fileIndex] = files[fileIndex] with
            {
                Content = content,
            };

            session.GeneratedFiles = files;
            session.LastAccessedUtc = DateTimeOffset.UtcNow;

            return true;
        }
    }

    /// <inheritdoc />
    public bool TryRestoreGeneratedFile(string id, int fileIndex)
    {
        if (!TryGet(id, out var session) || session is null)
        {
            return false;
        }

        lock (session)
        {
            if (fileIndex < 0 ||
                fileIndex >= session.GeneratedFiles.Count ||
                fileIndex >= session.OriginalGeneratedFiles.Count)
            {
                return false;
            }

            var files = session.GeneratedFiles.ToArray();
            files[fileIndex] = session.OriginalGeneratedFiles[fileIndex];

            session.GeneratedFiles = files;
            session.LastAccessedUtc = DateTimeOffset.UtcNow;

            return true;
        }
    }

    private void RemoveExpiredSessions()
    {
        var cutoff = DateTimeOffset.UtcNow - SessionLifetime;

        foreach (var session in sessions)
        {
            if (session.Value.LastAccessedUtc < cutoff)
            {
                sessions.TryRemove(session.Key, out _);
            }
        }
    }
}
