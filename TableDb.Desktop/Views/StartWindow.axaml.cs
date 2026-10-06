using Avalonia.Controls;
using Avalonia.Interactivity;
using TableDb.Core;
using TableDb.Desktop.DataSources;

namespace TableDb.Desktop.Views;

public partial class StartWindow : Window
{
    private readonly Action<IDataSource>? _onChosen;

    public StartWindow()
    {
        InitializeComponent();
    }

    public StartWindow(Action<IDataSource> onChosen) : this()
    {
        _onChosen = onChosen;
    }

    private bool IsRemoteSelected => ModeList.SelectedIndex == 1;

    private void OnModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ServerPanel == null)
            return;
        ServerPanel.IsVisible = IsRemoteSelected;
        ShowInfo(string.Empty, false);
    }

    private void ShowInfo(string text, bool isError)
    {
        InfoText.Text = text;
        InfoText.Classes.Set("error", isError);
        InfoText.Classes.Set("muted", !isError);
    }

    private RemoteDataSource? CreateRemoteSource()
    {
        if (!RemoteDataSource.TryNormalizeAddress(AddressBox.Text ?? string.Empty, out string address, out string error))
        {
            ShowInfo(error, true);
            return null;
        }
        return new RemoteDataSource(address);
    }

    private async void OnCheckConnection(object? sender, RoutedEventArgs e)
    {
        RemoteDataSource? source = CreateRemoteSource();
        if (source == null)
            return;

        SetBusy(true);
        ValidationResult result = await source.CheckConnectionAsync();
        SetBusy(false);

        if (result.IsValid)
            ShowInfo("З'єднання з сервером встановлено", false);
        else
            ShowInfo(result.Error, true);
    }

    private async void OnContinue(object? sender, RoutedEventArgs e)
    {
        if (!IsRemoteSelected)
        {
            _onChosen!(new LocalDataSource());
            return;
        }

        RemoteDataSource? source = CreateRemoteSource();
        if (source == null)
            return;

        SetBusy(true);
        ValidationResult result = await source.CheckConnectionAsync();
        SetBusy(false);

        if (!result.IsValid)
        {
            ShowInfo(result.Error, true);
            return;
        }

        _onChosen!(source);
    }

    private void SetBusy(bool busy)
    {
        CheckButton.IsEnabled = !busy;
        ContinueButton.IsEnabled = !busy;
        if (busy)
            ShowInfo("З'єднання…", false);
    }
}
