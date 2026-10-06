using System.Globalization;

namespace TableDb.Core.FieldTypes;

public class IntegerType : FieldType
{
    public override string Name => "integer";
    public override string Hint => "Ціле число, наприклад: 42 або -7";

    public override bool Validate(string value)
    {
        return int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
    }
}
