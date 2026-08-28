using Zaphira.Client.Configuration;
using Zaphira.Client.ViewModels;

namespace Zaphira.Client.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void ConstructorStartsInFirstRunWhenConfigurationRequestsIt()
    {
        MainWindowViewModel viewModel = new(ZaphiraClientConfiguration.Default());

        Assert.Equal(ClientPage.FirstRun, viewModel.SelectedPage);
        Assert.Equal(BackendConnectionState.SetupRequired, viewModel.BackendConnectionState);
    }

    [Fact]
    public void NavigationCommandsChangeSelectedPage()
    {
        MainWindowViewModel viewModel = new(ZaphiraClientConfiguration.Default());

        viewModel.ShowSettingsCommand.Execute(null);
        Assert.Equal(ClientPage.Settings, viewModel.SelectedPage);

        viewModel.ShowChatCommand.Execute(null);
        Assert.Equal(ClientPage.Chat, viewModel.SelectedPage);
    }
}
