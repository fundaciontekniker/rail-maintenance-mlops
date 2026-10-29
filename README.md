# Data-Driven Metro Rail Maintenance: Open Science Artifact

This repository is the **open-science artifact accompanying the paper
"Data-Driven Metro Rail Maintenance within a Sustainability-Oriented MLOps
paradigm."** It publishes a safe, locally executable subset of the code and
data used in that study so that the workflow can be inspected, reused, and
extended.

The artifact is organised in the following structure:

- `sample-data/`: 11 pseudonymised vibration acquisitions (`.mat`) containing `Acceleration_1..6`
- `services/kpi-calculator/`: C# KPI (feature) extraction (local runner + Azure Functions wrapper)
- `pipeline/`: Python modelling entry points + Azure Machine Learning (AML) training pipelines
- `services/model-invoker/`: C# Azure Functions that submit AML batch jobs (requires your own deployed endpoints)
- `sustainability/`: ADEC/YEEC calculation from a CodeCarbon-compatible reconstructed table

## What is included vs not included

This artifact is **sanitised for public release** and designed to be **locally
executable**, so some parts necessarily differ from the private
research/production workspace.

Included here (public):

- refactored, sanitised implementations of the KPI extractor and model-invoker services
- Python modelling entry points for outlier, sensor-health, and lubrication workflows
- Azure Machine Learning and Azure DevOps pipeline definitions (training orchestration)
- documentation of the data/feature contracts and reproducibility limits
- 11 pseudonymised sample acquisitions

Not included here (restricted or unavailable):

- the complete operational dataset, timestamps, maintenance context, and the full manual label corpus used for the paper's reported metrics
- organisation-specific cloud resources (AML workspaces, batch endpoints, identities, networks) and the original deployment configuration

**Important:** the sample runs validate interfaces and execution. They do
**not** reproduce the paper's reported model-performance metrics or the full
emissions experiments; those depend on the restricted dataset and the original
cloud execution context. See [`docs/reproducibility.md`](docs/reproducibility.md)
for claim-level detail and [`sample-data/README.md`](sample-data/README.md) for
the data restriction statement.

For an exact "what changed and why" record, see [`docs/provenance.md`](docs/provenance.md).

## Repository map

| Folder | What to find there | Start here |
|---|---|---|
| [`sample-data`](sample-data) | 11 pseudonymised MATLAB files, plotting notebook | `sample-data/README.md` |
| [`services`](services) | Implemented C# KPI and AML model-invoker Azure Functions | `services/README.md` |
| [`pipeline`](pipeline) | Local model scripts, AML pipeline definitions, and (optional) Azure DevOps reference pipelines | `pipeline/README.md` |
| [`sustainability`](sustainability) | Reconstructed CodeCarbon records and ADEC/YEEC calculation | `sustainability/README.md` |
| [`docs`](docs) | Data dictionary, provenance, and exact reproducibility limits | `docs/reproducibility.md` |

Generated files are written under `artifacts/`. That folder is intentionally
not part of the repository and is ignored by Git.

## Quick start

### 1. Environment setup

Prerequisites:

- .NET SDK capable of building `net6.0` (used by `services/`)
- Python 3.11 (used by `pipeline/` and `sustainability/`)

Create a Python virtual environment and install dependencies (from the
repository root):

```powershell
python -m venv .venv
.venv\Scripts\Activate.ps1
python -m pip install -r requirements.txt
```

On Linux/macOS, activate with `source .venv/bin/activate`.

### 2. Explore the dataset

The artifact includes **11** pseudonymised raw acquisitions (vibration signals)
under [`sample-data/raw/`](sample-data/raw). They illustrate the file contract
and signal characteristics, not the paper's model metrics.

- Dataset notes and schema: [`sample-data/README.md`](sample-data/README.md)
- Plot the raw signals: [`sample-data/plot_raw_signals.ipynb`](sample-data/plot_raw_signals.ipynb)

### 3. Extract KPIs locally (C#)

Generate KPI outputs for all shared samples in one command (no Azure required):

```powershell
dotnet run --project services/kpi-calculator/local-runner -- --samples
```

This reads all `.mat` files under `sample-data/raw/` and writes one KPI CSV per
input under `sample-data/kpi/`. To process a single file, see
[`services/kpi-calculator/README.md`](services/kpi-calculator/README.md).

### 4. Validate the modelling code (sample inputs)

Run a minimal end-to-end example using the **provided** sample KPI table and
outlier labels:

```powershell
python pipeline/src/train_outlier.py --kpi-data sample-data/kpi/kpis.csv --labels sample-data/labels/outlier-labels.csv --output artifacts/models/outlier-samples
python pipeline/src/train_sensor_health.py --kpi-data sample-data/kpi/kpis.csv --output artifacts/models/sensor-health-samples
python -m pytest
```

This sample set is intentionally small. The output is for demonstration and
code validation, not to reproduce the paper's reported metrics.

### 5. Recompute ADEC/YEEC (sustainability)

Recompute the paper-defined ADEC/YEEC from the reconstructed emissions table:

```powershell
python sustainability/analyze_emissions.py
```

### Optional: build the C# Function Apps

```powershell
dotnet build services/kpi-calculator/KpiCalculator.csproj
dotnet build services/model-invoker/ModelInvoker.csproj
```

Continue with [`services/README.md`](services/README.md) for Azure Functions or
[`pipeline/README.md`](pipeline/README.md) for local/AML model execution.

## Data contracts

The artifact is built around stable file contracts, so the workflow can be
reused by swapping only the data sources:

- **Raw acquisition** (`.mat`): six arrays `Acceleration_1..6`.
- **KPI table** (`.csv`): semicolon-delimited, six rows per file (one per accelerometer), 82 columns.
- **Labels**: outlier class per acquisition; lubrication dosage per acquisition (restricted).

The Quick start above walks through each step. The availability table below
lists what is public and what is restricted.

## Availability

| Artifact | Availability | Location |
|---|---|---|
| 11 pseudonymised accelerometer acquisitions | Public sample | [`sample-data/raw`](sample-data/raw) |
| Data schema and restrictions | Public | [`sample-data/README.md`](sample-data/README.md) |
| KPI extraction and Azure Function wrapper | Public | [`services/kpi-calculator`](services/kpi-calculator) |
| AML model invocation Azure Functions | Public | [`services/model-invoker`](services/model-invoker) |
| Random-forest outlier pipeline | Public code; tiny labelled sample subset | [`pipeline`](pipeline) |
| Sensor-health clustering pipeline | Public code; sample KPI table | [`pipeline`](pipeline) |
| XGBoost lubrication-regression pipeline | Public code; requires restricted lubrication schedule | [`pipeline`](pipeline) |
| AML and Azure DevOps pipeline definitions | Public (training orchestration only) | [`pipeline`](pipeline) |
| CodeCarbon ADEC/YEEC analysis | Public paper-reported values and code | [`sustainability`](sustainability) |
| Full operational corpus (raw, timestamps, context) | Restricted | Rationale in [`sample-data/README.md`](sample-data/README.md) |
| Manual anomaly labels (full 4-class corpus) | Restricted | Rationale in [`sample-data/README.md`](sample-data/README.md) |
| Maintenance / lubrication records | Restricted | Rationale in [`sample-data/README.md`](sample-data/README.md) |
| Original CodeCarbon logs (Figure 13 simulation) | Not available | Not found in the research workspace |
| Power BI operational dashboard | Not public | Contains restricted operational data |

## Paper-to-artifact map

Section numbers follow the submitted manuscript and may shift in the final
published layout.

| Paper section | Public implementation or documentation |
|---|---|
| 3.1 Physical system | Sample schema and acquisition notes in [`sample-data/README.md`](sample-data/README.md) |
| 3.2 Data acquisition and preprocessing | [`services/kpi-calculator`](services/kpi-calculator) |
| 3.2 Model invocation | [`services/model-invoker`](services/model-invoker) |
| 3.2 Modelling and release | [`pipeline/ml-pipelines`](pipeline/ml-pipelines) and [`pipeline/azure-devops`](pipeline/azure-devops) |
| 4.1 Sensor health | [`pipeline/src/train_sensor_health.py`](pipeline/src/train_sensor_health.py) |
| 4.2 Outlier detection | [`pipeline/src/train_outlier.py`](pipeline/src/train_outlier.py) |
| 4.3 Lubrication regression | [`pipeline/src/train_lubrication.py`](pipeline/src/train_lubrication.py) |
| 3.3 and 5.2 resource efficiency | [`sustainability/analyze_emissions.py`](sustainability/analyze_emissions.py); values transcribed from the paper because the original CodeCarbon exports were not found |

## Reproducibility scope

This repository supports:

- Inspection and plotting of representative raw signals.
- Re-execution of KPI extraction on the shared `.mat` files.
- Execution of the modelling scripts on the sample KPI table and labels, as a software check.
- Inspection of the AML pipeline structure; running it requires your own AML workspace and an authorised dataset.

It does **not** support independent numerical replication of the reported F1,
regression, or CO2 values from public data alone, since those depend on the
restricted dataset and the original cloud execution context. See
[`sample-data/README.md`](sample-data/README.md) for the restriction and
[`docs/reproducibility.md`](docs/reproducibility.md) for claim-level detail.

## Citation

If you use this artifact, please cite the Zenodo release. <https://doi.org/10.5281/zenodo.23733565>

## Licences and attribution

Code is released under the Apache License 2.0. Shareable data and documentation are
released under CC BY 4.0; see [`DATA_LICENSE.md`](DATA_LICENSE.md). The AML
design was informed by Microsoft's `Azure/mlops-v2-ado-demo`; details are in
[`NOTICE`](NOTICE) and [`docs/provenance.md`](docs/provenance.md).