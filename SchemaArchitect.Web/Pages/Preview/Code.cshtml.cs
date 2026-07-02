using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Services;
using SchemaArchitect.Web.ViewModels;

namespace SchemaArchitect.Web.Pages.Preview;

/// <summary>
/// Displays generated source files and serves ZIP downloads.
/// </summary>
public sealed class CodeModel : PageModel
{
    private readonly IGeneratedFileArchiveService archiveService;
    private readonly IGenerationSessionStore sessionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeModel"/> class.
    /// </summary>
    /// <param name="archiveService">The ZIP archive service.</param>
    /// <param name="sessionStore">The temporary generation session store.</param>
    public CodeModel(
        IGeneratedFileArchiveService archiveService,
        IGenerationSessionStore sessionStore)
    {
        this.archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
        this.sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <summary>
    /// Gets or sets the temporary generation session identifier.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected generated file index.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int SelectedFileIndex { get; set; }

    /// <summary>
    /// Gets or sets the editable content for the selected generated file.
    /// </summary>
    [BindProperty]
    public string EditedContent { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a short success message for the current page.
    /// </summary>
    [TempData]
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Gets or sets a short error message for the current page.
    /// </summary>
    [TempData]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets generated file list items.
    /// </summary>
    public IReadOnlyList<GeneratedFileListItemViewModel> Files { get; private set; } = [];

    /// <summary>
    /// Gets the selected generated file.
    /// </summary>
    public GeneratedFile? SelectedFile { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the selected file has user edits.
    /// </summary>
    public bool SelectedFileIsEdited { get; private set; }

    /// <summary>
    /// Gets the CSS class used for basic code highlighting.
    /// </summary>
    public string CodeLanguageClass { get; private set; } = "language-text";

    /// <summary>
    /// Displays generated files.
    /// </summary>
    /// <returns>The code preview page.</returns>
    public IActionResult OnGet()
    {
        if (!TryLoadGeneratedFiles(out var session))
        {
            return RedirectToPage("/Upload/Index");
        }

        LoadViewModel(session);

        return Page();
    }

    /// <summary>
    /// Saves edited content for the selected generated file.
    /// </summary>
    /// <returns>A redirect to the selected file preview.</returns>
    public IActionResult OnPostSave()
    {
        if (!TryLoadGeneratedFiles(out var session))
        {
            return RedirectToPage("/Upload/Index");
        }

        SelectedFileIndex = Math.Clamp(SelectedFileIndex, 0, session.GeneratedFiles.Count - 1);
        var selectedFilePath = session.GeneratedFiles[SelectedFileIndex].RelativePath;

        if (!sessionStore.TryUpdateGeneratedFileContent(Id, SelectedFileIndex, EditedContent ?? string.Empty))
        {
            ErrorMessage = "We could not save that file. Please select it again and retry.";
            return RedirectToPage("/Preview/Code", new { id = Id, selectedFileIndex = SelectedFileIndex });
        }

        StatusMessage = $"Saved edits to {selectedFilePath}.";

        return RedirectToPage("/Preview/Code", new { id = Id, selectedFileIndex = SelectedFileIndex });
    }

    /// <summary>
    /// Restores the selected generated file to its original generated content.
    /// </summary>
    /// <returns>A redirect to the selected file preview.</returns>
    public IActionResult OnPostRestore()
    {
        if (!TryLoadGeneratedFiles(out var session))
        {
            return RedirectToPage("/Upload/Index");
        }

        SelectedFileIndex = Math.Clamp(SelectedFileIndex, 0, session.GeneratedFiles.Count - 1);
        var selectedFilePath = session.GeneratedFiles[SelectedFileIndex].RelativePath;

        if (!sessionStore.TryRestoreGeneratedFile(Id, SelectedFileIndex))
        {
            ErrorMessage = "We could not restore that file. Please select it again and retry.";
            return RedirectToPage("/Preview/Code", new { id = Id, selectedFileIndex = SelectedFileIndex });
        }

        StatusMessage = $"Restored {selectedFilePath} to the original generated version.";

        return RedirectToPage("/Preview/Code", new { id = Id, selectedFileIndex = SelectedFileIndex });
    }

    /// <summary>
    /// Downloads generated files as a ZIP archive.
    /// </summary>
    /// <returns>The generated ZIP file.</returns>
    public IActionResult OnGetDownload()
    {
        if (!TryLoadGeneratedFiles(out var session))
        {
            return RedirectToPage("/Upload/Index");
        }

        var archive = archiveService.CreateZipArchive(session.GeneratedFiles);
        var zipFileName = CreateSafeDownloadFileName(session.OriginalFileName);

        return File(archive, "application/zip", zipFileName);
    }

    private void LoadViewModel(GenerationSession session)
    {
        Files = session.GeneratedFiles
            .Select((file, index) => new GeneratedFileListItemViewModel
            {
                Index = index,
                RelativePath = file.RelativePath,
                IsEdited = IsEdited(session, index),
            })
            .ToArray();

        SelectedFileIndex = Math.Clamp(SelectedFileIndex, 0, session.GeneratedFiles.Count - 1);
        SelectedFile = session.GeneratedFiles[SelectedFileIndex];
        EditedContent = SelectedFile.Content;
        SelectedFileIsEdited = IsEdited(session, SelectedFileIndex);
        CodeLanguageClass = SelectedFile.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            ? "language-csharp"
            : "language-markdown";
    }

    private bool TryLoadGeneratedFiles(out GenerationSession session)
    {
        session = null!;

        if (!sessionStore.TryGet(Id, out var foundSession) ||
            foundSession is null ||
            foundSession.GeneratedFiles.Count == 0)
        {
            return false;
        }

        session = foundSession;

        return true;
    }

    private static bool IsEdited(GenerationSession session, int fileIndex)
    {
        if (fileIndex < 0 ||
            fileIndex >= session.GeneratedFiles.Count ||
            fileIndex >= session.OriginalGeneratedFiles.Count)
        {
            return false;
        }

        return !string.Equals(
            session.GeneratedFiles[fileIndex].Content,
            session.OriginalGeneratedFiles[fileIndex].Content,
            StringComparison.Ordinal);
    }

    private static string CreateSafeDownloadFileName(string originalFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "schemaarchitect";
        }

        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            baseName = baseName.Replace(invalidCharacter, '-');
        }

        return $"{baseName}-schemaarchitect.zip";
    }
}
