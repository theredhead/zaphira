using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Zaphira.Client.Configuration;
using Zaphira.Client.Logging;
using Zaphira.Client.Storage;
using Zaphira.Client.ViewModels;
using Zaphira.Client.Views;

namespace Zaphira.Client;

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
            ZaphiraClientConfiguration configuration = LoadConfiguration();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(configuration),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ZaphiraClientConfiguration LoadConfiguration()
    {
        ZaphiraClientDataDirectories dataDirectories = ZaphiraClientDataDirectories.ForCurrentUser();
        ZaphiraClientConfigurationLoader loader = new(dataDirectories);
        ZaphiraClientStartupLogger logger = new(dataDirectories);

        ZaphiraClientConfiguration configuration = loader.LoadOrCreateAsync(CancellationToken.None).GetAwaiter().GetResult();
        logger.LogStartupAsync(configuration, CancellationToken.None).GetAwaiter().GetResult();

        return configuration;
    }
}
