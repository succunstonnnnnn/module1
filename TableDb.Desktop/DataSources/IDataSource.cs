using TableDb.Core;

namespace TableDb.Desktop.DataSources;

public interface IDataSource
{
    bool IsRemote { get; }

    string Description { get; }

    string? CurrentTarget { get; }

    Task<ValidationResult> NewDatabaseAsync(string name);

    Task<Result<string?>> GetDatabaseNameAsync();

    Task<Result<List<string>>> GetTableNamesAsync();

    Task<Result<Table>> GetTableAsync(string name);

    Task<ValidationResult> CreateTableAsync(string name, List<Column> columns);
    Task<ValidationResult> DropTableAsync(string name);

    Task<ValidationResult> AddRowAsync(string tableName, List<string> values);
    Task<ValidationResult> EditRowAsync(string tableName, int rowIndex, List<string> values);
    Task<ValidationResult> DeleteRowAsync(string tableName, int rowIndex);

    Task<ValidationResult> CartesianProductAsync(string tableAName, string tableBName, string resultName);

    Task<ValidationResult> SaveAsync(string target);

    Task<ValidationResult> LoadAsync(string target);
}
