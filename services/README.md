# C# Azure Functions services

This directory contains the implemented service layer described in the paper.
Both applications are C#, matching the research deployment.

If you are new to the repository and only want to reproduce preprocessing, start with the cloud-independent KPI local runner in `kpi-calculator`.

| Service | Trigger | Responsibility |
|---|---|---|
| [`kpi-calculator`](kpi-calculator) | New raw `.mat` blob | Calibrate signals and write 82 KPI columns |
| [`model-invoker`](model-invoker) | New KPI blob or daily timer | Submit outlier, lubrication, and sensor-health AML batch jobs |

Open [`metro-services.code-workspace`](metro-services.code-workspace) in VS
Code. Recommended extensions are declared in that workspace.

The applications can be restored and built without Azure:

```powershell
dotnet build kpi-calculator/KpiCalculator.csproj
dotnet build model-invoker/ModelInvoker.csproj
```

The KPI numerical code can also process a local sample directly, without
Azure Functions or Azurite. Single file:

```powershell
dotnet run --project kpi-calculator/local-runner -- `
  ../sample-data/raw/sample_0001.mat `
  ../sample-data/kpi/sample_0001.csv
```

Or all shared samples at once (from the repository root):

```powershell
dotnet run --project services/kpi-calculator/local-runner -- --samples
```

Running the triggers locally requires Azure Functions Core Tools 4. Blob
triggers additionally require Azurite or an Azure Storage account. Actual
model submission requires authenticated access to configured AML endpoints.

The projects target .NET 6 to preserve compatibility with the implementation
used in the study. .NET 6 is end-of-support; reusers starting a production
system should migrate to the current .NET isolated-worker model.
