using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services.Interfaces;
using System.Net;
using Settings = Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models.Settings;
using Newtonsoft.Json;

namespace Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services;

public sealed class SaveSnip(
    CosmosClient client,
    IOptions<Settings.Configuration> configurationOptions
) : ISnipService
{
    private readonly Settings.Configuration configuration = configurationOptions.Value;
    public string GetEndpoint() => $"{client.Endpoint}";

    [Function("saveSnip")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            //Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);
            Database database = client.GetDatabase("snipsnop");
            var response = req.CreateResponse(HttpStatusCode.OK);

            var item = await req.ReadFromJsonAsync<SnipData>();

            database = await database.ReadAsync();

            //Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);
            Container container = database.GetContainer("snips");

            container = await container.ReadContainerAsync();

            ItemResponse<SnipData> dbResponse = await container.UpsertItemAsync<SnipData>(
                item: item,
                partitionKey: new PartitionKey(item.userId)
            );

            return response;
        }
        catch(Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.WriteString($"Error: {ex.Message}\n{ex.StackTrace}");
            return response;
        }
        
    }
}

public sealed class LoadSnips(
    CosmosClient client,
    IOptions<Settings.Configuration> configurationOptions
) : ISnipService
{
    private readonly Settings.Configuration configuration = configurationOptions.Value;
    public string GetEndpoint() => $"{client.Endpoint}";

    [Function("loadSnips")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            //Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);
            Database database = client.GetDatabase("snipsnop");
            var response = req.CreateResponse(HttpStatusCode.OK);

            database = await database.ReadAsync();

            //Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);
            Container container = database.GetContainer("snips");
            container = await container.ReadContainerAsync();

            var query = new QueryDefinition(
                    query: "SELECT * FROM snips s"
                );

            using FeedIterator<SnipData> feed = container.GetItemQueryIterator<SnipData>(
                queryDefinition: query
            );

            List<SnipData> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<SnipData> dbResponse = await feed.ReadNextAsync();
                foreach (SnipData item in dbResponse)
                {
                    items.Add(item);
                }
                requestCharge += dbResponse.RequestCharge;
            }

            response.WriteString(JsonConvert.SerializeObject(items, Formatting.Indented));
            return response;
        }
        catch(Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.WriteString($"Error: {ex.Message}\n{ex.StackTrace}");
            return response;
        }
    }
}

public sealed class DeleteSnip(
    CosmosClient client,
    IOptions<Settings.Configuration> configurationOptions
) : ISnipService
{
    private readonly Settings.Configuration configuration = configurationOptions.Value;
    public string GetEndpoint() => $"{client.Endpoint}";

    [Function("deleteSnip")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData req)
    {
        try
        {
            var item = await req.ReadFromJsonAsync<SnipData>();

            //Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);
            Database database = client.GetDatabase("snipsnop");
            var response = req.CreateResponse(HttpStatusCode.OK);

            database = await database.ReadAsync();

            //Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);
            Container container = database.GetContainer("snips");

            var deleteResponse = await container.DeleteItemAsync<SnipData>(item.id, new PartitionKey(item.userId));
     
            return response;
        }
        catch(Exception ex)
        {
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            response.WriteString($"Error: {ex.Message}\n{ex.StackTrace}");
            return response;
        }
    }
}
