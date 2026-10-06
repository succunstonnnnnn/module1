using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TableDb.Desktop.DataSources;
using TableDb.Desktop.Views;

namespace TableDb.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StartWindow? start = null;
            start = new StartWindow(source =>
            {
                MainWindow main = new MainWindow(source);
                desktop.MainWindow = main;
                main.Show();
                start!.Close();
            });
            desktop.MainWindow = start;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
