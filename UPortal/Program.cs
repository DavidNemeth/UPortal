using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
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
using Microsoft.AspNetCore.Authorization; // Added for IAuthorizationHandler
using UPortal.Security; // Added for PermissionHandler and PermissionRequirement

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

    // Configure forwarded headers to handle HTTPS termination from a reverse proxy
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
            Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    });

    // Configure authentication with Azure AD using OpenID Connect.
    // Let AddMicrosoftIdentityWebApp handle the primary setup, including adding the cookie handler.
    builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

    // Now, configure the cookie settings that AddMicrosoftIdentityWebApp created.
    // This is the correct way to customize the cookie without causing conflicts.
    builder.Services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = ".Auth.UPortal";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Requires HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;

        // Read domain from configuration
        options.Cookie.Domain = builder.Configuration["CookieSettings:Domain"];
    });

    // Configure Data Protection
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtectionSettings:KeyPath"]))
        .SetApplicationName("UPortal");

    // Configure OpenID Connect options, specifically the OnTokenValidated event.
    builder.Services.Configure<OpenIdConnectOptions>("AzureAd", options =>
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
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        // Define permissions that will be used in policies
        // This list should match the permissions seeded in DataSeeder.cs
        var permissions = new List<string>
        {
            "ManageUsers", "ViewUsers", "EditUsers",
            "ManageRoles", "ViewRoles", "AssignRoles",
            "ManagePermissions", "ViewPermissions",
            "ManageSettings",
            "AccessAdminPages",
            "ViewDashboard",
            "ManageMachines", "ViewMachines",
            "ManageLocations", "ViewLocations",
            "ManageExternalApplications", "ViewExternalApplications"
        };

        foreach (var permission in permissions)
        {
            options.AddPolicy($"Require{permission}Permission", policy =>
                policy.Requirements.Add(new PermissionRequirement(permission)));
        }
    });

    // Add Razor Pages services and Microsoft Identity UI for handling authentication-related UI (login, logout pages).
    builder.Services.AddRazorPages()
     .AddMicrosoftIdentityUI()
     .AddRazorPagesOptions(options =>
     {
         // This convention allows anonymous access to all pages in the /Account/ folder 
         // within the MicrosoftIdentity area, which is where Login, Logout, etc. are.
         options.Conventions.AllowAnonymousToAreaFolder("MicrosoftIdentity", "/Account");
     });

    // Configure Entity Framework Core DbContext.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    // Use AddDbContextFactory for creating DbContext instances on demand, which is good for Blazor Server apps.
    builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString));

    // Add Blazor Server components and enable interactive server-side rendering.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();
    builder.Services.AddControllers();
    // Add Fluent UI Blazor components.
    builder.Services.AddFluentUIComponents();
    // Add HttpClient for making HTTP requests.
    builder.Services.AddHttpClient();

    // Register application-specific services with their interfaces for dependency injection.
    builder.Services.AddScoped<ILocationService, LocationService>(); // Scoped: instance per request (or Blazor circuit).
    builder.Services.AddScoped<IMachineService, MachineService>();
    builder.Services.AddScoped<IAppUserService, AppUserService>();
    builder.Services.AddScoped<IExternalApplicationService, ExternalApplicationService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<IPermissionService, PermissionService>();
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

        // Add Cookie Authentication Security Definition
        c.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
        {
            Name = ".Auth.UPortal", // Must match the cookie name configured in AddCookie()
            Type = SecuritySchemeType.ApiKey, // Using ApiKey to represent a cookie
            In = ParameterLocation.Cookie,
            Scheme = "Cookie", // Descriptive scheme name for Swagger UI
            Description = "Cookie-based authentication. Login via the Blazor UI first, then the cookie will be automatically sent by the browser."
        });

        // Add Global Security Requirement for Cookie Authentication
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "CookieAuth" // Must match the ID used in AddSecurityDefinition
                    }
                },
                new string[] {} // No specific scopes needed for cookie auth in this context
            }
        });
    });

    var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: MyAllowSpecificOrigins,
                          policy =>
                          {
                              if (builder.Environment.IsDevelopment())
                              {
                                  policy.WithOrigins("https://dev.uportal.local:7293",
                                                     "http://dev.uportal.local:5053")
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials();
                              }
                              else
                              {
                                  var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
                                  if (allowedOrigins != null && allowedOrigins.Length > 0)
                                  {
                                      policy.WithOrigins(allowedOrigins)
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                                            .AllowCredentials();
                                  }
                                  else
                                  {
                                      policy.WithOrigins("https://uportal.yourcompany.com") // Placeholder
                                            .AllowAnyHeader()
                                            .AllowAnyMethod()
                                            .AllowCredentials();
                                      Log.Warning("CORS production origins not configured in appsettings.json (Cors:AllowedOrigins). Using placeholder.");
                                  }
                              }
                          });
    });

    var app = builder.Build();
    // This must be one of the first middleware components to run.
    app.UseForwardedHeaders();

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

    app.UseCors(MyAllowSpecificOrigins);

    // Enable authentication and authorization middleware.
    app.UseAuthentication(); // Attempts to authenticate the user.
    app.UseAuthorization();  // Verifies if the authenticated user is authorized to access resources.

    // Map Blazor components to endpoints, enabling interactive server-side rendering for the main 'App' component.
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode()
        .RequireAuthorization();

    app.MapControllers();
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
