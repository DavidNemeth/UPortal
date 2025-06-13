using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using UPortal.Components;
using UPortal.Data;
using UPortal.Services;
using UPortal.HelperServices;
using Serilog;
using Microsoft.OpenApi.Models;

// Configure Serilog early in the application startup.
// This ensures that all startup activities can be logged.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext() // Allows enrichment from LogContext, useful for adding properties dynamically.
    .Enrich.WithMachineName() // Adds the machine name where the log event occurred.
    .Enrich.WithThreadId() // Adds the managed thread ID.
    .Enrich.WithProcessId() // Adds the process ID.
    .Enrich.WithEnvironmentUserName() // Adds the environment user name.
    .WriteTo.Console() // Outputs logs to the console.
    .WriteTo.File("logs/uportal-.txt", rollingInterval: RollingInterval.Day) // Outputs logs to a file, rolling daily.
    .CreateLogger();

try // Main try-catch block for application startup.
{
    Log.Information("Application Starting Up");

    var builder = WebApplication.CreateBuilder(args);

    // Integrate Serilog with the .NET Core logging system.
    // This makes Serilog the logging provider for the application.
    builder.Host.UseSerilog();

    // Add services to the container.
    // Configure authentication with Azure AD using OpenID Connect.
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    // Configure OpenID Connect options, specifically the OnTokenValidated event.
    builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
    {
        options.Events ??= new OpenIdConnectEvents();
        // This event is triggered after a user's token has been validated.
        options.Events.OnTokenValidated = async context =>
        {
            if (context.Principal != null)
            {
                // Attempt to resolve the AppUserService to create or update the user in the local database.
                // This ensures that users logging in via Azure AD are provisioned in the application's user store.
                var userService = context.HttpContext.RequestServices.GetService<IAppUserService>();
                if (userService != null)
                {
                    // Call the service to synchronize Azure AD user information with the local database.
                    await userService.CreateOrUpdateUserFromAzureAdAsync(context.Principal);
                }
                else
                {
                    // Log a warning if the AppUserService cannot be resolved. This indicates a potential DI configuration issue.
                    Log.Warning("IAppUserService not found in HttpContext.RequestServices during OnTokenValidated event.");
                }
            }
            // No explicit Task.CompletedTask needed if there are no further async operations here.
        };
    });

    // Optional configuration for calling protected APIs (e.g., Microsoft Graph).
    // .EnableTokenAcquisitionToCallDownstreamApi(builder.Configuration.GetSection("AzureAd:Scopes").Get<string[]>())
    // .AddMicrosoftGraph(builder.Configuration.GetSection("Graph"))
    // .AddInMemoryTokenCaches();

    // Configure authorization policies.
    builder.Services.AddAuthorization(options =>
    {
        // FallbackPolicy ensures that any request not matching a specific authorization policy
        // will fall back to the DefaultPolicy (which usually requires an authenticated user).
        // This is a common setup to protect the application by default.
        options.FallbackPolicy = options.DefaultPolicy;
    });

    // Add Razor Pages services and Microsoft Identity UI for handling authentication-related UI (login, logout pages).
    builder.Services.AddRazorPages().AddMicrosoftIdentityUI();

    // Configure Entity Framework Core DbContext.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    // Use AddDbContextFactory for creating DbContext instances on demand, which is good for Blazor Server apps.
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Add Blazor Server components and enable interactive server-side rendering.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Add Fluent UI Blazor components.
    builder.Services.AddFluentUIComponents();
    // Add HttpClient for making HTTP requests.
    builder.Services.AddHttpClient();

    // Register application-specific services with their interfaces for dependency injection.
    builder.Services.AddScoped<ILocationService, LocationService>(); // Scoped: instance per request (or Blazor circuit).
    builder.Services.AddScoped<IMachineService, MachineService>();
    builder.Services.AddScoped<IAppUserService, AppUserService>();
    builder.Services.AddScoped<IExternalApplicationService, ExternalApplicationService>();
    builder.Services.AddSingleton<IIconService, IconService>(); // Singleton: single instance for the application lifetime.

    // Add API Explorer and Swagger Gen services
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "UPortal API", Version = "v1" });

        // Configure Swagger to use XML comments.
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, xmlFile);
        if (System.IO.File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }

        // Optional: Add security definitions for Azure AD (JWT Bearer)
        // This helps Swagger UI to send the token correctly.
        /*
        c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuth2Flows
            {
                Implicit = new OpenApiOAuth2Flow // Or AuthorizationCode, depending on your Azure AD setup for APIs
                {
                    AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                    TokenUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        // Define scopes your API expects, e.g., "api://<your-api-client-id>/access_as_user"
                        // These need to match the scopes defined in Azure AD for your application.
                        // Example: { "api://your-client-id/user_impersonation", "Access UPortal API" }
                    }
                }
            }
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                },
                new[] { /* list required scopes here, e.g., "api://your-client-id/user_impersonation" */ }
            }
        });
        */
    });

    var app = builder.Build();

    // Seed initial data into the database. This is often done at startup.
    await DataSeeder.SeedAsync(app);

    // Configure the HTTP request pipeline (middleware).
    if (!app.Environment.IsDevelopment())
    {
        // For non-development environments, configure a user-friendly error page.
        Log.Information("Configuring production exception handler to use /Error page.");
        app.UseExceptionHandler("/Error", createScopeForErrors: true); // Handles unhandled exceptions.
        // Use HSTS (HTTP Strict Transport Security) for enhanced security.
        // The default HSTS value is 30 days. Consider adjusting for production.
        app.UseHsts();
    }
  else // Development environment specific configurations
  {
    Log.Information("Configuring development environment settings.");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UPortal API V1");
        // Optional: Configure OAuth2 for Swagger UI
        // c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]); // Client ID of this API registration or a dedicated Swagger UI client
        // c.OAuthAppName("UPortal API - Swagger UI");
        // c.OAuthUsePkce(); // If using PKCE
    });
  }

    // Redirect HTTP requests to HTTPS.
    app.UseHttpsRedirection();

    // Enable serving static files (e.g., CSS, JavaScript, images).
    app.UseStaticFiles();
    // Add antiforgery middleware to protect against Cross-Site Request Forgery (CSRF) attacks.
    app.UseAntiforgery();

    // Enable authentication and authorization middleware.
    app.UseAuthentication(); // Attempts to authenticate the user.
    app.UseAuthorization();  // Verifies if the authenticated user is authorized to access resources.

    // Map Blazor components to endpoints, enabling interactive server-side rendering for the main 'App' component.
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Start the application.
    app.Run();
}
catch (Exception ex) // Catch any fatal exceptions during startup.
{
    Log.Fatal(ex, "Application start-up failed");
}
finally // Ensure Serilog is closed and flushed on application exit, regardless of success or failure.
{
    Log.CloseAndFlush();
}
