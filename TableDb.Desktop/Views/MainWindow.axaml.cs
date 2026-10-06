using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TableDb.Core;
using TableDb.Core.FieldTypes;
using TableDb.Desktop.DataSources;

namespace TableDb.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly IDataSource _source;

    private bool _isDirty;
    private bool _closeConfirmed;
    private bool _isRefreshing;

    private string? _databaseName;
    private string? _currentTableName;
    private Table? _currentTable;

    public MainWindow() : this(new LocalDataSource())
    {
    }

    public MainWindow(IDataSource source)
    {
        _source = source;
        InitializeComponent();
        Opened += async (_, _) => await RefreshAllAsync();
    }

    private void SetDirty(bool value)
    {
        _isDirty = value;
        UpdateStatus();
    }

    private async Task ShowErrorAsync(string message)
    {
        await new MessageWindow("Помилка", message).ShowDialog(this);
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        await new MessageWindow(title, message).ShowDialog(this);
    }

    private async Task RefreshAllAsync()
    {
        Result<string?> name = await _source.GetDatabaseNameAsync();
        Result<List<string>> names = await _source.GetTableNamesAsync();
        if (!name.IsOk || !names.IsOk)
        {
            UpdateStatus();
            await ShowErrorAsync(name.IsOk ? names.Error : name.Error);
            return;
        }

        _databaseName = name.Value;

        _isRefreshing = true;
        TablesList.ItemsSource = names.Value;
        if (_currentTableName != null && names.Value.Contains(_currentTableName))
            TablesList.SelectedItem = _currentTableName;
        else
            _currentTableName = null;
        _isRefreshing = false;

        await ShowTableAsync();
        UpdateStatus();
    }

    private async Task ShowTableAsync()
    {
        RowsGrid.Columns.Clear();
        RowsGrid.ItemsSource = null;
        _currentTable = null;

        if (_currentTableName == null)
            return;

        Result<Table> result = await _source.GetTableAsync(_currentTableName);
        if (!result.IsOk)
        {
            await ShowErrorAsync(result.Error);
            return;
        }

        Table table = result.Value;
        _currentTable = table;

        for (int i = 0; i < table.Columns.Count; i++)
        {
            RowsGrid.Columns.Add(new DataGridTextColumn
            {
                Header = table.Columns[i].Name + " (" + table.Columns[i].Type.Name + ")",
                Binding = new Binding("[" + i + "]")
            });
        }

        List<List<string>> displayRows = new List<List<string>>();
        foreach (List<string> row in table.Rows)
        {
            List<string> displayRow = new List<string>();
            for (int i = 0; i < row.Count; i++)
                displayRow.Add(ToDisplayValue(table.Columns[i].Type, row[i]));
            displayRows.Add(displayRow);
        }
        RowsGrid.ItemsSource = displayRows;
    }

    private static string ToDisplayValue(FieldType type, string value)
    {
        if (type is TextFileType)
            return TextFileType.GetFileName(value) + " (текстовий файл)";
        return value;
    }

    private void UpdateStatus()
    {
        bool hasDatabase = _databaseName != null;
        MenuSave.IsEnabled = hasDatabase;
        MenuSaveAs.IsEnabled = hasDatabase;
        MenuTable.IsEnabled = hasDatabase;
        MenuRow.IsEnabled = hasDatabase;
        MenuOperations.IsEnabled = hasDatabase;

        if (!hasDatabase)
        {
            StatusText.Text = "База не відкрита  |  " + _source.Description;
            Title = "TableDb";
            return;
        }

        string text = "База: " + _databaseName + (_isDirty ? " *" : "");
        text += "  |  " + _source.Description;
        if (_currentTable != null)
            text += "  |  Таблиця: " + _currentTable.Name + ", рядків: " + _currentTable.Rows.Count;
        StatusText.Text = text;
        Title = "TableDb — " + _databaseName + (_isDirty ? " *" : "");
    }

    private async void OnTableSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
            return;

        _currentTableName = TablesList.SelectedItem as string;
        await ShowTableAsync();
        UpdateStatus();
    }

    private async Task<bool> ConfirmUnsavedChangesAsync()
    {
        if (!_isDirty)
            return true;

        UnsavedChangesWindow dialog = new UnsavedChangesWindow();
        await dialog.ShowDialog(this);

        if (dialog.Choice == UnsavedChoice.Cancel)
            return false;
        if (dialog.Choice == UnsavedChoice.Discard)
            return true;

        return await SaveAsync(false);
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_closeConfirmed || !_isDirty)
            return;

        e.Cancel = true;
        if (await ConfirmUnsavedChangesAsync())
        {
            _closeConfirmed = true;
            Close();
        }
    }

    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private async void OnNewDatabase(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmUnsavedChangesAsync())
            return;

        InputWindow dialog = new InputWindow("Нова база", "Назва нової бази", "Нова база", _source.NewDatabaseAsync);
        await dialog.ShowDialog(this);
        if (!dialog.Confirmed)
            return;

        _currentTableName = null;
        SetDirty(false);
        await RefreshAllAsync();
    }

    private async void OnOpen(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmUnsavedChangesAsync())
            return;

        string? target = await AskOpenTargetAsync();
        if (target == null)
            return;

        ValidationResult result = await _source.LoadAsync(target);
        if (!result.IsValid)
        {
            await ShowErrorAsync(result.Error);
            return;
        }

        _currentTableName = null;
        SetDirty(false);
        await RefreshAllAsync();
    }

    private async void OnSave(object? sender, RoutedEventArgs e) => await SaveAsync(false);

    private async void OnSaveAs(object? sender, RoutedEventArgs e) => await SaveAsync(true);

    private async Task<bool> SaveAsync(bool forceDialog)
    {
        string? target = _source.CurrentTarget;
        if (target == null || forceDialog)
        {
            target = await AskSaveTargetAsync();
            if (target == null)
                return false;
        }

        ValidationResult result = await _source.SaveAsync(target);
        if (!result.IsValid)
        {
            await ShowErrorAsync(result.Error);
            return false;
        }

        SetDirty(false);
        return true;
    }

    private async Task<string?> AskSaveTargetAsync()
    {
        if (_source.IsRemote)
            return await AskServerFileNameAsync("Зберегти базу на сервері");

        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Зберегти базу",
            SuggestedFileName = (_databaseName ?? "database") + ".json",
            DefaultExtension = "json",
            FileTypeChoices = new[] { new FilePickerFileType("База TableDb (JSON)") { Patterns = new[] { "*.json" } } }
        });
        return file?.TryGetLocalPath();
    }

    private async Task<string?> AskOpenTargetAsync()
    {
        if (_source.IsRemote)
            return await AskServerFileNameAsync("Відкрити базу з сервера");

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Відкрити базу",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("База TableDb (JSON)") { Patterns = new[] { "*.json" } } }
        });
        if (files.Count == 0)
            return null;
        return files[0].TryGetLocalPath();
    }

    private async Task<string?> AskServerFileNameAsync(string title)
    {
        string fileName = _source.CurrentTarget ?? "database.json";
        InputWindow dialog = new InputWindow(title, "Ім'я файлу на сервері", fileName,
            name => Task.FromResult(ValidationResult.Ok()));
        await dialog.ShowDialog(this);
        if (!dialog.Confirmed)
            return null;
        return dialog.EnteredText;
    }

    private async void OnCreateTable(object? sender, RoutedEventArgs e)
    {
        CreateTableWindow dialog = new CreateTableWindow(_source.CreateTableAsync);
        await dialog.ShowDialog(this);
        if (!dialog.Confirmed)
            return;

        _currentTableName = dialog.CreatedTableName;
        SetDirty(true);
        await RefreshAllAsync();
    }

    private async void OnDropTable(object? sender, RoutedEventArgs e)
    {
        if (_currentTableName == null)
        {
            await ShowMessageAsync("Таблиця", "Спочатку оберіть таблицю у списку зліва");
            return;
        }

        ValidationResult result = await _source.DropTableAsync(_currentTableName);
        if (!result.IsValid)
        {
            await ShowErrorAsync(result.Error);
            return;
        }

        _currentTableName = null;
        SetDirty(true);
        await RefreshAllAsync();
    }

    private async void OnAddRow(object? sender, RoutedEventArgs e)
    {
        Table? table = _currentTable;
        if (table == null)
        {
            await ShowMessageAsync("Рядок", "Спочатку оберіть таблицю у списку зліва");
            return;
        }

        string tableName = table.Name;
        RowWindow dialog = new RowWindow("Новий рядок", table.Columns, null,
            values => _source.AddRowAsync(tableName, values));
        await dialog.ShowDialog(this);
        if (!dialog.Confirmed)
            return;

        SetDirty(true);
        await ShowTableAsync();
        UpdateStatus();
    }

    private async void OnEditRow(object? sender, RoutedEventArgs e) => await EditSelectedRowAsync();

    private async void OnGridDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e) => await EditSelectedRowAsync();

    private async Task EditSelectedRowAsync()
    {
        Table? table = _currentTable;
        int index = RowsGrid.SelectedIndex;
        if (table == null || index < 0 || index >= table.Rows.Count)
        {
            await ShowMessageAsync("Рядок", "Оберіть рядок у сітці");
            return;
        }

        string tableName = table.Name;
        RowWindow dialog = new RowWindow("Редагування рядка", table.Columns, new List<string>(table.Rows[index]),
            values => _source.EditRowAsync(tableName, index, values));
        await dialog.ShowDialog(this);
        if (!dialog.Confirmed)
            return;

        SetDirty(true);
        await ShowTableAsync();
        UpdateStatus();
    }

    private async void OnDeleteRow(object? sender, RoutedEventArgs e)
    {
        Table? table = _currentTable;
        int index = RowsGrid.SelectedIndex;
        if (table == null || index < 0 || index >= table.Rows.Count)
        {
            await ShowMessageAsync("Рядок", "Оберіть рядок у сітці");
            return;
        }

        ValidationResult result = await _source.DeleteRowAsync(table.Name, index);
        if (!result.IsValid)
        {
            await ShowErrorAsync(result.Error);
            return;
        }

        SetDirty(true);
        await ShowTableAsync();
        UpdateStatus();
    }

    private async void OnProduct(object? sender, RoutedEventArgs e)
    {
        Result<List<string>> names = await _source.GetTableNamesAsync();
        if (!names.IsOk)
        {
            await ShowErrorAsync(names.Error);
            return;
        }
        if (names.Value.Count < 2)
        {
            await ShowMessageAsync("Прямий добуток", "Для добутку потрібно щонайменше дві таблиці");
            return;
        }

        ProductWindow dialog = new ProductWindow(names.Value, _source.CartesianProductAsync);
        await dialog.ShowDialog(this);
        if (!dialog.Confirmed)
            return;

        _currentTableName = dialog.ResultName;
        SetDirty(true);
        await RefreshAllAsync();
    }
}
