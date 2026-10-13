using ActivityMonitor.Admin.Components;
using ActivityMonitor.Admin.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MonitoringApiOptions>(
    builder.Configuration.GetSection(MonitoringApiOptions.SectionName));

builder.Services.AddHttpClient<MonitoringApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MonitoringApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(Math.Max(options.ScreenshotWaitSeconds + 10, 30));
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
