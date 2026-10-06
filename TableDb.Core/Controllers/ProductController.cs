using TableDb.Core.Operations;

namespace TableDb.Core.Controllers;

public class ProductController
{
    private readonly Database? _database;

    public ProductController(Database? database)
    {
        _database = database;
    }

    public ValidationResult CartesianProduct(string tableAName, string tableBName, string resultName)
    {
        if (_database == null)
            return ValidationResult.Fail(DatabaseController.NoDatabaseMessage);

        if (string.IsNullOrWhiteSpace(tableAName) || string.IsNullOrWhiteSpace(tableBName))
            return ValidationResult.Fail("Оберіть обидві таблиці");

        if (tableAName == tableBName)
            return ValidationResult.Fail("Оберіть дві різні таблиці");

        Table? a = _database.GetTable(tableAName);
        if (a == null)
            return ValidationResult.Fail($"Таблиця '{tableAName}' не існує");

        Table? b = _database.GetTable(tableBName);
        if (b == null)
            return ValidationResult.Fail($"Таблиця '{tableBName}' не існує");

        if (string.IsNullOrWhiteSpace(resultName))
            return ValidationResult.Fail("Назва результату не може бути порожньою");

        if (_database.GetTable(resultName) != null)
            return ValidationResult.Fail($"Таблиця '{resultName}' вже існує");

        Table product = TableOperations.CartesianProduct(a, b, resultName);

        for (int i = 0; i < product.Columns.Count; i++)
        {
            for (int j = i + 1; j < product.Columns.Count; j++)
            {
                if (product.Columns[i].Name == product.Columns[j].Name)
                    return ValidationResult.Fail(
                        $"У результаті дві колонки з назвою '{product.Columns[i].Name}'");
            }
        }

        _database.Tables.Add(product);
        return ValidationResult.Ok();
    }
}
