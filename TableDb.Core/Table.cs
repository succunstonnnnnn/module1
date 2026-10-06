namespace TableDb.Core;

public class Table
{
    public string Name { get; set; }
    public List<Column> Columns { get; set; }
    public List<List<string>> Rows { get; set; }

    public Table(string name)
    {
        Name = name;
        Columns = new List<Column>();
        Rows = new List<List<string>>();
    }

    public int IndexOfColumn(string columnName)
    {
        for (int i = 0; i < Columns.Count; i++)
        {
            if (Columns[i].Name == columnName)
                return i;
        }
        return -1;
    }

    public ValidationResult ValidateRow(List<string> values)
    {
        if (values.Count != Columns.Count)
            return ValidationResult.Fail(
                $"Очікується {Columns.Count} значень, отримано {values.Count}");

        for (int i = 0; i < Columns.Count; i++)
        {
            string value = values[i] ?? string.Empty;
            if (!Columns[i].Type.Validate(value))
                return ValidationResult.Fail(
                    $"Поле '{Columns[i].Name}' ({Columns[i].Type.Name}): некоректне значення. {Columns[i].Type.Hint}");
        }

        return ValidationResult.Ok();
    }

    public ValidationResult AddRow(List<string> values)
    {
        ValidationResult check = ValidateRow(values);
        if (!check.IsValid)
            return check;

        Rows.Add(new List<string>(values));
        return ValidationResult.Ok();
    }

    public ValidationResult EditRow(int rowIndex, List<string> values)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
            return ValidationResult.Fail($"Рядок з номером {rowIndex} не існує");

        ValidationResult check = ValidateRow(values);
        if (!check.IsValid)
            return check;

        Rows[rowIndex] = new List<string>(values);
        return ValidationResult.Ok();
    }

    public ValidationResult DeleteRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= Rows.Count)
            return ValidationResult.Fail($"Рядок з номером {rowIndex} не існує");

        Rows.RemoveAt(rowIndex);
        return ValidationResult.Ok();
    }
}
