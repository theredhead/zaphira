using Zaphira.Application.Hardware;
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
        CatalogSearchService catalogSearchService,
        CatalogCompatibilityEstimator compatibilityEstimator,
        IHardwareProfileDetector hardwareProfileDetector,
        HttpContext context,
        CancellationToken cancellationToken) =>
        LoadCatalogResultAsync(
            modelCatalogService,
            catalogSearchService,
            compatibilityEstimator,
            hardwareProfileDetector,
            CreateSearchRequest(context),
            forceSync: false,
            cancellationToken);

    private static Task<IResult> SyncCatalogAsync(
        ModelCatalogService modelCatalogService,
        CatalogSearchService catalogSearchService,
        CatalogCompatibilityEstimator compatibilityEstimator,
        IHardwareProfileDetector hardwareProfileDetector,
        CancellationToken cancellationToken) =>
        LoadCatalogResultAsync(
            modelCatalogService,
            catalogSearchService,
            compatibilityEstimator,
            hardwareProfileDetector,
            CatalogSearchRequest.All(),
            forceSync: true,
            cancellationToken);

    private static async Task<IResult> LoadCatalogResultAsync(
        ModelCatalogService modelCatalogService,
        CatalogSearchService catalogSearchService,
        CatalogCompatibilityEstimator compatibilityEstimator,
        IHardwareProfileDetector hardwareProfileDetector,
        CatalogSearchRequest searchRequest,
        bool forceSync,
        CancellationToken cancellationToken)
    {
        ModelCatalogLoadResult result = await modelCatalogService.LoadAsync(forceSync, cancellationToken);
        if (!result.IsAvailable)
        {
            return Results.Json(ErrorResponse.CatalogUnavailable(), statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        IReadOnlyList<CatalogModelSearchResult> searchResults = catalogSearchService.Search(result.Models, searchRequest);
        HardwareProfile hardwareProfile = await hardwareProfileDetector.DetectAsync(cancellationToken);

        return Results.Ok(new ModelCatalogResponse(
            result.IsFromCache,
            result.Message,
            result.Suggestion,
            searchResults
                .Select(searchResult => ToResponse(
                    searchResult,
                    compatibilityEstimator.Estimate(searchResult.Model, hardwareProfile)))
                .ToArray()));
    }

    private static CatalogSearchRequest CreateSearchRequest(HttpContext context)
    {
        string query = context.Request.Query["query"].ToString();
        List<CatalogModelPurpose> purposes = [];
        foreach (string? rawPurposeValue in context.Request.Query["purpose"])
        {
            string purposeValue = rawPurposeValue ?? string.Empty;
            if (Enum.TryParse(purposeValue, ignoreCase: true, out CatalogModelPurpose purpose))
            {
                purposes.Add(purpose);
            }
        }

        return new CatalogSearchRequest(query, purposes);
    }

    private static CatalogModelResponse ToResponse(
        CatalogModelSearchResult searchResult,
        CatalogModelSearchResult compatibilityEstimate) =>
        new(
            searchResult.Model.Id,
            searchResult.Model.DisplayName,
            searchResult.Model.Tags,
            searchResult.Model.Purposes.Select(purpose => purpose.ToString()).ToArray(),
            compatibilityEstimate.CompatibilityStatus.ToString(),
            compatibilityEstimate.CompatibilityConfidence.ToString(),
            compatibilityEstimate.MatchExplanation,
            searchResult.MatchExplanation);
}
