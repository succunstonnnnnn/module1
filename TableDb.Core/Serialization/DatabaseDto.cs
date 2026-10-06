namespace TableDb.Core.Serialization;

internal class DatabaseDto
{
    public string Name { get; set; } = string.Empty;
    public List<TableDto> Tables { get; set; } = new();
}

internal class TableDto
{
    public string Name { get; set; } = string.Empty;
    public List<ColumnDto> Columns { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

internal class ColumnDto
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}
