using Avalonia.Controls;
using Avalonia.Threading;
using System.Collections.Specialized;
using Zaphira.Client.ViewModels;

namespace Zaphira.Client.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? subscribedViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += HandleDataContextChanged;
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

    private void HandleDataContextChanged(object? sender, EventArgs e)
    {
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.ChatWorkspace.Messages.CollectionChanged -= HandleMessagesChanged;
            foreach (ChatMessageViewModel message in subscribedViewModel.ChatWorkspace.Messages)
            {
                message.RenderedParts.CollectionChanged -= HandleRenderedPartsChanged;
            }
        }

        subscribedViewModel = DataContext as MainWindowViewModel;
        if (subscribedViewModel is null)
        {
            return;
        }

        subscribedViewModel.ChatWorkspace.Messages.CollectionChanged += HandleMessagesChanged;
        foreach (ChatMessageViewModel message in subscribedViewModel.ChatWorkspace.Messages)
        {
            message.RenderedParts.CollectionChanged += HandleRenderedPartsChanged;
        }

        ScrollMessagesToEnd();
    }

    private void HandleMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (ChatMessageViewModel message in e.OldItems.OfType<ChatMessageViewModel>())
            {
                message.RenderedParts.CollectionChanged -= HandleRenderedPartsChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (ChatMessageViewModel message in e.NewItems.OfType<ChatMessageViewModel>())
            {
                message.RenderedParts.CollectionChanged += HandleRenderedPartsChanged;
            }
        }

        ScrollMessagesToEnd();
    }

    private void HandleRenderedPartsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        ScrollMessagesToEnd();

    private void ScrollMessagesToEnd()
    {
        Dispatcher.UIThread.Post(() => MessagesScrollViewer.ScrollToEnd());
    }
}
