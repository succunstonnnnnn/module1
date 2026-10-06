using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TableDb.Desktop.Views;

public partial class MessageWindow : Window
{
    public MessageWindow()
    {
        InitializeComponent();
    }

    public MessageWindow(string title, string message) : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close();
}
