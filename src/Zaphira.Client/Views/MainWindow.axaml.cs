using Avalonia.Controls;
using Zaphira.Client.ViewModels;

namespace Zaphira.Client.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.ChatWorkspace.LoadAsync(CancellationToken.None);
        }
    }

    private void OpenModelCatalogWindow(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        ModelCatalogWindow modelCatalogWindow = new()
        {
            DataContext = viewModel.ModelCatalogWorkspace
        };

        modelCatalogWindow.Show(this);
    }
}
