using Zaphira.Application.ModelCatalog;
using Zaphira.Contracts;

namespace Zaphira.Server.ModelCatalog;

internal static class ModelCatalogApiEndpoints
{
    public static IEndpointRouteBuilder MapModelCatalogApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api/model-catalog");

        group.MapGet("/", LoadCatalogAsync);
        group.MapPost("/sync", SyncCatalogAsync);

        return endpoints;
    }

    private static Task<IResult> LoadCatalogAsync(
        ModelCatalogService modelCatalogService,
        CancellationToken cancellationToken) =>
        LoadCatalogResultAsync(modelCatalogService, forceSync: false, cancellationToken);

    private static Task<IResult> SyncCatalogAsync(
        ModelCatalogService modelCatalogService,
        CancellationToken cancellationToken) =>
        LoadCatalogResultAsync(modelCatalogService, forceSync: true, cancellationToken);

    private static async Task<IResult> LoadCatalogResultAsync(
        ModelCatalogService modelCatalogService,
        bool forceSync,
        CancellationToken cancellationToken)
    {
        ModelCatalogLoadResult result = await modelCatalogService.LoadAsync(forceSync, cancellationToken);
        if (!result.IsAvailable)
        {
            return Results.Json(ErrorResponse.CatalogUnavailable(), statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Ok(new ModelCatalogResponse(
            result.IsFromCache,
            result.Message,
            result.Suggestion,
            result.Models.Select(ToResponse).ToArray()));
    }

    private static CatalogModelResponse ToResponse(CatalogModelSummary model) =>
        new(
            model.Id,
            model.DisplayName,
            model.Tags,
            model.Purposes.Select(purpose => purpose.ToString()).ToArray());
}
