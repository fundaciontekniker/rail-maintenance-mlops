# KPI calculator (C#)

This service converts one MATLAB acquisition into one semicolon-delimited KPI
file. It is a cleaned C# version of `AzureFunctionKPICalculator` from the
research workspace and retains its MATLAB contract, calibration constants,
event threshold, output column names, and Blob-triggered architecture.

Sanitisation changes are recorded in [`../../docs/provenance.md`](../../docs/provenance.md): unique
temporary files, invariant numeric formatting, correct frequency selection,
empty pseudonymised timestamps, and one immutable output CSV per input.

## Run directly on a local file

Only the .NET SDK is required. From `services/kpi-calculator`:

```powershell
dotnet run --project local-runner -- `
  ../../sample-data/raw/sample_0001.mat `
  ../../sample-data/kpi/sample_0001.csv
```

This is the fastest reproducibility path. It invokes the same
`IndicatorManager` used by the Azure Function and makes no network calls. The
expected output has six rows and 82 columns.

## Generate KPI tables for all shared samples

From the repository root:

```powershell
dotnet run --project services/kpi-calculator/local-runner -- --samples
```

This reads all `.mat` files under `sample-data/raw/` and writes one KPI CSV per input under `sample-data/kpi/`.

## Run as an Azure Function locally

Prerequisites:

- .NET SDK capable of building `net6.0`.
- Azure Functions Core Tools 4.
- Azurite or an Azure Storage account.
- VS Code C# and Azure Functions extensions, if using the debugger.

From `services/kpi-calculator`:

```powershell
Copy-Item local.settings.example.json local.settings.json
dotnet restore
dotnet build
func start
```

Start Azurite and create the `raw-signals` and `kpis` containers. Upload a
`.mat` file to `raw-signals`; `CalculateKpis` explicitly creates/opens the KPI
container through `BlobServiceClient` and uploads `<blob-name>.csv` through a
`BlobClient`, matching the storage-access pattern used in the research
Function. Input storage, output storage, and container names are app settings.

`local.settings.json` is ignored and must never be committed.

## Input contract

Each public MATLAB file contains exactly six accelerometer arrays. The sampling
rate is fixed at 10000 Hz in the study implementation and is not stored as an
additional variable in the sanitised sample files.

| Variables | Shape range in shared sample | Sampling rate |
|---|---:|---:|
| `Acceleration_1` ... `Acceleration_6` | `1 x 204000` to `1 x 308000` | 10000 Hz |

Production filenames beginning `YYYYMMDD_HHMMSS_mmm` provide the acquisition
timestamp. Pseudonymised sample names produce an empty `datetime` field.

## Output contract

There is one row per accelerometer. Feature names are written in English for
international reuse (for example `Mode`, `TrimmedMean`, `InterquartileRange`,
`Skewness`, `Kurtosis`). See [`../../docs/data-dictionary.md`](../../docs/data-dictionary.md).

Calibration constants and the event threshold are installation-specific and
must be recalibrated before reuse with other transducers.
