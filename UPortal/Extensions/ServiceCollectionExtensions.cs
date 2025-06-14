using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi.Models;
using UPortal.Data;
using UPortal.HelperServices;
using UPortal.Security;
using UPortal.Services;

namespace UPortal.Extensions;

/// <summary>
/// Provides extension methods for configuring services in the IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures authentication (Azure AD, Cookies) and authorization (Permissions-based policies).
    /// </summary>
    public static IServiceCollection AddUportalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure authentication with Azure AD using OpenID Connect
        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"));

        // Configure the cookie settings
        services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.Cookie.Name = ".Auth.UPortal";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.Domain = configuration["CookieSettings:Domain"];
        });

        // Configure Data Protection to persist keys
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(configuration["DataProtectionSettings:KeyPath"]))
            .SetApplicationName("UPortal");

        // Add the custom authorization handler for permission-based policies
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        // ** REFACTORED AUTHORIZATION **
        // Register the custom policy provider as a singleton.
        // This provider will create permission-based policies on-the-fly.
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Configure the fallback authorization policy. All other policies are now handled dynamically.
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        // Add event to create/update local user from Azure AD principal upon successful login
        services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
        {
            options.Events.OnTokenValidated = async context =>
            {
                if (context.Principal == null) return;

                var userService = context.HttpContext.RequestServices.GetRequiredService<IAppUserService>();
                await userService.CreateOrUpdateUserFromAzureAdAsync(context.Principal);
            };
        });

        return services;
    }

    /// <summary>
    /// Configures UI and presentation layer services (Blazor, FluentUI, Razor Pages).
    /// </summary>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        services.AddFluentUIComponents();

        services.AddRazorPages()
            .AddMicrosoftIdentityUI()
            .AddRazorPagesOptions(options =>
            {
                // Allow anonymous access to the login/logout pages
                options.Conventions.AllowAnonymousToAreaFolder("MicrosoftIdentity", "/Account");
            });

        return services;
    }

    /// <summary>
    /// Registers application-specific business logic and data services.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Entity Framework Core DbContext
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Register application services
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IMachineService, MachineService>();
        services.AddScoped<IAppUserService, AppUserService>();
        services.AddScoped<IExternalApplicationService, ExternalApplicationService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<IIconService, IconService>();

        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Configures Swagger/OpenAPI for API documentation.
    /// </summary>
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "UPortal API", Version = "v1" });

            // Use XML comments for documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Define cookie authentication for Swagger UI
            c.AddSecurityDefinition("CookieAuth", new OpenApiSecurityScheme
            {
                Name = ".Auth.UPortal",
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Cookie,
                Scheme = "Cookie",
                Description = "Cookie-based authentication. Login via the Blazor UI first."
            });

            // Make cookie authentication a global requirement
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "CookieAuth" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configures Cross-Origin Resource Sharing (CORS) policies.
    /// </summary>
    public static IServiceCollection AddUportalCors(this IServiceCollection services, IWebHostEnvironment environment, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: "DefaultCorsPolicy", policy =>
            {
                string[] allowedOrigins;
                if (environment.IsDevelopment())
                {
                    allowedOrigins = new[] { "https://dev.uportal.local:7293", "http://dev.uportal.local:5053" };
                }
                else
                {
                    allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                                     ?? new[] { "https://uportal.yourcompany.com" }; // Fallback
                }

                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}