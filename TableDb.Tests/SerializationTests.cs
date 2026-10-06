using TableDb.Core;
using TableDb.Core.FieldTypes;
using Xunit;

namespace TableDb.Tests;

public class SerializationTests
{
    [Fact]
    public void SaveAndLoad_PreservesSchemaAndData()
    {
        var db = new Database("TestDb");
        var columns = new List<Column>
        {
            new Column("id", new IntegerType()),
            new Column("range", new IntegerInvlType()),
            new Column("label", new StringType())
        };
        db.CreateTable("Items", columns);

        var table = db.GetTable("Items")!;
        table.AddRow(new List<string> { "1", "5..10", "low" });
        table.AddRow(new List<string> { "2", "-3..0", "neg" });

        string path = Path.Combine(Path.GetTempPath(), $"tabledb_test_{Guid.NewGuid()}.json");
        try
        {
            db.SaveToFile(path);
            var loaded = Database.LoadFromFile(path);

            Assert.Equal("TestDb", loaded.Name);
            Assert.Single(loaded.Tables);

            var loadedTable = loaded.Tables[0];
            Assert.Equal("Items", loadedTable.Name);
            Assert.Equal(3, loadedTable.Columns.Count);

            Assert.IsType<IntegerInvlType>(loadedTable.Columns[1].Type);
            Assert.Equal("range", loadedTable.Columns[1].Name);

            Assert.Equal(2, loadedTable.Rows.Count);
            Assert.Equal("5..10", loadedTable.Rows[0][1]);
            Assert.Equal("-3..0", loadedTable.Rows[1][1]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
