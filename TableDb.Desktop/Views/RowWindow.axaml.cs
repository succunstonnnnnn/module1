using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TableDb.Core;
using TableDb.Core.FieldTypes;

namespace TableDb.Desktop.Views;

public partial class RowWindow : Window
{
    private readonly List<Column> _columns = new List<Column>();
    private readonly Func<List<string>, Task<ValidationResult>>? _submit;

    private readonly List<TextBox?> _textBoxes = new List<TextBox?>();
    private readonly List<string> _fileValues = new List<string>();
    private readonly List<TextBlock?> _fileNameLabels = new List<TextBlock?>();

    public bool Confirmed { get; private set; }

    public RowWindow()
    {
        InitializeComponent();
    }

    public RowWindow(string title, List<Column> columns, List<string>? initialValues,
        Func<List<string>, Task<ValidationResult>> submit) : this()
    {
        Title = title;
        _columns = columns;
        _submit = submit;
        BuildFields(initialValues);
    }

    private void BuildFields(List<string>? initialValues)
    {
        for (int i = 0; i < _columns.Count; i++)
        {
            Column column = _columns[i];
            string initial = initialValues != null ? initialValues[i] : string.Empty;

            StackPanel block = new StackPanel { Spacing = 3 };
            TextBlock label = new TextBlock { Text = column.Name + " (" + column.Type.Name + ")" };
            label.Classes.Add("label");
            block.Children.Add(label);

            if (column.Type is TextFileType)
            {
                _textBoxes.Add(null);
                _fileValues.Add(initial);

                TextBlock nameLabel = new TextBlock
                {
                    Text = initial.Length > 0 ? TextFileType.GetFileName(initial) : "(файл не обрано)"
                };
                _fileNameLabels.Add(nameLabel);

                Button pick = new Button { Content = "Обрати файл…" };
                int columnIndex = i;
                pick.Click += async (_, _) => await PickFileAsync(columnIndex);

                StackPanel line = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
                line.Children.Add(pick);
                line.Children.Add(nameLabel);
                block.Children.Add(line);
            }
            else
            {
                _fileValues.Add(string.Empty);
                _fileNameLabels.Add(null);
                TextBox box = new TextBox { Text = initial };
                _textBoxes.Add(box);
                block.Children.Add(box);
            }

            TextBlock hint = new TextBlock { Text = column.Type.Hint };
            hint.Classes.Add("hint");
            block.Children.Add(hint);

            FieldsPanel.Children.Add(block);
        }
    }

    private async Task PickFileAsync(int columnIndex)
    {
        IStorageProvider? storage = GetTopLevel(this)?.StorageProvider;
        if (storage == null)
            return;

        IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Оберіть текстовий файл",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Текстові файли") { Patterns = new[] { "*.txt" }, MimeTypes = new[] { "text/*" } },
                FilePickerFileTypes.All
            }
        });
        if (files.Count == 0)
            return;

        string? path = files[0].TryGetLocalPath();
        if (path == null)
            return;

        try
        {
            _fileValues[columnIndex] = TextFileType.FromFile(path);
            _fileNameLabels[columnIndex]!.Text = TextFileType.GetFileName(_fileValues[columnIndex]);
        }
        catch (InvalidDataException ex)
        {
            ErrorText.Text = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorText.Text = "Не вдалося прочитати файл: " + ex.Message;
        }
    }

    private async void OnOk(object? sender, RoutedEventArgs e)
    {
        List<string> values = new List<string>();
        for (int i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].Type is TextFileType)
                values.Add(_fileValues[i]);
            else
                values.Add(_textBoxes[i]!.Text ?? string.Empty);
        }

        OkButton.IsEnabled = false;
        ValidationResult result;
        try
        {
            result = await _submit!(values);
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

        Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
