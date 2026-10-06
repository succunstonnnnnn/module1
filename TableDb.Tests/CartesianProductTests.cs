using TableDb.Core;
using TableDb.Core.FieldTypes;
using TableDb.Core.Operations;
using Xunit;

namespace TableDb.Tests;

public class CartesianProductTests
{
    private static Table BuildTable(string name, string[] columnNames, string[][] rows)
    {
        var table = new Table(name);
        foreach (var colName in columnNames)
            table.Columns.Add(new Column(colName, new StringType()));

        foreach (var row in rows)
            table.Rows.Add(new List<string>(row));

        return table;
    }

    [Fact]
    public void CartesianProduct_TwoByThree_GivesSixRowsAndSumOfColumns()
    {
        var tableA = BuildTable("A",
            columnNames: ["x", "y"],
            rows: [["a1", "a2"], ["b1", "b2"]]);

        var tableB = BuildTable("B",
            columnNames: ["z"],
            rows: [["r1"], ["r2"], ["r3"]]);

        var result = TableOperations.CartesianProduct(tableA, tableB, "Result");

        Assert.Equal(6, result.Rows.Count);
        Assert.Equal(3, result.Columns.Count);

        Assert.Equal(new List<string> { "a1", "a2", "r1" }, result.Rows[0]);
        Assert.Equal(new List<string> { "b1", "b2", "r3" }, result.Rows[5]);
    }

    [Fact]
    public void CartesianProduct_CollidingColumnNames_GetPrefixed()
    {
        var students = BuildTable("Students",
            columnNames: ["id", "name"],
            rows: [["1", "Alice"]]);

        var courses = BuildTable("Courses",
            columnNames: ["id", "title"],
            rows: [["101", "Math"]]);

        var result = TableOperations.CartesianProduct(students, courses, "Enrolled");

        var colNames = result.Columns.Select(c => c.Name).ToList();

        Assert.Contains("Students.id", colNames);
        Assert.Contains("Courses.id", colNames);
        Assert.Contains("name", colNames);
        Assert.Contains("title", colNames);
    }
}
