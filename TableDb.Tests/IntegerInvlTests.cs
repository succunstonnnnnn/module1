using TableDb.Core.FieldTypes;
using Xunit;

namespace TableDb.Tests;

public class IntegerInvlTests
{
    private readonly IntegerInvlType _type = new IntegerInvlType();

    [Theory]
    [InlineData("5..20")]
    [InlineData("-3..0")]
    [InlineData("7..7")]
    public void Validate_ValidIntervals_ReturnsTrue(string value)
    {
        Assert.True(_type.Validate(value));
    }

    [Theory]
    [InlineData("20..5")]
    [InlineData("5-20")]
    [InlineData("a..b")]
    [InlineData("5")]
    [InlineData("")]
    public void Validate_InvalidIntervals_ReturnsFalse(string value)
    {
        Assert.False(_type.Validate(value));
    }
}
