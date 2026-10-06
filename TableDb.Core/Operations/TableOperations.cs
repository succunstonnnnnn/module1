namespace TableDb.Core.Operations;

public static class TableOperations
{
    public static Table CartesianProduct(Table a, Table b, string resultName)
    {
        Table result = new Table(resultName);

        foreach (Column column in a.Columns)
        {
            string name = b.IndexOfColumn(column.Name) >= 0 ? a.Name + "." + column.Name : column.Name;
            result.Columns.Add(new Column(name, column.Type));
        }

        foreach (Column column in b.Columns)
        {
            string name = a.IndexOfColumn(column.Name) >= 0 ? b.Name + "." + column.Name : column.Name;
            result.Columns.Add(new Column(name, column.Type));
        }

        foreach (List<string> rowA in a.Rows)
        {
            foreach (List<string> rowB in b.Rows)
            {
                List<string> row = new List<string>(rowA);
                row.AddRange(rowB);
                result.Rows.Add(row);
            }
        }

        return result;
    }
}
