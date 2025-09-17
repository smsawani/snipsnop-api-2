using System.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models;
using Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;


using Settings = Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models.Settings;

namespace Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services;

public sealed class DemoService(
    CosmosClient client,
    IOptions<Settings.Configuration> configurationOptions
) : IDemoService
{
    private readonly Settings.Configuration configuration = configurationOptions.Value;

    public string GetEndpoint() => $"{client.Endpoint}";

    [Function("saveSnip")]
    public async Task RunAsync(Func<string, Task> writeOutputAync)
    {
        Database database = client.GetDatabase(configuration.AzureCosmosDB.DatabaseName);

        database = await database.ReadAsync();
        await writeOutputAync($"Get database:\t{database.Id}");

        Container container = database.GetContainer(configuration.AzureCosmosDB.ContainerName);

        container = await container.ReadContainerAsync();
        await writeOutputAync($"Get container:\t{container.Id}");

        {
            SnipData item = new(
                id: "aaaaaaaa-0000-1111-2222-bbFFFFFFFFb",
                userId: "111330695976541866581",
                startTime: "05:22",
                endTime: "09:44",
                lastModified: "2025-09-17T10:12:32.622Z",
                trackId: 1000720287429,
                episodeUrl: "https://traffic.megaphone.fm/APO7451021879.mp3",
                trackName: "Episode 1066",
                collectionName: "COCOCOCO Uhh Yeah Dude",
                artworkUrl: "https://is1-ssl.mzstatic.com/image/thumb/Podcasts221/v4/5d/90/fb/5d90fbd1-5941-260d-083d-96cf86e26dcc/mza_2505598615688257128.jpg/600x600bb.jpg"
            );

            ItemResponse<SnipData> response = await container.UpsertItemAsync<SnipData>(
                item: item,
                partitionKey: new PartitionKey("111330695976541866581")
            );

            await writeOutputAync($"Upserted item:\t{response.Resource}");
            await writeOutputAync($"Status code:\t{response.StatusCode}");
            await writeOutputAync($"Request charge:\t{response.RequestCharge:0.00}");
        }

        {
            SnipData item = new(
                id: "aaaaaaaa-3333-4444-5555-dddddddddddd",
                userId: "222441806087652977692",
                startTime: "15:30",
                endTime: "18:45",
                lastModified: "2025-09-17T11:15:45.889Z",
                trackId: 1000720287430,
                episodeUrl: "https://traffic.megaphone.fm/APO7451021879.mp3",
                trackName: "Episode 1067",
                collectionName: "DODODODODOD Comedy Bang Bang",
                artworkUrl: "https://is1-ssl.mzstatic.com/image/thumb/Podcasts125/v4/8d/a1/fc/8da1fc12-6852-371e-094f-97de97e36ddd/mza_3506798726899368329.jpg/600x600bb.jpg"                        
            );

            ItemResponse<SnipData> response = await container.UpsertItemAsync<SnipData>(
                item: item,
                partitionKey: new PartitionKey("222441806087652977692")
            );
            await writeOutputAync($"Upserted item:\t{response.Resource}");
            await writeOutputAync($"Status code:\t{response.StatusCode}");
            await writeOutputAync($"Request charge:\t{response.RequestCharge:0.00}");
        }

        {
            ItemResponse<SnipData> response = await container.ReadItemAsync<SnipData>(
                id: "aaaaaaaa-0000-1111-2222-bbbbbbbbbbbb",
                partitionKey: new PartitionKey("111330695976541866581")
            );

            await writeOutputAync($"Read item userId:\t{response.Resource.userId}");
            await writeOutputAync($"Read item:\t{response.Resource}");
            await writeOutputAync($"Status code:\t{response.StatusCode}");
            await writeOutputAync($"Request charge:\t{response.RequestCharge:0.00}");
        }

        {
            var query = new QueryDefinition(
                query: "SELECT * FROM snips s WHERE s.userId = @userId"
            )
                .WithParameter("@userId", "111330695976541866581");

            using FeedIterator<SnipData> feed = container.GetItemQueryIterator<SnipData>(
                queryDefinition: query
            );

            await writeOutputAync($"Ran query:\t{query.QueryText}");

            List<SnipData> items = new();
            double requestCharge = 0d;
            while (feed.HasMoreResults)
            {
                FeedResponse<SnipData> response = await feed.ReadNextAsync();
                foreach (SnipData item in response)
                {
                    items.Add(item);
                }
                requestCharge += response.RequestCharge;
            }

            foreach (var item in items)
            {
                await writeOutputAync($"Found item:\t{item.trackName}\t[{item.userId}]");
            }
            await writeOutputAync($"Request charge:\t{requestCharge:0.00}");
        }
    }
}
