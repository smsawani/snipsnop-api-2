using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Settings = Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models.Settings;

var builder = WebApplication.CreateBuilder(args);

var hostBuilder = new HostBuilder();
hostBuilder.ConfigureFunctionsWorkerDefaults();
hostBuilder.ConfigureServices(services =>
{
    services.AddApplicationInsightsTelemetryWorkerService();
    services.ConfigureFunctionsApplicationInsights();
    services.AddOptions<Settings.Configuration>().Bind(builder.Configuration.GetSection(nameof(Settings.Configuration)));
    services.AddSingleton<CosmosClient>((serviceProvider) =>
    {
        IOptions<Settings.Configuration> configurationOptions = serviceProvider.GetRequiredService<IOptions<Settings.Configuration>>();
        Settings.Configuration configuration = configurationOptions.Value;

        // <create_client>
        CosmosClient client = new(
            accountEndpoint: "https://cosmos-db-nosql-xkcua7mhixbbs.documents.azure.com:443/", //configuration.AzureCosmosDB.Endpoint,
            tokenCredential: new DefaultAzureCredential()
        );
        // </create_client>
        return client;
    });
});

await hostBuilder.Build().RunAsync();
