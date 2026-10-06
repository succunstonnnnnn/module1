namespace TableDb.Core.FieldTypes;

public class CharType : FieldType
{
    public override string Name => "char";
    public override string Hint => "Один символ, наприклад: A";

    public override bool Validate(string value)
    {
        return value.Length == 1;
    }
}
