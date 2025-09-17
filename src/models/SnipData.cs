namespace Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models;


public record SnipData(
    string id,
    string userId,
    string startTime,
    string endTime,
    string lastModified,
    long trackId,
    string episodeUrl,
    string trackName,
    string collectionName,
    string artworkUrl
);
