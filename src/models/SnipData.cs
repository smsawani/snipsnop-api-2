namespace Microsoft.Samples.Cosmos.NoSQL.Quickstart.Models;

public record EpisodeData(
    long trackId,
    string episodeUrl,
    string trackName,
    string collectionName,
    string artworkUrl
);

public record SnipData(
    string id,
    string trackId,
    string startTime,
    string endTime,
    EpisodeData episodeData,
    string lastModified,
    string storageKey,
    string userId
);
