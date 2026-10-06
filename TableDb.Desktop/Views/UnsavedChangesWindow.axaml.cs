using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TableDb.Desktop.Views;

public enum UnsavedChoice
{
    Save,
    Discard,
    Cancel
}

public partial class UnsavedChangesWindow : Window
{
    public UnsavedChoice Choice { get; private set; } = UnsavedChoice.Cancel;

    public UnsavedChangesWindow()
    {
        InitializeComponent();
    }

    private void OnSave(object? sender, RoutedEventArgs e) { Choice = UnsavedChoice.Save; Close(); }
    private void OnDiscard(object? sender, RoutedEventArgs e) { Choice = UnsavedChoice.Discard; Close(); }
    private void OnCancel(object? sender, RoutedEventArgs e) { Choice = UnsavedChoice.Cancel; Close(); }
}
