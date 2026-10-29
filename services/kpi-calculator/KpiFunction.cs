using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MetroMaintenance.KpiCalculator;

public sealed class KpiFunction
{
    private readonly IndicatorManager _indicatorManager = new();

    [FunctionName("CalculateKpis")]
    public async Task RunAsync(
        [BlobTrigger("%RawContainer%/{name}", Connection = "DataStorage")]
        Stream input,
        string name,
        ILogger log)
    {
        log.LogInformation("Processing {Name} ({Length} bytes)", name, input.Length);

        var outputConnection = Required("KpiStorage");
        var containerName = Required("KpiContainer");
        var serviceClient = new BlobServiceClient(outputConnection);
        var containerClient = serviceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        await using var csv = new MemoryStream();
        await _indicatorManager.WriteKpisAsync(input, Path.GetFileName(name), csv);
        csv.Position = 0;

        var outputName = $"{Path.GetFileNameWithoutExtension(name)}.csv";
        var outputBlob = containerClient.GetBlobClient(outputName);
        await outputBlob.UploadAsync(csv, overwrite: true);
        log.LogInformation("KPI extraction completed. Output: {Container}/{OutputName}", containerName, outputName);
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Required setting '{name}' is missing.");
}
