using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services.Interfaces;
using System.Configuration;
using System.Net;
using Settings = Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models.Settings;

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
        //Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);
        Database database = client.GetDatabase("snipsnop");
        var response = req.CreateResponse(HttpStatusCode.OK);

        var item = await req.ReadFromJsonAsync<SnipData>();

        database = await database.ReadAsync();
        response.WriteString($"Get database:\t{database.Id}");

        //Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);
        Container container = database.GetContainer("snips");

        container = await container.ReadContainerAsync();
        response.WriteString($"Get container:\t{container.Id}");

        ItemResponse<SnipData> dbResponse = await container.UpsertItemAsync<SnipData>(
            item: item,
            partitionKey: new PartitionKey(item.userId)
        );

        response.WriteString($"Upserted item:\t{dbResponse.Resource}");
        response.WriteString($"Status code:\t{dbResponse.StatusCode}");
        response.WriteString($"Request charge:\t{dbResponse.RequestCharge:0.00}");

        var query = new QueryDefinition(
                query: "SELECT * FROM snips s"
            );

        using FeedIterator<SnipData> feed = container.GetItemQueryIterator<SnipData>(
            queryDefinition: query
        );

        response.WriteString($"Ran query:\n{query.QueryText}\n\n\n");

        List<SnipData> items = new();
        double requestCharge = 0d;
        while (feed.HasMoreResults)
        {
            FeedResponse<SnipData> dbResponse1 = await feed.ReadNextAsync();
            foreach (SnipData snip in dbResponse1)
            {
                items.Add(snip);
            }
            requestCharge += dbResponse1.RequestCharge;
        }

        foreach (var snip in items)
        {
            response.WriteString($"Found item:\t{snip.trackName}\t[{snip.userId}]\t{snip.id}\n");
        }
        response.WriteString($"\n\n\n\nRequest charge:\t{requestCharge:0.00}");

        return response;
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
        //Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);
        Database database = client.GetDatabase("snipsnop");
        var response = req.CreateResponse(HttpStatusCode.OK);

        database = await database.ReadAsync();
        response.WriteString($"Get database:\t{database.Id}\n");

        //Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);
        Container container = database.GetContainer("snips");

        container = await container.ReadContainerAsync();
        response.WriteString($"Get container:\t{container.Id}\n");

        var query = new QueryDefinition(
                query: "SELECT * FROM snips s"
            );

        using FeedIterator<SnipData> feed = container.GetItemQueryIterator<SnipData>(
            queryDefinition: query
        );

        response.WriteString($"Ran query:\n{query.QueryText}\n\n\n");

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

        foreach (var item in items)
        {
            response.WriteString($"Found item:\t{item.trackName}\t[{item.userId}]\n");
        }
        response.WriteString($"\n\n\n\nRequest charge:\t{requestCharge:0.00}");


        return response;
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
        //Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);
        Database database = client.GetDatabase("snipsnop");
        var response = req.CreateResponse(HttpStatusCode.OK);

        database = await database.ReadAsync();
        response.WriteString($"Get database:\t{database.Id}");

        //Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);
        Container container = database.GetContainer("snips");

        container = await container.ReadContainerAsync();
        response.WriteString($"Get container:\t{container.Id}");

        return response;
    }
}
