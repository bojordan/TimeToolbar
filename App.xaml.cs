using Microsoft.UI.Xaml;

namespace TimeToolbar;

public partial class App : Application
{
    private MainWindow? _mainWindow;

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
