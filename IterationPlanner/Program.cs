using IterationPlanner.Components;
using IterationPlanner;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables().AddCommandLine(args);

// ASP.NET configuration also accepts appsettings.{Environment}.json,
// environment variables and command-line overrides.
builder.Services.AddOptions<AzureDevOpsOptions>()
    .BindConfiguration(AzureDevOpsOptions.SectionName)
    .Validate(o => string.IsNullOrWhiteSpace(o.CollectionUrl) ||
        (Uri.TryCreate(o.CollectionUrl, UriKind.Absolute, out var uri) &&
         (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) &&
         string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) &&
         string.IsNullOrEmpty(uri.Fragment)),
        "AzureDevOps:CollectionUrl must be an HTTP(S) collection URL without credentials, query or fragment.")
    .Validate(o => System.Text.RegularExpressions.Regex.IsMatch(o.ApiVersion ?? "", @"^\d+\.\d+$"),
        "AzureDevOps:ApiVersion must be a numeric API version, for example 7.0.")
    .Validate(o => o.TimeoutSeconds is >= 1 and <= 600,
        "AzureDevOps:TimeoutSeconds must be between 1 and 600.")
    .ValidateOnStart();

builder.Services.AddHttpClient<AzureIterationsClient>().ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Logging.AddFilter("System.Net.Http.HttpClient.AzureIterationsClient", LogLevel.None);
builder.Services.AddHttpClient<AzureProjectsClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Logging.AddFilter("System.Net.Http.HttpClient.AzureProjectsClient", LogLevel.None);
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<UiLanguage>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();




