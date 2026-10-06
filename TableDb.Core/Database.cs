using System.Text.Json;
using TableDb.Core.FieldTypes;
using TableDb.Core.Serialization;

namespace TableDb.Core;

public class Database
{
    public string Name { get; set; }
    public List<Table> Tables { get; set; }

    public Database(string name)
    {
        Name = name;
        Tables = new List<Table>();
    }

    public ValidationResult CreateTable(string name, List<Column> columns)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Fail("Назва таблиці не може бути порожньою");

        if (GetTable(name) != null)
            return ValidationResult.Fail($"Таблиця '{name}' вже існує");

        if (columns.Count == 0)
            return ValidationResult.Fail("Таблиця повинна мати хоча б одну колонку");

        List<string> seen = new List<string>();
        foreach (Column column in columns)
        {
            if (string.IsNullOrWhiteSpace(column.Name))
                return ValidationResult.Fail("Назва колонки не може бути порожньою");
            if (seen.Contains(column.Name))
                return ValidationResult.Fail($"Колонка '{column.Name}' вказана двічі");
            seen.Add(column.Name);
        }

        Table table = new Table(name);
        table.Columns.AddRange(columns);
        Tables.Add(table);
        return ValidationResult.Ok();
    }

    public ValidationResult DropTable(string name)
    {
        Table? table = GetTable(name);
        if (table == null)
            return ValidationResult.Fail($"Таблиця '{name}' не існує");

        Tables.Remove(table);
        return ValidationResult.Ok();
    }

    public Table? GetTable(string name)
    {
        foreach (Table table in Tables)
        {
            if (table.Name == name)
                return table;
        }
        return null;
    }

    public void SaveToFile(string filePath)
    {
        DatabaseDto dto = new DatabaseDto { Name = Name };

        foreach (Table table in Tables)
        {
            TableDto tableDto = new TableDto { Name = table.Name };
            foreach (Column column in table.Columns)
                tableDto.Columns.Add(new ColumnDto { Name = column.Name, TypeName = column.Type.Name });
            foreach (List<string> row in table.Rows)
                tableDto.Rows.Add(new List<string>(row));
            dto.Tables.Add(tableDto);
        }

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        File.WriteAllText(filePath, JsonSerializer.Serialize(dto, options));
    }

    public static Database LoadFromFile(string filePath)
    {
        string json = File.ReadAllText(filePath);
        DatabaseDto? dto = JsonSerializer.Deserialize<DatabaseDto>(json);
        if (dto == null)
            throw new InvalidDataException("Файл бази порожній або пошкоджений");

        Database database = new Database(dto.Name);

        foreach (TableDto tableDto in dto.Tables)
        {
            List<Column> columns = new List<Column>();
            foreach (ColumnDto columnDto in tableDto.Columns)
            {
                FieldType type;
                try
                {
                    type = FieldTypeFactory.Create(columnDto.TypeName);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidDataException(ex.Message);
                }
                columns.Add(new Column(columnDto.Name, type));
            }

            ValidationResult created = database.CreateTable(tableDto.Name, columns);
            if (!created.IsValid)
                throw new InvalidDataException($"Таблиця '{tableDto.Name}': {created.Error}");

            Table table = database.GetTable(tableDto.Name)!;
            for (int i = 0; i < tableDto.Rows.Count; i++)
            {
                ValidationResult added = table.AddRow(tableDto.Rows[i]);
                if (!added.IsValid)
                    throw new InvalidDataException(
                        $"Таблиця '{tableDto.Name}', рядок {i + 1}: {added.Error}");
            }
        }

        return database;
    }
}
