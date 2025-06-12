using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using UPortal.Components;
using UPortal.Data;
using UPortal.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

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
