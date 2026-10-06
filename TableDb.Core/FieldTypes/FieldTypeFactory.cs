namespace TableDb.Core.FieldTypes;

public static class FieldTypeFactory
{
    public static FieldType Create(string typeName)
    {
        return typeName switch
        {
            "integer"     => new IntegerType(),
            "real"        => new RealType(),
            "char"        => new CharType(),
            "string"      => new StringType(),
            "textFile"    => new TextFileType(),
            "integerInvl" => new IntegerInvlType(),
            _ => throw new ArgumentException($"Невідомий тип поля: '{typeName}'")
        };
    }

    public static string[] AllTypeNames()
    {
        return ["integer", "real", "char", "string", "textFile", "integerInvl"];
    }
}
