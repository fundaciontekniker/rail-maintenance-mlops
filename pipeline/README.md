# Modelling pipelines

This directory maps the paper's modelling and release layer to scripts that
can run locally or as Azure Machine Learning (AML) command jobs.

| Paper analysis | Local entry point | AML job |
|---|---|---|
| Outlier classification (random forest) | `src/train_outlier.py` | `ml-pipelines/outlier-ml-pipeline.yml` |
| Sensor-health clustering | `src/train_sensor_health.py` | `ml-pipelines/sensor-health-ml-pipeline.yml` |
| Lubrication regression (XGBoost) | `src/train_lubrication.py` | Not runnable without restricted lubrication schedule |

The complete operational dataset and the full manual label corpus cannot be distributed.
Consequently, the shared samples demonstrate acquisition, KPI extraction, and a
software check of the outlier and sensor-health workflows. These sample
runs are software checks, not replications of the performance values reported in
the paper.

## What this folder does (and does not do)

This folder provides:

- local Python entry points that implement the paper-facing modelling steps
- AML pipeline job definitions under `ml-pipelines/` that run those scripts as AML command jobs

This folder does not provide:

- an AML batch-endpoint deployment
- model promotion/registration gates
- a production inference service

The deployed batch endpoints referenced by `services/model-invoker` are intentionally out of scope here because they require organisation-specific identity, networking, and billable cloud resources.

For a reference Azure DevOps orchestration (tests + AML pipeline + endpoint deployment), see `azure-devops/`.

## Local smoke run

From the repository root, after installing `requirements.txt` and generating KPIs with:

```powershell
dotnet run --project services/kpi-calculator/local-runner -- --samples
```

```powershell
python pipeline/src/train_outlier.py --kpi-data sample-data/kpi/kpis.csv --labels sample-data/labels/outlier-labels.csv --output artifacts/models/outlier-samples
python pipeline/src/train_sensor_health.py --kpi-data sample-data/kpi/kpis.csv --output artifacts/models/sensor-health-samples
```

Lubrication regression requires the operational lubrication schedule (`lubrication_percentage` by acquisition), which is restricted and not distributed in this artifact.

All random seeds are explicit. Outputs include model files, metrics JSON, and
diagnostic plots. Results can vary slightly by dependency and platform.

## Extract KPIs from a single file

To extract KPIs from one shared `.mat` file with the C# implementation:

```powershell
dotnet run --project services/kpi-calculator/local-runner -- `
  sample-data/raw/sample_0001.mat `
  sample-data/kpi/sample_0001.csv
```

One anonymised file is insufficient to fit the paper's models and has no
shareable outlier or lubrication label. It is intended to validate the public
raw-data contract and preprocessing path.

## Azure Machine Learning

1. Install Azure CLI and the ML extension: `az extension add --name ml`.
2. Authenticate with `az login` and select a subscription.
3. Create or select an AML workspace and CPU compute cluster.
4. Provide your own authorised KPI table and label/context files.
5. Register the pinned environment with `az ml environment create --file aml/environment.yml`.
6. Edit only the `default_compute` value in the `ml-pipelines/*.yml` file if your cluster is not
   named `cpu-cluster`.
7. From this directory, submit one of the pipeline jobs under `ml-pipelines/`:

```powershell
az ml job create --file ml-pipelines/outlier-ml-pipeline.yml `
  --resource-group <resource-group> `
  --workspace-name <workspace>
```

The YAML uses local paths, so AML uploads a snapshot of the referenced files
with the job. For operational deployments, replace those paths with versioned
AML data assets and add model registration/promotion gates. Endpoint deployment
is not automatic here because it creates billable cloud resources and requires
an organisation-specific identity and network configuration.

If you do create endpoints externally, document their input/output contract and then configure `services/model-invoker` to match it.

The sensor-health script follows the research implementation by treating each
sensor's row in the RMS correlation matrix as its feature vector. Linkage is
therefore based on similarity between correlation profiles, not directly on
the scalar pairwise correlation distance. This distinction matters when
adapting the method to a different sensor layout.

## Relationship to the research system

This public package follows the final manuscript's modelling set. The original
project was adapted from Microsoft's
[`Azure/mlops-v2-ado-demo`](https://github.com/Azure/mlops-v2-ado-demo); the
full adaptation and attribution record is in
[`../docs/provenance.md`](../docs/provenance.md) and [`../NOTICE`](../NOTICE).
