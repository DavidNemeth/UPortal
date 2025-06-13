using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using UPortal.Components;
using UPortal.Data;
using UPortal.Services;
using UPortal.HelperServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events ??= new OpenIdConnectEvents();
    options.Events.OnTokenValidated = async context =>
    {
        if (context.Principal != null)
        {
            // Resolve the IAppUserService from the HttpContext.RequestServices
            // Using GetService to avoid throwing if not found, though it should be registered.
            // GetRequiredService would throw, which might be desirable in some cases.
            var userService = context.HttpContext.RequestServices.GetService<IAppUserService>();
            if (userService != null)
            {
                await userService.CreateOrUpdateUserFromAzureAdAsync(context.Principal);
            }
            // else: Log that userService was not found, if necessary
        }
        // To ensure other handlers are not overridden if they exist (though usually we initialize new OpenIdConnectEvents)
        // await Task.CompletedTask; // Not strictly necessary if the method signature is Task and no other async ops follow
    };
});

// Optional: If you want to call protected APIs from your web app
// .EnableTokenAcquisitionToCallDownstreamApi(builder.Configuration.GetSection("AzureAd:Scopes").Get<string[]>())
// .AddMicrosoftGraph(builder.Configuration.GetSection("Graph")) // Example for Graph API
// .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    // By default, all authenticated users are authorized.
    // This policy ensures that any unauthenticated access attempt will trigger the OIDC challenge.
    options.FallbackPolicy = options.DefaultPolicy;
});

// For Blazor Server, ensure Microsoft Identity UI is added for handling redirects correctly
// and providing default pages for sign-in, sign-out.
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// To allow creating DbContext instances on demand in other services
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFluentUIComponents();
builder.Services.AddHttpClient();

builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IMachineService, MachineService>();
builder.Services.AddScoped<IAppUserService, AppUserService>();
builder.Services.AddScoped<IExternalApplicationService, ExternalApplicationService>(); // Ensure this is present once
builder.Services.AddSingleton<IIconService, IconService>();

var app = builder.Build();

await DataSeeder.SeedAsync(app);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
