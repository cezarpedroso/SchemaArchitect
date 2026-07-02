using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Services;
using SchemaArchitect.Web.ViewModels;

namespace SchemaArchitect.Web.Pages.Preview;

/// <summary>
/// Displays the parsed schema and handles generation option selection.
/// </summary>
public sealed class IndexModel : PageModel
{
    private readonly ICodeGenerator codeGenerator;
    private readonly ISchemaHealthAnalyzer schemaHealthAnalyzer;
    private readonly IGenerationSessionStore sessionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="codeGenerator">The source code generator used after the user confirms generation settings.</param>
    /// <param name="schemaHealthAnalyzer">The schema health analyzer.</param>
    /// <param name="sessionStore">The temporary generation session store.</param>
    public IndexModel(
        ICodeGenerator codeGenerator,
        ISchemaHealthAnalyzer schemaHealthAnalyzer,
        IGenerationSessionStore sessionStore)
    {
        this.codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        this.schemaHealthAnalyzer = schemaHealthAnalyzer ?? throw new ArgumentNullException(nameof(schemaHealthAnalyzer));
        this.sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <summary>
    /// Gets or sets the temporary generation session identifier.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected table index shown in the schema explorer.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int SelectedTableIndex { get; set; }

    /// <summary>
    /// Gets or sets the generation options selected by the user.
    /// </summary>
    [BindProperty]
    public GenerationOptionsInputModel Options { get; set; } = new();

    /// <summary>
    /// Gets the loaded temporary generation session.
    /// </summary>
    public GenerationSession? Session { get; private set; }

    /// <summary>
    /// Gets the selected table shown in the schema explorer.
    /// </summary>
    public TableSchema? SelectedTable { get; private set; }

    /// <summary>
    /// Gets the schema health report for the parsed schema.
    /// </summary>
    public SchemaHealthReport? HealthReport { get; private set; }

    /// <summary>
    /// Displays the parsed schema preview.
    /// </summary>
    /// <returns>The schema preview page.</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!TryLoadSession())
        {
            return RedirectToPage("/Upload/Index");
        }

        Options = GenerationOptionsInputModel.FromGenerationOptions(Session!.Options);
        await LoadExplorerAsync(cancellationToken);

        return Page();
    }

    /// <summary>
    /// Generates files from the parsed schema and selected options.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A redirect to code preview when successful; otherwise, the schema preview page.</returns>
    public async Task<IActionResult> OnPostGenerateAsync(CancellationToken cancellationToken)
    {
        if (!TryLoadSession())
        {
            return RedirectToPage("/Upload/Index");
        }

        if (!HasAnyOutputSelected())
        {
            ModelState.AddModelError(string.Empty, "Select at least one output to generate.");
        }

        if (!ModelState.IsValid)
        {
            await LoadExplorerAsync(cancellationToken);
            return Page();
        }

        var generationOptions = Options.ToGenerationOptions();
        var files = await codeGenerator.GenerateAsync(Session!.Schema, generationOptions, cancellationToken);

        if (files.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No files were generated. Select at least one output option.");
            await LoadExplorerAsync(cancellationToken);
            return Page();
        }

        sessionStore.SaveGeneratedFiles(Id, generationOptions, files);

        return RedirectToPage("/Preview/Code", new { id = Id });
    }

    private bool TryLoadSession()
    {
        if (!sessionStore.TryGet(Id, out var session) || session is null)
        {
            return false;
        }

        Session = session;
        return true;
    }

    private async Task LoadExplorerAsync(CancellationToken cancellationToken)
    {
        if (Session is null || Session.Schema.Tables.Count == 0)
        {
            SelectedTable = null;
            return;
        }

        SelectedTableIndex = Math.Clamp(SelectedTableIndex, 0, Session.Schema.Tables.Count - 1);
        SelectedTable = Session.Schema.Tables[SelectedTableIndex];
        HealthReport = schemaHealthAnalyzer.Analyze(Session.Schema);
        await Task.CompletedTask;
    }

    private bool HasAnyOutputSelected()
    {
        return Options.GenerateEntities ||
            Options.GenerateDbContext ||
            Options.GenerateConfigurations ||
            Options.GenerateDtos ||
            Options.GenerateControllers ||
            Options.GenerateMigrationInstructions;
    }

}
