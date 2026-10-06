using TableDb.Core.FieldTypes;
using Xunit;

namespace TableDb.Tests;

public class FieldTypeTests
{
    [Fact]
    public void TextFile_SplitsNameAndContent()
    {
        string value = "notes.txt\nрядок 1\nрядок 2";

        Assert.True(new TextFileType().Validate(value));
        Assert.Equal("notes.txt", TextFileType.GetFileName(value));
        Assert.Equal("рядок 1\nрядок 2", TextFileType.GetContent(value));
    }

    [Fact]
    public void TextFile_FromFile_RoundTripsAndNormalizesLineEndings()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "a\r\nb");
        try
        {
            string value = TextFileType.FromFile(path);

            Assert.Equal(Path.GetFileName(path), TextFileType.GetFileName(value));
            Assert.Equal("a\nb", TextFileType.GetContent(value));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("\nтільки вміст")]
    public void TextFile_WithoutFileName_IsInvalid(string value)
    {
        Assert.False(new TextFileType().Validate(value));
    }

    [Fact]
    public void TextFile_ValueWithNullByte_IsInvalid()
    {
        Assert.False(new TextFileType().Validate("a.txt\nab\0cd"));
    }

    [Fact]
    public void TextFile_ValueWithReplacementChar_IsInvalid()
    {
        Assert.False(new TextFileType().Validate("a.txt\nab\uFFFDcd"));
    }

    [Fact]
    public void TextFile_OtherControlChars_AreInvalid()
    {
        Assert.False(new TextFileType().Validate("a.txt\nab\u0001cd"));
        Assert.False(new TextFileType().Validate("a.txt\nab\u007fcd"));
    }

    [Fact]
    public void TextFile_PlainTextWithNewlinesAndTabs_IsValid()
    {
        Assert.True(new TextFileType().Validate("a.txt\nрядок 1\r\nрядок\t2\n"));
    }

    [Fact]
    public void TextFile_FromFile_BinaryFile_ThrowsUkrainianMessage()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_bin_{Guid.NewGuid()}.png");
        File.WriteAllBytes(path, new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0xFF, 0xFE, 0x00 });
        try
        {
            InvalidDataException ex = Assert.Throws<InvalidDataException>(() => TextFileType.FromFile(path));

            Assert.Contains("Файл не є текстовим", ex.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TextFile_FromFile_ValidUtf8WithNullByte_Throws()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_nul_{Guid.NewGuid()}.txt");
        File.WriteAllBytes(path, new byte[] { 0x61, 0x00, 0x62 });
        try
        {
            Assert.Throws<InvalidDataException>(() => TextFileType.FromFile(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void IntegerInvl_GetLeftAndRight()
    {
        Assert.Equal(-3, IntegerInvlType.GetLeft("-3..0"));
        Assert.Equal(0, IntegerInvlType.GetRight("-3..0"));
    }

    [Theory]
    [InlineData("a", true)]
    [InlineData("A", true)]
    [InlineData("AB", false)]
    [InlineData("", false)]
    public void Char_AcceptsExactlyOneCharacter(string value, bool expected)
    {
        Assert.Equal(expected, new CharType().Validate(value));
    }

    [Theory]
    [InlineData("3.14", true)]
    [InlineData("-0.5", true)]
    [InlineData("3,14", false)]
    [InlineData("abc", false)]
    public void Real_UsesDotAsSeparator(string value, bool expected)
    {
        Assert.Equal(expected, new RealType().Validate(value));
    }
}
