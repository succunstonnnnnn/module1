using System.Net;
using System.Text;
using TableDb.Core;
using TableDb.Core.FieldTypes;
using TableDb.Desktop.DataSources;
using Xunit;

namespace TableDb.Tests;

public class FakeHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string, HttpResponseMessage> _respond;

    public string LastMethod = string.Empty;
    public string LastUrl = string.Empty;
    public string LastBody = string.Empty;

    public FakeHandler(Func<HttpRequestMessage, string, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastMethod = request.Method.Method;
        LastUrl = request.RequestUri!.AbsoluteUri;
        LastBody = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        return _respond(request, LastBody);
    }
}

public class RemoteDataSourceTests
{
    private static HttpResponseMessage Json(HttpStatusCode code, string json)
    {
        return new HttpResponseMessage(code) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private static RemoteDataSource Make(FakeHandler handler)
    {
        return new RemoteDataSource("http://srv:5080", new HttpClient(handler));
    }

    [Fact]
    public async Task ServerValidationError_ShowsSameTextAsServerSent()
    {
        FakeHandler handler = new FakeHandler((_, _) =>
            Json(HttpStatusCode.BadRequest, "{\"error\":\"Поле 'id' (integer): некоректне значення.\"}"));

        ValidationResult result = await Make(handler).AddRowAsync("A", new List<string> { "x" });

        Assert.False(result.IsValid);
        Assert.Equal("Поле 'id' (integer): некоректне значення.", result.Error);
    }

    [Fact]
    public async Task ServerUnreachable_GivesUkrainianMessageInsteadOfException()
    {
        FakeHandler handler = new FakeHandler((_, _) => throw new HttpRequestException("Connection refused"));

        Result<List<string>> result = await Make(handler).GetTableNamesAsync();

        Assert.False(result.IsOk);
        Assert.Contains("Не вдалося з'єднатися з сервером http://srv:5080", result.Error);
    }

    [Fact]
    public async Task Timeout_GivesUkrainianMessage()
    {
        FakeHandler handler = new FakeHandler((_, _) => throw new TaskCanceledException("timeout"));

        ValidationResult result = await Make(handler).CheckConnectionAsync();

        Assert.False(result.IsValid);
        Assert.Contains("не відповів вчасно", result.Error);
    }

    [Fact]
    public async Task ServerErrorWithoutJson_ReportsStatusCode()
    {
        FakeHandler handler = new FakeHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("oops") });

        ValidationResult result = await Make(handler).DropTableAsync("A");

        Assert.False(result.IsValid);
        Assert.Contains("код 500", result.Error);
    }

    [Fact]
    public async Task GarbageResponse_DoesNotThrow()
    {
        FakeHandler handler = new FakeHandler((_, _) => Json(HttpStatusCode.OK, "<html>not json</html>"));

        Result<string?> result = await Make(handler).GetDatabaseNameAsync();

        Assert.False(result.IsOk);
        Assert.Contains("невідомому форматі", result.Error);
    }

    [Fact]
    public async Task GetTable_BuildsSchemaAndRows()
    {
        string json = "{\"name\":\"T\",\"columns\":[{\"name\":\"id\",\"type\":\"integer\"},{\"name\":\"f\",\"type\":\"textFile\"}]," +
                      "\"rows\":[[\"1\",\"a.txt\\nhello\"]]}";
        FakeHandler handler = new FakeHandler((_, _) => Json(HttpStatusCode.OK, json));

        Result<Table> result = await Make(handler).GetTableAsync("T");

        Assert.True(result.IsOk);
        Assert.IsType<TextFileType>(result.Value.Columns[1].Type);
        Assert.Equal("a.txt\nhello", result.Value.Rows[0][1]);
    }

    [Fact]
    public async Task AddRow_SendsValuesAsIs_IncludingTextFileFormat()
    {
        FakeHandler handler = new FakeHandler((_, _) => Json(HttpStatusCode.OK, "{\"ok\":true}"));

        await Make(handler).AddRowAsync("My table", new List<string> { "1", "n.txt\nпривіт" });

        Assert.Equal("POST", handler.LastMethod);
        Assert.Equal("http://srv:5080/api/tables/My%20table/rows", handler.LastUrl);
        Assert.Contains("\"values\"", handler.LastBody);
        Assert.Contains("n.txt\\n", handler.LastBody);
    }

    [Fact]
    public async Task SaveAndNewDatabase_TrackServerFileName()
    {
        FakeHandler handler = new FakeHandler((_, _) => Json(HttpStatusCode.OK, "{\"ok\":true}"));
        RemoteDataSource source = Make(handler);

        await source.SaveAsync("demo.json");
        Assert.Equal("demo.json", source.CurrentTarget);
        Assert.Equal("http://srv:5080/api/db/save", handler.LastUrl);

        await source.NewDatabaseAsync("X");
        Assert.Null(source.CurrentTarget);
    }

    [Fact]
    public async Task ServerWithoutDatabase_IsNormalState_NotConnectionError()
    {
        FakeHandler handler = new FakeHandler((_, _) => Json(HttpStatusCode.OK, "{\"exists\":false}"));
        RemoteDataSource source = Make(handler);

        Assert.True((await source.CheckConnectionAsync()).IsValid);

        Result<string?> name = await source.GetDatabaseNameAsync();
        Assert.True(name.IsOk);
        Assert.Null(name.Value);

        Result<List<string>> tables = await source.GetTableNamesAsync();
        Assert.True(tables.IsOk);
        Assert.Empty(tables.Value);
    }

    [Fact]
    public async Task ServerWithDatabase_ReturnsNameAndTables()
    {
        FakeHandler handler = new FakeHandler((_, _) =>
            Json(HttpStatusCode.OK, "{\"exists\":true,\"name\":\"U\",\"tables\":[{\"name\":\"A\"}]}"));
        RemoteDataSource source = Make(handler);

        Assert.Equal("U", (await source.GetDatabaseNameAsync()).Value);
        Assert.Equal("A", (await source.GetTableNamesAsync()).Value[0]);
    }

    [Fact]
    public async Task Conflict409_ShowsServerText()
    {
        FakeHandler handler = new FakeHandler((_, _) =>
            Json(HttpStatusCode.Conflict, "{\"error\":\"База не створена\"}"));

        ValidationResult result = await Make(handler).AddRowAsync("A", new List<string> { "1" });

        Assert.False(result.IsValid);
        Assert.Equal("База не створена", result.Error);
    }

    [Theory]
    [InlineData("http://localhost:5080", true)]
    [InlineData("localhost:5080/", true)]
    [InlineData("", false)]
    [InlineData("ftp://x", false)]
    [InlineData("http://", false)]
    public void TryNormalizeAddress_ChecksInput(string input, bool expected)
    {
        Assert.Equal(expected, RemoteDataSource.TryNormalizeAddress(input, out _, out _));
    }
}
