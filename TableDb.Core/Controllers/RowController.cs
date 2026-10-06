namespace TableDb.Core.Controllers;

public class RowController
{
    private readonly Database? _database;

    public RowController(Database? database)
    {
        _database = database;
    }

    public ValidationResult AddRow(string tableName, List<string> values)
    {
        if (_database == null)
            return ValidationResult.Fail(DatabaseController.NoDatabaseMessage);

        Table? table = _database.GetTable(tableName);
        if (table == null)
            return NotFound(tableName);
        return table.AddRow(values);
    }

    public ValidationResult EditRow(string tableName, int rowIndex, List<string> values)
    {
        if (_database == null)
            return ValidationResult.Fail(DatabaseController.NoDatabaseMessage);

        Table? table = _database.GetTable(tableName);
        if (table == null)
            return NotFound(tableName);
        return table.EditRow(rowIndex, values);
    }

    public ValidationResult DeleteRow(string tableName, int rowIndex)
    {
        if (_database == null)
            return ValidationResult.Fail(DatabaseController.NoDatabaseMessage);

        Table? table = _database.GetTable(tableName);
        if (table == null)
            return NotFound(tableName);
        return table.DeleteRow(rowIndex);
    }

    private static ValidationResult NotFound(string tableName)
    {
        return ValidationResult.Fail($"Таблиця '{tableName}' не існує");
    }
}
