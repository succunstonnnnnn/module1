using TableDb.Core;
using TableDb.Core.Controllers;
using TableDb.Core.FieldTypes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new DatabaseController());

var app = builder.Build();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { error = "Некоректний JSON у тілі запиту" });
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

object sync = new object();

string dataDir = Path.Combine(app.Environment.ContentRootPath, "data");

static IResult Bad(string message) => Results.BadRequest(new { error = message });
static IResult NotFoundTable(string name) =>
    Results.NotFound(new { error = $"Таблиця '{name}' не існує" });

static IResult FromResult(ValidationResult result) =>
    result.IsValid ? Results.Ok(new { ok = true }) : Bad(result.Error);

static async ValueTask<object?> RequireDatabase(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
{
    DatabaseController db = context.HttpContext.RequestServices.GetRequiredService<DatabaseController>();
    if (!db.HasDatabase)
        return Results.Conflict(new { error = DatabaseController.NoDatabaseMessage });
    return await next(context);
}

app.MapGet("/api/types", () =>
{
    var list = new List<object>();
    foreach (string name in FieldTypeFactory.AllTypeNames())
        list.Add(new { name, hint = FieldTypeFactory.Create(name).Hint });
    return Results.Ok(list);
});

app.MapGet("/api/db", (DatabaseController db) =>
{
    lock (sync)
    {
        if (!db.HasDatabase)
            return Results.Ok(new { exists = false });

        var tables = new List<object>();
        foreach (string name in db.GetTableNames())
        {
            Table table = db.GetTable(name)!;
            tables.Add(new { name, columnCount = table.Columns.Count, rowCount = table.Rows.Count });
        }
        return Results.Ok(new { exists = true, name = db.DatabaseName, tables });
    }
});

app.MapPost("/api/db", (NewDbRequest req, DatabaseController db) =>
{
    lock (sync) return FromResult(db.NewDatabase(req.Name ?? string.Empty));
});

app.MapPost("/api/db/save", (FileRequest req, DatabaseController db) =>
{
    lock (sync)
    {
        string? path = ResolvePath(dataDir, req.FileName, out string error);
        if (path == null) return Bad(error);
        Directory.CreateDirectory(dataDir);
        return FromResult(db.Save(path));
    }
}).AddEndpointFilter(RequireDatabase);

app.MapPost("/api/db/load", (FileRequest req, DatabaseController db) =>
{
    lock (sync)
    {
        string? path = ResolvePath(dataDir, req.FileName, out string error);
        if (path == null) return Bad(error);
        if (!File.Exists(path)) return Bad($"Файл '{Path.GetFileName(path)}' не знайдено на сервері");
        return FromResult(db.Load(path));
    }
});

app.MapPost("/api/tables", (CreateTableRequest req, DatabaseController db) =>
{
    lock (sync)
    {
        var columns = new List<Column>();
        foreach (ColumnRequest c in req.Columns ?? new List<ColumnRequest>())
        {
            try
            {
                columns.Add(new Column((c.Name ?? string.Empty).Trim(), FieldTypeFactory.Create(c.Type ?? string.Empty)));
            }
            catch (ArgumentException ex)
            {
                return Bad(ex.Message);
            }
        }
        return FromResult(db.CreateTable((req.Name ?? string.Empty).Trim(), columns));
    }
}).AddEndpointFilter(RequireDatabase);

app.MapDelete("/api/tables/{name}", (string name, DatabaseController db) =>
{
    lock (sync)
    {
        if (db.GetTable(name) == null) return NotFoundTable(name);
        return FromResult(db.DropTable(name));
    }
}).AddEndpointFilter(RequireDatabase);

app.MapGet("/api/tables/{name}", (string name, DatabaseController db) =>
{
    lock (sync)
    {
        Table? table = db.GetTable(name);
        if (table == null) return NotFoundTable(name);

        var columns = new List<object>();
        foreach (Column c in table.Columns)
            columns.Add(new { name = c.Name, type = c.Type.Name, hint = c.Type.Hint });

        return Results.Ok(new { name = table.Name, columns, rows = table.Rows });
    }
}).AddEndpointFilter(RequireDatabase);

app.MapPost("/api/tables/{name}/rows", (string name, RowRequest req, DatabaseController db) =>
{
    lock (sync)
    {
        if (db.GetTable(name) == null) return NotFoundTable(name);
        return FromResult(db.Rows.AddRow(name, req.Values ?? new List<string>()));
    }
}).AddEndpointFilter(RequireDatabase);

app.MapPut("/api/tables/{name}/rows/{index:int}", (string name, int index, RowRequest req, DatabaseController db) =>
{
    lock (sync)
    {
        if (db.GetTable(name) == null) return NotFoundTable(name);
        return FromResult(db.Rows.EditRow(name, index, req.Values ?? new List<string>()));
    }
}).AddEndpointFilter(RequireDatabase);

app.MapDelete("/api/tables/{name}/rows/{index:int}", (string name, int index, DatabaseController db) =>
{
    lock (sync)
    {
        if (db.GetTable(name) == null) return NotFoundTable(name);
        return FromResult(db.Rows.DeleteRow(name, index));
    }
}).AddEndpointFilter(RequireDatabase);

app.MapPost("/api/operations/product", (ProductRequest req, DatabaseController db) =>
{
    lock (sync)
    {
        return FromResult(db.Product.CartesianProduct(
            req.TableA ?? string.Empty, req.TableB ?? string.Empty, (req.ResultName ?? string.Empty).Trim()));
    }
}).AddEndpointFilter(RequireDatabase);

app.Run();

static string? ResolvePath(string dataDir, string? fileName, out string error)
{
    error = string.Empty;
    string name = string.IsNullOrWhiteSpace(fileName) ? "database.json" : fileName.Trim();

    if (Path.GetFileName(name) != name)
    {
        error = "Вкажіть лише ім'я файлу, без папок";
        return null;
    }
    if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        name += ".json";

    return Path.Combine(dataDir, name);
}

record NewDbRequest(string? Name);
record FileRequest(string? FileName);
record ColumnRequest(string? Name, string? Type);
record CreateTableRequest(string? Name, List<ColumnRequest>? Columns);
record RowRequest(List<string>? Values);
record ProductRequest(string? TableA, string? TableB, string? ResultName);
