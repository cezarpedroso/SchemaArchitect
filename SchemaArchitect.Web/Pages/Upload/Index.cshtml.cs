using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchemaArchitect.Core.Interfaces;
using SchemaArchitect.Core.Models;
using SchemaArchitect.Web.Services;

namespace SchemaArchitect.Web.Pages.Upload;

/// <summary>
/// Handles SQL schema uploads.
/// </summary>
[RequestSizeLimit(5_242_880)]
[RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
public sealed class IndexModel : PageModel
{
    private const long MaxUploadBytes = 5_242_880;
    private static readonly SqlDialect[] DialectDisplayOrder =
    [
        SqlDialect.SqlServer,
        SqlDialect.PostgreSql,
        SqlDialect.MySql,
        SqlDialect.SQLite,
        SqlDialect.Oracle,
        SqlDialect.IbmDb2,
    ];

    private readonly ISchemaParserResolver schemaParserResolver;
    private readonly IGenerationSessionStore sessionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexModel"/> class.
    /// </summary>
    /// <param name="schemaParserResolver">The SQL schema parser resolver.</param>
    /// <param name="sessionStore">The temporary generation session store.</param>
    public IndexModel(ISchemaParserResolver schemaParserResolver, IGenerationSessionStore sessionStore)
    {
        this.schemaParserResolver = schemaParserResolver ?? throw new ArgumentNullException(nameof(schemaParserResolver));
        this.sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <summary>
    /// Gets the SQL dialect options shown on the upload page.
    /// </summary>
    public IReadOnlyList<SelectListItem> AvailableDialects { get; } = DialectDisplayOrder
        .Select(dialect => new SelectListItem(
            dialect.GetDisplayName(),
            dialect.ToString()))
        .ToArray();

    /// <summary>
    /// Gets or sets the SQL dialect selected for parsing.
    /// </summary>
    [BindProperty]
    public SqlDialect SelectedDialect { get; set; } = SqlDialect.SqlServer;

    /// <summary>
    /// Gets or sets the uploaded SQL schema file.
    /// </summary>
    [BindProperty]
    public IFormFile? SchemaFile { get; set; }

    /// <summary>
    /// Displays the upload page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Validates, reads, and parses the uploaded SQL schema file.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A redirect to schema preview when successful; otherwise, the upload page.</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ValidateUpload())
        {
            return Page();
        }

        try
        {
            await using var stream = SchemaFile!.OpenReadStream();
            using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            var schemaParser = schemaParserResolver.Resolve(SelectedDialect);
            var schema = await schemaParser.ParseAsync(sql, cancellationToken);

            if (schema.Tables.Count == 0)
            {
                ModelState.AddModelError(nameof(SchemaFile), "No CREATE TABLE statements were found in the uploaded file.");
                return Page();
            }

            var session = sessionStore.Create(SchemaFile.FileName, schema, SelectedDialect);

            return RedirectToPage("/Preview/Index", new { id = session.Id });
        }
        catch (NotSupportedException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
        }
        catch (FormatException exception)
        {
            ModelState.AddModelError(nameof(SchemaFile), $"The SQL file could not be parsed: {exception.Message}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(nameof(SchemaFile), $"Unexpected parsing error: {exception.Message}");
        }

        return Page();
    }

    private bool ValidateUpload()
    {
        if (SchemaFile is null)
        {
            ModelState.AddModelError(nameof(SchemaFile), "Choose a .sql schema file to upload.");
            return false;
        }

        if (!Enum.IsDefined(SelectedDialect))
        {
            ModelState.AddModelError(nameof(SelectedDialect), "Choose a supported SQL dialect.");
            return false;
        }

        if (SchemaFile.Length == 0)
        {
            ModelState.AddModelError(nameof(SchemaFile), "The uploaded file is empty.");
            return false;
        }

        if (SchemaFile.Length > MaxUploadBytes)
        {
            ModelState.AddModelError(nameof(SchemaFile), "The uploaded file is too large. Maximum file size is 5 MB.");
            return false;
        }

        var extension = Path.GetExtension(SchemaFile.FileName);
        if (!string.Equals(extension, ".sql", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(SchemaFile), "Upload a file with the .sql extension.");
            return false;
        }

        return true;
    }
}
