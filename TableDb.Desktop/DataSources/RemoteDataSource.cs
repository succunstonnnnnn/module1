using System.Text;
using System.Text.Json;
using TableDb.Core;
using TableDb.Core.FieldTypes;

namespace TableDb.Desktop.DataSources;

public class RemoteDataSource : IDataSource
{
    private readonly HttpClient _http;
    private readonly string _baseAddress;

    public bool IsRemote => true;

    public string? CurrentTarget { get; private set; }

    public string Description => "Сервер: " + _baseAddress;

    public RemoteDataSource(string baseAddress, HttpClient? client = null)
    {
        _baseAddress = baseAddress.TrimEnd('/');
        _http = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public static bool TryNormalizeAddress(string input, out string address, out string error)
    {
        address = string.Empty;
        error = string.Empty;

        string text = input.Trim();
        if (text.Length == 0)
        {
            error = "Вкажіть адресу сервера, наприклад http://localhost:5080";
            return false;
        }

        if (!text.Contains("://"))
            text = "http://" + text;

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "Некоректна адреса сервера. Приклад: http://localhost:5080";
            return false;
        }

        address = text.TrimEnd('/');
        return true;
    }

    public async Task<ValidationResult> CheckConnectionAsync()
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Get, "/api/db", null);
        return response.IsOk ? ValidationResult.Ok() : ValidationResult.Fail(response.Error);
    }

    public async Task<ValidationResult> NewDatabaseAsync(string name)
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Post, "/api/db", new { name });
        if (response.IsOk)
            CurrentTarget = null;
        return ToValidation(response);
    }

    public async Task<Result<string?>> GetDatabaseNameAsync()
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Get, "/api/db", null);
        if (!response.IsOk)
            return Result<string?>.Fail(response.Error);

        if (ServerHasNoDatabase(response.Value))
            return Result<string?>.Ok(null);

        try
        {
            return Result<string?>.Ok(response.Value.GetProperty("name").GetString() ?? string.Empty);
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
        {
            return Result<string?>.Fail(BadResponseMessage);
        }
    }

    public async Task<Result<List<string>>> GetTableNamesAsync()
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Get, "/api/db", null);
        if (!response.IsOk)
            return Result<List<string>>.Fail(response.Error);

        if (ServerHasNoDatabase(response.Value))
            return Result<List<string>>.Ok(new List<string>());

        try
        {
            List<string> names = new List<string>();
            foreach (JsonElement table in response.Value.GetProperty("tables").EnumerateArray())
                names.Add(table.GetProperty("name").GetString() ?? string.Empty);
            return Result<List<string>>.Ok(names);
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException)
        {
            return Result<List<string>>.Fail(BadResponseMessage);
        }
    }

    public async Task<Result<Table>> GetTableAsync(string name)
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Get, "/api/tables/" + Escape(name), null);
        if (!response.IsOk)
            return Result<Table>.Fail(response.Error);

        try
        {
            Table table = new Table(name);
            foreach (JsonElement column in response.Value.GetProperty("columns").EnumerateArray())
            {
                string columnName = column.GetProperty("name").GetString() ?? string.Empty;
                string typeName = column.GetProperty("type").GetString() ?? string.Empty;
                table.Columns.Add(new Column(columnName, FieldTypeFactory.Create(typeName)));
            }

            foreach (JsonElement rowElement in response.Value.GetProperty("rows").EnumerateArray())
            {
                List<string> row = new List<string>();
                foreach (JsonElement value in rowElement.EnumerateArray())
                    row.Add(value.GetString() ?? string.Empty);
                table.Rows.Add(row);
            }

            return Result<Table>.Ok(table);
        }
        catch (Exception ex) when (ex is KeyNotFoundException || ex is InvalidOperationException || ex is ArgumentException)
        {
            return Result<Table>.Fail(BadResponseMessage);
        }
    }

    public async Task<ValidationResult> CreateTableAsync(string name, List<Column> columns)
    {
        List<object> columnList = new List<object>();
        foreach (Column column in columns)
            columnList.Add(new { name = column.Name, type = column.Type.Name });

        return ToValidation(await SendAsync(HttpMethod.Post, "/api/tables", new { name, columns = columnList }));
    }

    public async Task<ValidationResult> DropTableAsync(string name)
    {
        return ToValidation(await SendAsync(HttpMethod.Delete, "/api/tables/" + Escape(name), null));
    }

    public async Task<ValidationResult> AddRowAsync(string tableName, List<string> values)
    {
        return ToValidation(await SendAsync(HttpMethod.Post, "/api/tables/" + Escape(tableName) + "/rows", new { values }));
    }

    public async Task<ValidationResult> EditRowAsync(string tableName, int rowIndex, List<string> values)
    {
        string path = "/api/tables/" + Escape(tableName) + "/rows/" + rowIndex;
        return ToValidation(await SendAsync(HttpMethod.Put, path, new { values }));
    }

    public async Task<ValidationResult> DeleteRowAsync(string tableName, int rowIndex)
    {
        string path = "/api/tables/" + Escape(tableName) + "/rows/" + rowIndex;
        return ToValidation(await SendAsync(HttpMethod.Delete, path, null));
    }

    public async Task<ValidationResult> CartesianProductAsync(string tableAName, string tableBName, string resultName)
    {
        object body = new { tableA = tableAName, tableB = tableBName, resultName };
        return ToValidation(await SendAsync(HttpMethod.Post, "/api/operations/product", body));
    }

    public async Task<ValidationResult> SaveAsync(string target)
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Post, "/api/db/save", new { fileName = target });
        if (response.IsOk)
            CurrentTarget = target;
        return ToValidation(response);
    }

    public async Task<ValidationResult> LoadAsync(string target)
    {
        Result<JsonElement> response = await SendAsync(HttpMethod.Post, "/api/db/load", new { fileName = target });
        if (response.IsOk)
            CurrentTarget = target;
        return ToValidation(response);
    }

    private const string BadResponseMessage = "Сервер повернув відповідь у невідомому форматі";

    private static bool ServerHasNoDatabase(JsonElement root)
    {
        return root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("exists", out JsonElement exists)
            && exists.ValueKind == JsonValueKind.False;
    }

    private static string Escape(string text) => Uri.EscapeDataString(text);

    private static ValidationResult ToValidation(Result<JsonElement> response)
    {
        return response.IsOk ? ValidationResult.Ok() : ValidationResult.Fail(response.Error);
    }

    private async Task<Result<JsonElement>> SendAsync(HttpMethod method, string path, object? body)
    {
        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(method, _baseAddress + path);
            if (body != null)
                request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            using HttpResponseMessage response = await _http.SendAsync(request);
            string text = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Result<JsonElement>.Fail(ReadServerError(text, (int)response.StatusCode));

            if (text.Length == 0)
                return Result<JsonElement>.Ok(default);

            using JsonDocument document = JsonDocument.Parse(text);
            return Result<JsonElement>.Ok(document.RootElement.Clone());
        }
        catch (OperationCanceledException)
        {
            return Result<JsonElement>.Fail($"Сервер {_baseAddress} не відповів вчасно. Спробуйте ще раз.");
        }
        catch (HttpRequestException)
        {
            return Result<JsonElement>.Fail(
                $"Не вдалося з'єднатися з сервером {_baseAddress}. Перевірте адресу й чи запущено сервер.");
        }
        catch (JsonException)
        {
            return Result<JsonElement>.Fail(BadResponseMessage);
        }
    }

    private static string ReadServerError(string body, int statusCode)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out JsonElement error)
                && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return $"Сервер повернув помилку (код {statusCode})";
    }
}
