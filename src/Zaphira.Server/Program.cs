using Zaphira.Contracts;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/health", () => Results.Ok(new HealthResponse("Zaphira.Server", "Healthy")))
    .WithName("GetHealth");

app.Run();
