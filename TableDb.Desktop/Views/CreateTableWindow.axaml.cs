using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using TableDb.Core;
using TableDb.Core.FieldTypes;

namespace TableDb.Desktop.Views;

public class ColumnEditor
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = "string";
}

public partial class CreateTableWindow : Window
{
    private readonly ObservableCollection<ColumnEditor> _columns = new ObservableCollection<ColumnEditor>();
    private readonly Func<string, List<Column>, Task<ValidationResult>>? _submit;

    public string[] TypeNames { get; } = FieldTypeFactory.AllTypeNames();

    public bool Confirmed { get; private set; }

    public string CreatedTableName { get; private set; } = string.Empty;

    public CreateTableWindow()
    {
        InitializeComponent();
        ColumnsList.ItemsSource = _columns;
        _columns.Add(new ColumnEditor());
    }

    public CreateTableWindow(Func<string, List<Column>, Task<ValidationResult>> submit) : this()
    {
        _submit = submit;
    }

    private void OnAddColumn(object? sender, RoutedEventArgs e)
    {
        _columns.Add(new ColumnEditor());
    }

    private void OnRemoveColumn(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ColumnEditor editor)
            _columns.Remove(editor);
    }

    private async void OnCreate(object? sender, RoutedEventArgs e)
    {
        List<Column> columns = new List<Column>();
        foreach (ColumnEditor editor in _columns)
            columns.Add(new Column(editor.Name.Trim(), FieldTypeFactory.Create(editor.TypeName)));

        string tableName = (TableNameBox.Text ?? string.Empty).Trim();
        OkButton.IsEnabled = false;
        ValidationResult result;
        try
        {
            result = await _submit!(tableName, columns);
        }
        finally
        {
            OkButton.IsEnabled = true;
        }
        if (!result.IsValid)
        {
            ErrorText.Text = result.Error;
            return;
        }

        CreatedTableName = tableName;
        Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
