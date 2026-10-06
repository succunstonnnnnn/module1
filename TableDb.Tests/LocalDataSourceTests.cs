using TableDb.Core;
using TableDb.Core.FieldTypes;
using TableDb.Desktop.DataSources;
using Xunit;

namespace TableDb.Tests;

public class LocalDataSourceTests
{
    [Fact]
    public async Task FullScenario_CreateRowsProductSaveLoad_KeepsData()
    {
        LocalDataSource source = new LocalDataSource();
        await source.NewDatabaseAsync("Univ");
        await source.CreateTableAsync("A", new List<Column> { new Column("id", new IntegerType()) });
        await source.CreateTableAsync("B", new List<Column> { new Column("name", new StringType()) });
        await source.AddRowAsync("A", new List<string> { "1" });
        await source.AddRowAsync("A", new List<string> { "2" });
        await source.AddRowAsync("B", new List<string> { "x" });
        await source.AddRowAsync("B", new List<string> { "y" });
        await source.AddRowAsync("B", new List<string> { "z" });

        ValidationResult product = await source.CartesianProductAsync("A", "B", "P");
        Assert.True(product.IsValid);
        Assert.Equal(6, (await source.GetTableAsync("P")).Value.Rows.Count);

        string path = Path.Combine(Path.GetTempPath(), $"tabledb_ds_{Guid.NewGuid()}.json");
        try
        {
            Assert.True((await source.SaveAsync(path)).IsValid);
            Assert.Equal(path, source.CurrentTarget);

            LocalDataSource reopened = new LocalDataSource();
            Assert.True((await reopened.LoadAsync(path)).IsValid);

            Assert.Equal("Univ", (await reopened.GetDatabaseNameAsync()).Value);
            Assert.Equal(3, (await reopened.GetTableNamesAsync()).Value.Count);
            Assert.Equal(6, (await reopened.GetTableAsync("P")).Value.Rows.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task InvalidRow_ReturnsCoreValidationText()
    {
        LocalDataSource source = new LocalDataSource();
        await source.NewDatabaseAsync("T");
        await source.CreateTableAsync("A", new List<Column> { new Column("id", new IntegerType()) });

        ValidationResult result = await source.AddRowAsync("A", new List<string> { "abc" });

        Assert.False(result.IsValid);
        Assert.Contains("Поле 'id'", result.Error);
    }

    [Fact]
    public async Task Description_ShowsFileNameAfterSave()
    {
        LocalDataSource source = new LocalDataSource();
        Assert.Equal("Локальний файл: (не збережено)", source.Description);
        await source.NewDatabaseAsync("T");

        string path = Path.Combine(Path.GetTempPath(), $"mydb_{Guid.NewGuid()}.json");
        try
        {
            await source.SaveAsync(path);
            Assert.Equal("Локальний файл: " + Path.GetFileName(path), source.Description);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task AtStart_NoDatabase_AndOperationsFailInsteadOfCreatingOne()
    {
        LocalDataSource source = new LocalDataSource();

        Assert.Null((await source.GetDatabaseNameAsync()).Value);
        Assert.Empty((await source.GetTableNamesAsync()).Value);
        Assert.False((await source.CreateTableAsync("A", new List<Column> { new Column("id", new IntegerType()) })).IsValid);
        Assert.False((await source.AddRowAsync("A", new List<string> { "1" })).IsValid);
        Assert.False((await source.SaveAsync(Path.Combine(Path.GetTempPath(), "never.json"))).IsValid);
        Assert.Null((await source.GetDatabaseNameAsync()).Value);
    }
}
