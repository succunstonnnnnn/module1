using TableDb.Core.FieldTypes;

namespace TableDb.Core;

public class Column
{
    public string Name { get; set; }
    public FieldType Type { get; set; }

    public Column(string name, FieldType type)
    {
        Name = name;
        Type = type;
    }
}
