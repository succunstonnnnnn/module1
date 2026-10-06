using Avalonia.Controls;
using Avalonia.Interactivity;
using TableDb.Core;

namespace TableDb.Desktop.Views;

public partial class InputWindow : Window
{
    private readonly Func<string, Task<ValidationResult>>? _submit;

    public bool Confirmed { get; private set; }

    public string EnteredText { get; private set; } = string.Empty;

    public InputWindow()
    {
        InitializeComponent();
    }

    public InputWindow(string title, string prompt, string initialValue, Func<string, Task<ValidationResult>> submit) : this()
    {
        Title = title;
        PromptText.Text = prompt;
        ValueBox.Text = initialValue;
        _submit = submit;
    }

    private async void OnOk(object? sender, RoutedEventArgs e)
    {
        OkButton.IsEnabled = false;
        ValidationResult result;
        try
        {
            result = await _submit!(ValueBox.Text ?? string.Empty);
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
        EnteredText = ValueBox.Text ?? string.Empty;
        Confirmed = true;
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e) => Close();
}
