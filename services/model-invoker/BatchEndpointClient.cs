using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace MetroMaintenance.ModelInvoker;

public sealed class BatchEndpointClient
{
    private static readonly HttpClient HttpClient = new();
    private static readonly TokenCredential Credential = new DefaultAzureCredential();

    public async Task<string> SubmitAsync(
        string endpointUrl,
        string inputName,
        string inputUri,
        string outputUri,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(endpointUrl, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException("The configured AML endpoint URL is not an absolute URL.");
        }

        var token = await Credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://ml.azure.com/.default" }),
            cancellationToken);
        var payload = new
        {
            properties = new
            {
                InputData = new Dictionary<string, object>
                {
                    [inputName] = new { JobInputType = "UriFile", Uri = inputUri }
                },
                OutputData = new Dictionary<string, object>
                {
                    ["score"] = new { JobOutputType = "UriFile", Uri = outputUri }
                }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"AML batch submission failed with {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }
        return body;
    }
}
