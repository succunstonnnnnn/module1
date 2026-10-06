namespace TableDb.Core.FieldTypes;

public class StringType : FieldType
{
    public override string Name => "string";
    public override string Hint => "Довільний рядок тексту";

    public override bool Validate(string value)
    {
        return true;
    }
}
