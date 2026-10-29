# Model invoker

This C# Azure Functions app is the sanitised equivalent of the three
model callers used in the research deployment:

| Public function | Original project | Trigger | Purpose |
|---|---|---|---|
| `InvokeOutlierModel` | `AzureOutlierModelCaller` | New KPI blob | Submit outlier-classification batch job |
| `InvokeLubricationModel` | `AzureTrainClusterModelCaller` | New KPI blob | Submit the paper-facing lubrication model job |
| `InvokeSensorHealthModel` | `AzureSensorsModelCaller` | Daily timer | Submit sensor-health batch job |

The original callers manually requested a service-principal token and embedded
endpoint/datastore names. This version sends the same AML batch-job payload but
uses `DefaultAzureCredential`, configurable resource names, and explicit HTTP
error propagation. In Azure, assign the Function App managed identity the AML
permissions required to invoke the endpoints. Locally, authenticate with Azure
CLI, Visual Studio/VS Code, or the standard `AZURE_TENANT_ID`,
`AZURE_CLIENT_ID`, and `AZURE_CLIENT_SECRET` environment variables.

The endpoint input name must match the deployed AML batch endpoint contract.
The historical deployment used the copied sample name `mnistinput`; the public
default is the clearer `input`, and can be changed per model in settings.

## What you need in Azure for this to work

This Function App does not create AML resources. To submit real jobs you must already have:

- an Azure Machine Learning workspace
- one or more **AML batch endpoints** deployed (outlier, lubrication, sensor-health)
- an identity that can invoke those endpoints

In Azure, the recommended identity is the Function App’s managed identity. Grant it the minimum role(s) needed to invoke batch endpoints in your AML workspace.

This repository intentionally does not ship an endpoint deployment template because endpoint deployment is billable and organisation-specific.

## Local build and execution

```powershell
Copy-Item local.settings.example.json local.settings.json
dotnet restore
dotnet build
func start
```

Replace AML placeholders in the untracked `local.settings.json`. Start Azurite
for blob triggers. A real AML workspace and authenticated identity are required
to submit jobs; local execution does not emulate an AML endpoint.

## Common newcomer pitfalls

- A successful `dotnet build` only proves the Function App compiles; it does not validate your AML endpoints.
- The `OutlierEndpointUrl` / `LubricationEndpointUrl` / `SensorHealthEndpointUrl` values must be the AML endpoint `.../jobs` URL.
- The configured `*InputName` must match the endpoint’s expected input port name.
- Storage triggers require Azurite or an Azure Storage account and the expected container names.

Do not commit `local.settings.json` or client secrets.
