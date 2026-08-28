using Zaphira.Contracts;
using Zaphira.Infrastructure.Storage;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

ZaphiraDataDirectories dataDirectories = ZaphiraDataDirectories.ForCurrentUser();
await dataDirectories.EnsureServerDirectoriesExistAsync(CancellationToken.None);

builder.Services.AddSingleton(dataDirectories);

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new HealthResponse("Zaphira.Server", "Healthy")))
    .WithName("GetHealth");

app.Run();
