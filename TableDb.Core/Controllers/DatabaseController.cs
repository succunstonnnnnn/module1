namespace TableDb.Core.Controllers;

public class DatabaseController
{
    public const string NoDatabaseMessage = "База не створена";

    private Database? _database;

    public RowController Rows { get; private set; }
    public ProductController Product { get; private set; }

    public DatabaseController()
    {
        Rows = new RowController(null);
        Product = new ProductController(null);
    }

    public bool HasDatabase => _database != null;

    public string? DatabaseName => _database?.Name;

    public List<string> GetTableNames()
    {
        List<string> names = new List<string>();
        if (_database == null)
            return names;

        foreach (Table table in _database.Tables)
            names.Add(table.Name);
        return names;
    }

    public Table? GetTable(string name) => _database?.GetTable(name);

    public ValidationResult NewDatabase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Fail("Назва бази не може бути порожньою");

        Replace(new Database(name));
        return ValidationResult.Ok();
    }

    public ValidationResult CreateTable(string name, List<Column> columns)
    {
        if (_database == null)
            return ValidationResult.Fail(NoDatabaseMessage);
        return _database.CreateTable(name, columns);
    }

    public ValidationResult DropTable(string name)
    {
        if (_database == null)
            return ValidationResult.Fail(NoDatabaseMessage);
        return _database.DropTable(name);
    }

    public ValidationResult Save(string path)
    {
        if (_database == null)
            return ValidationResult.Fail(NoDatabaseMessage);

        try
        {
            _database.SaveToFile(path);
            return ValidationResult.Ok();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail("Не вдалося зберегти: " + ex.Message);
        }
    }

    public ValidationResult Load(string path)
    {
        try
        {
            Replace(Database.LoadFromFile(path));
            return ValidationResult.Ok();
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail("Не вдалося відкрити: " + ex.Message);
        }
    }

    private void Replace(Database database)
    {
        _database = database;
        Rows = new RowController(database);
        Product = new ProductController(database);
    }
}
