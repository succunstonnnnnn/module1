using Avalonia.Controls;
using Avalonia.Interactivity;
using TableDb.Core;

namespace TableDb.Desktop.Views;

public partial class ProductWindow : Window
{
    private readonly Func<string, string, string, Task<ValidationResult>>? _submit;

    public bool Confirmed { get; private set; }
    public string ResultName { get; private set; } = string.Empty;

    public ProductWindow()
    {
        InitializeComponent();
    }

    public ProductWindow(List<string> tableNames, Func<string, string, string, Task<ValidationResult>> submit) : this()
    {
        _submit = submit;
        TableABox.ItemsSource = tableNames;
        TableBBox.ItemsSource = tableNames;
        if (tableNames.Count > 0) TableABox.SelectedIndex = 0;
        if (tableNames.Count > 1) TableBBox.SelectedIndex = 1;
    }

    private async void OnOk(object? sender, RoutedEventArgs e)
    {
        string a = TableABox.SelectedItem as string ?? string.Empty;
        string b = TableBBox.SelectedItem as string ?? string.Empty;
        string resultName = (ResultNameBox.Text ?? string.Empty).Trim();

        OkButton.IsEnabled = false;
        ValidationResult result;
        try
        {
            result = await _submit!(a, b, resultName);
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

        ResultName = resultName;
        Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
