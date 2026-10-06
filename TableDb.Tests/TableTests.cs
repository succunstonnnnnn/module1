using TableDb.Core;
using TableDb.Core.FieldTypes;
using Xunit;

namespace TableDb.Tests;

public class TableTests
{
    [Fact]
    public void AddRow_WrongType_RejectsAndDoesNotAddRow()
    {
        var table = new Table("Test");
        table.Columns.Add(new Column("age", new IntegerType()));
        table.Columns.Add(new Column("name", new StringType()));

        var result = table.AddRow(new List<string> { "hello", "Alice" });

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Error);
        Assert.Empty(table.Rows);
    }
}
