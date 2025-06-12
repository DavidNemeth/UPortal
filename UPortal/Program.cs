using Microsoft.EntityFrameworkCore;
using Microsoft.FluentUI.AspNetCore.Components;
using UPortal.Components;
using UPortal.Data;
using UPortal.Services;

var builder = WebApplication.CreateBuilder(args);

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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
