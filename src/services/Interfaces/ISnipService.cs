using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;


namespace Microsoft.Samples.Cosmos.NoSQL.Quickstart.Services.Interfaces;

public interface ISnipService
{
    Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req);
    
    string GetEndpoint();
}