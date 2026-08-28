using Zaphira.Application.Providers;
using Zaphira.Domain;

namespace Zaphira.Server.Chat;

internal sealed class ActiveModelSelectionStore
{
    private readonly object syncRoot = new();
    private ModelId selectedModelId = ModelId.NoActiveModel;

    public ModelId GetActiveModel(ProviderModelCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        lock (syncRoot)
        {
            if (catalog.Models.Any(model => model.Id == selectedModelId))
            {
                return selectedModelId;
            }

            return catalog.Models.Count > 0
                ? catalog.Models[0].Id
                : ModelId.NoActiveModel;
        }
    }

    public void Select(ModelId modelId)
    {
        ArgumentNullException.ThrowIfNull(modelId);

        lock (syncRoot)
        {
            selectedModelId = modelId;
        }
    }
}
