namespace TableDb.Core.FieldTypes;

public abstract class FieldType
{
    public abstract string Name { get; }

    public abstract string Hint { get; }

    public abstract bool Validate(string value);
}
