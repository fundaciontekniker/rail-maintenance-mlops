using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace MetroMaintenance.ModelInvoker;

public sealed class ModelInvokerFunctions
{
    private readonly BatchEndpointClient _client = new();

    [FunctionName("InvokeOutlierModel")]
    public Task InvokeOutlierAsync(
        [BlobTrigger("%KpiContainer%/{name}", Connection = "DataStorage")]
        Stream blob,
        string name,
        ILogger log) => InvokeForBlobAsync(
            "outlier",
            name,
            blob.Length,
            Required("OutlierEndpointUrl"),
            Required("OutlierInputName"),
            Required("OutlierOutputPath"),
            log);

    [FunctionName("InvokeLubricationModel")]
    public Task InvokeLubricationAsync(
        [BlobTrigger("%KpiContainer%/{name}", Connection = "DataStorage")]
        Stream blob,
        string name,
        ILogger log) => InvokeForBlobAsync(
            "lubrication",
            name,
            blob.Length,
            Required("LubricationEndpointUrl"),
            Required("LubricationInputName"),
            Required("LubricationOutputPath"),
            log);

    [FunctionName("InvokeSensorHealthModel")]
    public async Task InvokeSensorHealthAsync(
        [TimerTrigger("%SensorHealthSchedule%")] TimerInfo timer,
        ILogger log)
    {
        var endpoint = Required("SensorHealthEndpointUrl");
        var inputName = Required("SensorHealthInputName");
        var inputUri = Required("SensorHealthInputUri");
        var outputUri = ExpandDate(Required("SensorHealthOutputUri"), DateTime.UtcNow);
        log.LogInformation("Submitting scheduled sensor-health job. Input: {InputUri}", inputUri);
        var response = await _client.SubmitAsync(endpoint, inputName, inputUri, outputUri);
        log.LogInformation("Sensor-health job accepted: {Response}", response);
    }

    private async Task InvokeForBlobAsync(
        string model,
        string blobName,
        long blobLength,
        string endpoint,
        string inputName,
        string outputPath,
        ILogger log)
    {
        var inputDatastore = Required("InputDatastore");
        var outputDatastore = Required("OutputDatastore");
        var inputUri = $"azureml://datastores/{inputDatastore}/paths/{blobName}";
        var outputUri = $"azureml://datastores/{outputDatastore}/paths/{outputPath.Trim('/')}/{blobName}";
        log.LogInformation(
            "Submitting {Model} model for {BlobName} ({BlobLength} bytes)",
            model,
            blobName,
            blobLength);
        var response = await _client.SubmitAsync(endpoint, inputName, inputUri, outputUri);
        log.LogInformation("{Model} job accepted: {Response}", model, response);
    }

    private static string Required(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Required setting '{name}' is missing.");

    private static string ExpandDate(string value, DateTime timestamp) => value
        .Replace("{yyyy}", timestamp.ToString("yyyy"), StringComparison.Ordinal)
        .Replace("{MM}", timestamp.ToString("MM"), StringComparison.Ordinal)
        .Replace("{dd}", timestamp.ToString("dd"), StringComparison.Ordinal);
}
