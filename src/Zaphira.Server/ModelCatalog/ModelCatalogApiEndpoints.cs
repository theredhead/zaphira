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
        HttpContext context,
        CancellationToken cancellationToken) =>
        LoadCatalogResultAsync(
            modelCatalogService,
            catalogSearchService,
            CreateSearchRequest(context),
            forceSync: false,
            cancellationToken);

    private static Task<IResult> SyncCatalogAsync(
        ModelCatalogService modelCatalogService,
        CatalogSearchService catalogSearchService,
        CancellationToken cancellationToken) =>
        LoadCatalogResultAsync(
            modelCatalogService,
            catalogSearchService,
            CatalogSearchRequest.All(),
            forceSync: true,
            cancellationToken);

    private static async Task<IResult> LoadCatalogResultAsync(
        ModelCatalogService modelCatalogService,
        CatalogSearchService catalogSearchService,
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

        return Results.Ok(new ModelCatalogResponse(
            result.IsFromCache,
            result.Message,
            result.Suggestion,
            searchResults.Select(ToResponse).ToArray()));
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

    private static CatalogModelResponse ToResponse(CatalogModelSearchResult result) =>
        new(
            result.Model.Id,
            result.Model.DisplayName,
            result.Model.Tags,
            result.Model.Purposes.Select(purpose => purpose.ToString()).ToArray(),
            result.CompatibilityStatus.ToString(),
            result.CompatibilityConfidence.ToString(),
            result.MatchExplanation);
}
