using SchemaArchitect.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSchemaArchitectWeb();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception exception)
    {
        var logDirectory = Path.Combine(app.Environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(logDirectory);

        var logPath = Path.Combine(logDirectory, "schemaarchitect-errors.log");
        var message = string.Join(
            Environment.NewLine,
            DateTimeOffset.UtcNow.ToString("u"),
            $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}",
            exception.ToString(),
            string.Empty,
            string.Empty);

        await File.AppendAllTextAsync(logPath, message);

        throw;
    }
});

app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
