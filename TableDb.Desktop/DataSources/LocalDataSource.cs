using TableDb.Core;
using TableDb.Core.Controllers;

namespace TableDb.Desktop.DataSources;

public class LocalDataSource : IDataSource
{
    private readonly DatabaseController _db = new DatabaseController();

    public bool IsRemote => false;

    public string? CurrentTarget { get; private set; }

    public string Description
    {
        get
        {
            string file = CurrentTarget == null ? "(не збережено)" : Path.GetFileName(CurrentTarget);
            return "Локальний файл: " + file;
        }
    }

    public Task<ValidationResult> NewDatabaseAsync(string name)
    {
        ValidationResult result = _db.NewDatabase(name);
        if (result.IsValid)
            CurrentTarget = null;
        return Task.FromResult(result);
    }

    public Task<Result<string?>> GetDatabaseNameAsync()
    {
        return Task.FromResult(Result<string?>.Ok(_db.DatabaseName));
    }

    public Task<Result<List<string>>> GetTableNamesAsync()
    {
        return Task.FromResult(Result<List<string>>.Ok(_db.GetTableNames()));
    }

    public Task<Result<Table>> GetTableAsync(string name)
    {
        Table? table = _db.GetTable(name);
        if (table == null)
            return Task.FromResult(Result<Table>.Fail($"Таблиця '{name}' не існує"));
        return Task.FromResult(Result<Table>.Ok(table));
    }

    public Task<ValidationResult> CreateTableAsync(string name, List<Column> columns)
    {
        return Task.FromResult(_db.CreateTable(name, columns));
    }

    public Task<ValidationResult> DropTableAsync(string name)
    {
        return Task.FromResult(_db.DropTable(name));
    }

    public Task<ValidationResult> AddRowAsync(string tableName, List<string> values)
    {
        return Task.FromResult(_db.Rows.AddRow(tableName, values));
    }

    public Task<ValidationResult> EditRowAsync(string tableName, int rowIndex, List<string> values)
    {
        return Task.FromResult(_db.Rows.EditRow(tableName, rowIndex, values));
    }

    public Task<ValidationResult> DeleteRowAsync(string tableName, int rowIndex)
    {
        return Task.FromResult(_db.Rows.DeleteRow(tableName, rowIndex));
    }

    public Task<ValidationResult> CartesianProductAsync(string tableAName, string tableBName, string resultName)
    {
        return Task.FromResult(_db.Product.CartesianProduct(tableAName, tableBName, resultName));
    }

    public Task<ValidationResult> SaveAsync(string target)
    {
        ValidationResult result = _db.Save(target);
        if (result.IsValid)
            CurrentTarget = target;
        return Task.FromResult(result);
    }

    public Task<ValidationResult> LoadAsync(string target)
    {
        ValidationResult result = _db.Load(target);
        if (result.IsValid)
            CurrentTarget = target;
        return Task.FromResult(result);
    }
}
