using Zaphira.Contracts;
using Zaphira.Infrastructure.Security;

namespace Zaphira.Server.Pairing;

internal static class PairingApiEndpoints
{
    public static IEndpointRouteBuilder MapPairingApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/api");

        group.MapPost("/pairing-code", CreatePairingCode);
        group.MapPost("/pairings", CreatePairingAsync);
        group.MapGet("/pairings", ListPairingsAsync);
        group.MapDelete("/pairings/{pairingId:guid}", RevokePairingAsync);

        return endpoints;
    }

    private static IResult CreatePairingCode(ServerPairingRegistry pairingRegistry)
    {
        PairingCode code = pairingRegistry.CreatePairingCode();

        return Results.Ok(new CreatePairingCodeResponse(code.Value, code.ExpiresAt));
    }

    private static async Task<IResult> CreatePairingAsync(
        CreatePairingRequest request,
        ServerPairingRegistry pairingRegistry,
        ServerHttpsCertificateMaterial certificateMaterial,
        CancellationToken cancellationToken)
    {
        ServerPairingCreationResult result = await pairingRegistry.CreatePairingAsync(
            request.Code,
            request.ClientName,
            cancellationToken);

        if (!result.IsCreated)
        {
            return Results.BadRequest(ErrorResponse.PairingCodeInvalid());
        }

        return Results.Created(
            $"/api/pairings/{result.Pairing.Id}",
            new CreatePairingResponse(
                result.Pairing.Id,
                result.Pairing.AccessToken,
                certificateMaterial.Thumbprint,
                "Zaphira backend"));
    }

    private static async Task<IResult> ListPairingsAsync(
        ServerPairingRegistry pairingRegistry,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ServerPairing> pairings = await pairingRegistry.ListPairingsAsync(cancellationToken);

        return Results.Ok(new PairingListResponse(pairings.Select(pairing => pairing.ToResponse()).ToArray()));
    }

    private static async Task<IResult> RevokePairingAsync(
        Guid pairingId,
        ServerPairingRegistry pairingRegistry,
        CancellationToken cancellationToken)
    {
        bool revoked = await pairingRegistry.RevokePairingAsync(pairingId, cancellationToken);

        return revoked
            ? Results.NoContent()
            : Results.NotFound(ErrorResponse.PairingNotFound());
    }
}
