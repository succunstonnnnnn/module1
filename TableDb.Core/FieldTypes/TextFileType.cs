using System.Text;

namespace TableDb.Core.FieldTypes;

public class TextFileType : FieldType
{
    public override string Name => "textFile";
    public override string Hint => "Текстовий файл: оберіть текстовий файл у кодуванні UTF-8 (зберігається його ім'я та вміст)";

    public override bool Validate(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        if (GetFileName(value).Trim().Length == 0)
            return false;

        return IsText(value);
    }

    private static bool IsText(string value)
    {
        foreach (char c in value)
        {
            if (c == '\uFFFD')
                return false;

            if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                return false;
        }
        return true;
    }

    public static string GetFileName(string value)
    {
        int idx = value.IndexOf('\n');
        return idx >= 0 ? value.Substring(0, idx) : value;
    }

    public static string GetContent(string value)
    {
        int idx = value.IndexOf('\n');
        return idx >= 0 ? value.Substring(idx + 1) : string.Empty;
    }

    private const string NotTextMessage = "Файл не є текстовим: оберіть файл у кодуванні UTF-8";

    public static string FromFile(string path)
    {
        string fileName = Path.GetFileName(path);

        UTF8Encoding strictUtf8 = new UTF8Encoding(false, true);
        string content;
        try
        {
            content = File.ReadAllText(path, strictUtf8);
        }
        catch (DecoderFallbackException)
        {
            throw new InvalidDataException(NotTextMessage);
        }

        string value = fileName + "\n" + content.Replace("\r\n", "\n");

        if (!new TextFileType().Validate(value))
            throw new InvalidDataException(NotTextMessage);

        return value;
    }
}
