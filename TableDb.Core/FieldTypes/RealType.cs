using System.Globalization;

namespace TableDb.Core.FieldTypes;

public class RealType : FieldType
{
    public override string Name => "real";
    public override string Hint => "Дійсне число, наприклад: 3.14 або -0.5 (крапка як роздільник)";

    public override bool Validate(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }
}
