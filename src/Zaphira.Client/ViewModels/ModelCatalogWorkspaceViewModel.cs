using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Zaphira.Client.Chat;
using Zaphira.Client.ModelCatalog;
using Zaphira.Contracts;

namespace Zaphira.Client.ViewModels;

public partial class ModelCatalogWorkspaceViewModel : ViewModelBase
{
    private readonly IModelCatalogApiClient modelCatalogApiClient;
    private string searchText = string.Empty;
    private string selectedPurpose = "Any";
    private string statusText = "Catalog not loaded.";
    private bool isLoading;

    public ModelCatalogWorkspaceViewModel(IModelCatalogApiClient modelCatalogApiClient)
    {
        ArgumentNullException.ThrowIfNull(modelCatalogApiClient);

        this.modelCatalogApiClient = modelCatalogApiClient;
    }

    public ObservableCollection<CatalogModelItemViewModel> Models { get; } = [];

    public IReadOnlyList<string> PurposeOptions { get; } =
        ["Any", "GeneralChat", "Coding", "Vision", "Embeddings"];

    public string SearchText
    {
        get => searchText;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            SetProperty(ref searchText, value);
        }
    }

    public string SelectedPurpose
    {
        get => selectedPurpose;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref selectedPurpose, value);
        }
    }

    public string StatusText
    {
        get => statusText;
        private set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            SetProperty(ref statusText, value);
        }
    }

    public bool IsLoading
    {
        get => isLoading;
        private set => SetProperty(ref isLoading, value);
    }

    [RelayCommand]
    public async Task SearchAsync(CancellationToken cancellationToken)
    {
        await LoadCatalogAsync(forceSync: false, cancellationToken);
    }

    [RelayCommand]
    public async Task SyncNowAsync(CancellationToken cancellationToken)
    {
        await LoadCatalogAsync(forceSync: true, cancellationToken);
    }

    private async Task LoadCatalogAsync(bool forceSync, CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            ModelCatalogResponse response = forceSync
                ? await modelCatalogApiClient.SyncCatalogAsync(cancellationToken)
                : await modelCatalogApiClient.GetCatalogAsync(SearchText, SelectedPurpose, cancellationToken);

            Models.Clear();
            foreach (CatalogModelResponse model in response.Models)
            {
                Models.Add(new CatalogModelItemViewModel(model));
            }

            StatusText = response.Message;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            StatusText = ToStatusText(exception);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string ToStatusText(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is ChatApiException chatApiException
            ? $"{chatApiException.Error.Message} {chatApiException.Error.Suggestion}"
            : exception.Message;
    }
}
