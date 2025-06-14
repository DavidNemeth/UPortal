using Serilog;
using UPortal.Components;
using UPortal.Data;
using UPortal.Extensions; // <-- Add this using statement

// Configure Serilog first to capture all startup logs
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/uportal-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Application Starting Up");

    var builder = WebApplication.CreateBuilder(args);

    // --- 1. Configure Services ---

    builder.Host.UseSerilog();

    // Grouped service configurations using extension methods
    builder.Services
        .AddApplicationServices(builder.Configuration)
        .AddPresentationServices()
        .AddUportalAuthentication(builder.Configuration)
        .AddSwaggerServices()
        .AddUportalCors(builder.Environment, builder.Configuration);

    builder.Services.AddControllers();

    // --- 2. Build the Application ---

    var app = builder.Build();

    // --- 3. Configure Middleware Pipeline ---

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "UPortal API V1"));
    }
    else
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseCors("DefaultCorsPolicy");

    // Antiforgery middleware must be placed after UseAuthentication and UseAuthorization
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    // --- 4. Map Endpoints ---

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .RequireAuthorization();

    app.MapControllers();

    // --- 5. Seed Data and Run ---

    // Seed the database with initial data
    await DataSeeder.SeedAsync(app);

    Log.Information("Application Starting...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    // Ensure all logs are flushed on shutdown
    Log.CloseAndFlush();
}