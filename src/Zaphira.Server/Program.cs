using Microsoft.AspNetCore.Diagnostics;
using Zaphira.Contracts;
using Zaphira.Infrastructure.Storage;
using Zaphira.Server.Configuration;

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

WebApplication app = builder.Build();

ZaphiraDataDirectories dataDirectories = app.Services.GetRequiredService<ZaphiraDataDirectories>();
await dataDirectories.EnsureServerDirectoriesExistAsync(CancellationToken.None);

app.Logger.LogInformation("Zaphira server using data directory {ServerDataDirectory}", dataDirectories.ServerRoot);

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

app.MapFallback(() => Results.NotFound(ErrorResponse.RouteNotFound()));

app.Run();

public partial class Program;
