using Microsoft.UI.Xaml;

namespace GameStoreApp;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public Window? MainWindowInstance => _mainWindow;

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
