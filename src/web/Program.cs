using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services.Interfaces;
using snipsnop.Web;

using Settings = Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddOptions<Settings.Configuration>().Bind(builder.Configuration.GetSection(nameof(Settings.Configuration)));

builder.Services.AddSingleton<CosmosClient>((serviceProvider) =>
{
    IOptions<Settings.Configuration> configurationOptions = serviceProvider.GetRequiredService<IOptions<Settings.Configuration>>();
    Settings.Configuration configuration = configurationOptions.Value;

    // <create_client>
    CosmosClient client = new(
        accountEndpoint: configuration.AzureCosmosDB.Endpoint,
        tokenCredential: new DefaultAzureCredential()
    );
    // </create_client>
    return client;
});

builder.Services.AddTransient<ISnipService, SnipService>();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<snipsnop.Web.Components.App>().AddInteractiveServerRenderMode();

app.Run();
