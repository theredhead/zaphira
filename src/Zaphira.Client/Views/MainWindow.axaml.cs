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
}
