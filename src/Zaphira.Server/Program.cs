using Microsoft.AspNetCore.Diagnostics;
using Zaphira.Application;
using Zaphira.Application.ModelCatalog;
using Zaphira.Application.Providers;
using Zaphira.Infrastructure.Security;
using Zaphira.Contracts;
using Zaphira.Infrastructure.ModelCatalog;
using Zaphira.Infrastructure.Persistence;
using Zaphira.Infrastructure.Providers.Ollama;
using Zaphira.Infrastructure.Storage;
using Zaphira.Server.Chat;
using Zaphira.Server.Configuration;
using Zaphira.Server.ModelCatalog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(options =>
{
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId;
});

builder.Services.AddProblemDetails();

builder.Services.AddSingleton<ZaphiraDataDirectories>(serviceProvider =>
    ZaphiraServerConfiguration.LoadDataDirectories(serviceProvider.GetRequiredService<IConfiguration>()));
builder.Services.AddSingleton<ServerHttpsCertificateManager>();
builder.Services.AddSingleton<SqliteDatabaseMigrator>();
builder.Services.AddSingleton<IConversationRepository>(serviceProvider =>
    new SqliteConversationRepository(serviceProvider.GetRequiredService<ZaphiraDataDirectories>().ServerDatabaseFile));
builder.Services.AddSingleton<IMessageRepository>(serviceProvider =>
    new SqliteMessageRepository(serviceProvider.GetRequiredService<ZaphiraDataDirectories>().ServerDatabaseFile));
builder.Services.AddSingleton<IChatModelProvider>(_ =>
    new OllamaChatModelProvider(new HttpClient
    {
        BaseAddress = new Uri("http://localhost:11434")
    }));
builder.Services.AddSingleton<GenerationCancellationRegistry>();
builder.Services.AddSingleton<ICatalogSource>(_ =>
    new HuggingFaceCatalogSource(new HttpClient
    {
        BaseAddress = new Uri("https://huggingface.co")
    }));
builder.Services.AddSingleton<IModelCatalogCache>(serviceProvider =>
    new FileModelCatalogCache(serviceProvider.GetRequiredService<ZaphiraDataDirectories>().ServerModelCatalogCacheFile));
builder.Services.AddSingleton(serviceProvider =>
    new ModelCatalogService(
        serviceProvider.GetRequiredService<ICatalogSource>(),
        serviceProvider.GetRequiredService<IModelCatalogCache>(),
        TimeProvider.System));

builder.WebHost.ConfigureKestrel((context, options) =>
{
    ZaphiraDataDirectories serverDataDirectories = ZaphiraServerConfiguration.LoadDataDirectories(context.Configuration);
    ServerHttpsCertificateManager certificateManager = new();
    ServerHttpsCertificateMaterial certificateMaterial = certificateManager
        .LoadOrCreateAsync(serverDataDirectories, CancellationToken.None)
        .GetAwaiter()
        .GetResult();
    int httpsPort = ZaphiraServerConfiguration.LoadHttpsPort(context.Configuration);

    options.ListenLocalhost(httpsPort, listenOptions =>
    {
        listenOptions.UseHttps(certificateMaterial.Certificate);
    });
});

WebApplication app = builder.Build();

ZaphiraDataDirectories dataDirectories = app.Services.GetRequiredService<ZaphiraDataDirectories>();
await dataDirectories.EnsureServerDirectoriesExistAsync(CancellationToken.None);
await app.Services
    .GetRequiredService<SqliteDatabaseMigrator>()
    .MigrateAsync(dataDirectories.ServerDatabaseFile, CancellationToken.None);
ServerHttpsCertificateMaterial httpsCertificateMaterial = await app.Services
    .GetRequiredService<ServerHttpsCertificateManager>()
    .LoadOrCreateAsync(dataDirectories, CancellationToken.None);

app.Logger.LogInformation("Zaphira server using data directory {ServerDataDirectory}", dataDirectories.ServerRoot);
app.Logger.LogInformation(
    "Zaphira server HTTPS certificate loaded from {CertificatePath} with thumbprint {CertificateThumbprint}",
    httpsCertificateMaterial.CertificatePath,
    httpsCertificateMaterial.Thumbprint);

app.UseExceptionHandler(errorApplication =>
{
    errorApplication.Run(async context =>
    {
        ILogger<Program> logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        IExceptionHandlerPathFeature? exceptionDetails = context.Features.Get<IExceptionHandlerPathFeature>();
        if (exceptionDetails is null)
        {
            logger.LogError(
                "Unexpected server error while processing {RequestMethod} {RequestPath}",
                context.Request.Method,
                context.Request.Path);
        }
        else
        {
            logger.LogError(
                exceptionDetails.Error,
                "Unexpected server error while processing {RequestMethod} {RequestPath}",
                context.Request.Method,
                context.Request.Path);
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(ErrorResponse.UnexpectedServerError(), context.RequestAborted);
    });
});

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new HealthResponse("Zaphira.Server", "Healthy")))
    .WithName("GetHealth");

app.MapChatApi();
app.MapModelCatalogApi();

app.MapFallback(() => Results.NotFound(ErrorResponse.RouteNotFound()));

app.Run();

public partial class Program;
