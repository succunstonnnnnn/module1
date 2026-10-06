using System.Globalization;

namespace TableDb.Core.FieldTypes;

public class IntegerInvlType : FieldType
{
    public override string Name => "integerInvl";
    public override string Hint => "Інтервал цілих: ліва..права, наприклад: 5..20 або -3..0";

    public override bool Validate(string value)
    {
        return TryParse(value, out int left, out int right) && left <= right;
    }

    public static int GetLeft(string value)
    {
        TryParse(value, out int left, out _);
        return left;
    }

    public static int GetRight(string value)
    {
        TryParse(value, out _, out int right);
        return right;
    }

    private static bool TryParse(string value, out int left, out int right)
    {
        left = 0;
        right = 0;
        if (string.IsNullOrEmpty(value))
            return false;

        int sep = value.IndexOf("..", StringComparison.Ordinal);
        if (sep < 0)
            return false;

        string leftPart = value.Substring(0, sep);
        string rightPart = value.Substring(sep + 2);

        return int.TryParse(leftPart, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out left)
            && int.TryParse(rightPart, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out right);
    }
}
