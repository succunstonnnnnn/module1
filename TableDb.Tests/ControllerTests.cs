using TableDb.Core;
using TableDb.Core.Controllers;
using TableDb.Core.FieldTypes;
using Xunit;

namespace TableDb.Tests;

public class ControllerTests
{
    private static DatabaseController BuildDb()
    {
        DatabaseController db = new DatabaseController();
        db.NewDatabase("Test");
        db.CreateTable("A", new List<Column> { new Column("id", new IntegerType()) });
        db.CreateTable("B", new List<Column> { new Column("name", new StringType()) });
        db.Rows.AddRow("A", new List<string> { "1" });
        db.Rows.AddRow("B", new List<string> { "x" });
        return db;
    }

    [Fact]
    public void Product_SameTableTwice_IsRejected()
    {
        DatabaseController db = BuildDb();

        ValidationResult result = db.Product.CartesianProduct("A", "A", "R");

        Assert.False(result.IsValid);
        Assert.Null(db.GetTable("R"));
    }

    [Fact]
    public void Product_ResultNameAlreadyUsed_IsRejected()
    {
        DatabaseController db = BuildDb();

        ValidationResult result = db.Product.CartesianProduct("A", "B", "A");

        Assert.False(result.IsValid);
        Assert.Equal(2, db.GetTableNames().Count);
    }

    [Fact]
    public void Product_Valid_AddsResultTable()
    {
        DatabaseController db = BuildDb();

        ValidationResult result = db.Product.CartesianProduct("A", "B", "R");

        Assert.True(result.IsValid);
        Assert.Single(db.GetTable("R")!.Rows);
    }

    [Fact]
    public void RowController_UnknownTable_ReturnsError()
    {
        DatabaseController db = BuildDb();

        Assert.False(db.Rows.AddRow("Nope", new List<string> { "1" }).IsValid);
    }

    [Fact]
    public void Load_BrokenFile_KeepsCurrentDatabase()
    {
        DatabaseController db = BuildDb();
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_bad_{Guid.NewGuid()}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            ValidationResult result = db.Load(path);

            Assert.False(result.IsValid);
            Assert.Equal(2, db.GetTableNames().Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void NewController_HasNoDatabase()
    {
        DatabaseController db = new DatabaseController();

        Assert.False(db.HasDatabase);
        Assert.Null(db.DatabaseName);
        Assert.Empty(db.GetTableNames());
    }

    [Fact]
    public void OperationsWithoutDatabase_ReturnError_AndDoNotCreateDatabase()
    {
        DatabaseController db = new DatabaseController();
        List<Column> columns = new List<Column> { new Column("id", new IntegerType()) };
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_nodb_{Guid.NewGuid()}.json");

        ValidationResult[] results =
        {
            db.CreateTable("A", columns),
            db.DropTable("A"),
            db.Rows.AddRow("A", new List<string> { "1" }),
            db.Rows.EditRow("A", 0, new List<string> { "1" }),
            db.Rows.DeleteRow("A", 0),
            db.Product.CartesianProduct("A", "B", "R"),
            db.Save(path)
        };

        foreach (ValidationResult result in results)
        {
            Assert.False(result.IsValid);
            Assert.Equal(DatabaseController.NoDatabaseMessage, result.Error);
        }
        Assert.False(db.HasDatabase);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void NewDatabase_MakesDatabaseAvailable()
    {
        DatabaseController db = new DatabaseController();

        Assert.True(db.NewDatabase("X").IsValid);

        Assert.True(db.HasDatabase);
        Assert.Equal("X", db.DatabaseName);
        Assert.True(db.CreateTable("A", new List<Column> { new Column("id", new IntegerType()) }).IsValid);
    }

    [Fact]
    public void Load_IntoEmptyController_CreatesDatabase()
    {
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_load_{Guid.NewGuid()}.json");
        DatabaseController source = BuildDb();
        source.Save(path);
        try
        {
            DatabaseController db = new DatabaseController();

            Assert.True(db.Load(path).IsValid);

            Assert.True(db.HasDatabase);
            Assert.Equal(2, db.GetTableNames().Count);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_OldFileWithBinaryInTextFileCell_ReportsTableAndColumn()
    {
        string json = "{\"Name\":\"Old\",\"Tables\":[{\"Name\":\"Docs\",\"Columns\":[" +
                      "{\"Name\":\"id\",\"TypeName\":\"integer\"},{\"Name\":\"scan\",\"TypeName\":\"textFile\"}]," +
                      "\"Rows\":[[\"1\",\"Screenshot.png\\n\uFFFDPNG\\r\\n\\u001a\\n\\u0000\"]]}]}";
        string path = Path.Combine(Path.GetTempPath(), $"tabledb_old_{Guid.NewGuid()}.json");
        File.WriteAllText(path, json);
        try
        {
            DatabaseController db = new DatabaseController();

            ValidationResult result = db.Load(path);

            Assert.False(result.IsValid);
            Assert.Contains("Таблиця 'Docs'", result.Error);
            Assert.Contains("Поле 'scan'", result.Error);
            Assert.False(db.HasDatabase);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
