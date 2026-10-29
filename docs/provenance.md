# Provenance and adaptation record

## Research source

The public service was refactored from the internal research workspace. The
original implementation used .NET 6 Azure Functions, CSMatIO, Blob Storage
triggers, and separate model-invocation functions. The public refactoring:

- publishes the six accelerometer arrays discussed in the paper and removes
  unrelated channels and acquisition metadata from the shared MATLAB files;
- preserves the accelerometer calibration constants, 10000 Hz sampling rate,
  event threshold, output naming, and time/frequency feature families;
- replaces fixed temporary filenames and quadratic envelope loops;
- calculates frequency bands from the frequency vector rather than assuming
  one FFT bin per hertz;
- leaves pseudonymised timestamps empty rather than fabricating dates;
- uses the original explicit `BlobServiceClient`/`BlobClient` upload pattern,
  while writing one KPI CSV per input to avoid concurrent daily-file edits;
- exposes the same C# numerical logic through a cloud-independent local runner;
- consolidates three credential-bearing model callers into one configurable
  C# Function App using `DefaultAzureCredential`; and
- excludes all credentials, local settings, deployment profiles, build
  outputs, endpoint URLs, tenant identifiers, and resource names.

These are maintainability and correctness changes. The public package should
be treated as the archival method implementation, not as a byte-identical copy
of the production deployment.

## MLOps source

The specified research workspace was a domain adaptation of Microsoft's
`Azure/mlops-v2-ado-demo`. It contained AML CLI v2 pipeline jobs, Azure DevOps
templates, outlier classification, sensor-health clustering, and an earlier
operating-condition clustering experiment.

The final manuscript reports outlier classification, sensor-health clustering,
and XGBoost lubrication regression. The public pipeline follows that final
paper-facing set. It removes organisation-specific Azure DevOps service
connections and stale endpoint configuration, uses current coherent Python
dependencies, and supplies local entry points. The regression implementation
encodes the two validation scenarios and hyperparameter family described in
the manuscript; the full restricted training data is not included.

## Upstream reference and third-party code

The orchestration structure was informed by the following upstream project:

- Project: Azure MLOps v2 Azure DevOps demo
- URL: <https://github.com/Azure/mlops-v2-ado-demo>
- Owner: Microsoft Corporation
- Licence: MIT at the time it was used; verify the upstream repository for the
  authoritative current terms.

Only the Azure ML CLI step templates under `pipeline/azure-devops/azureml/` are
reused or copied from that upstream project. They carry the upstream copyright
header:

- `pipeline/azure-devops/azureml/connect-to-workspace.yml`
- `pipeline/azure-devops/azureml/create-compute.yml`
- `pipeline/azure-devops/azureml/create-deployment-default.yml`
- `pipeline/azure-devops/azureml/create-deployment.yml`
- `pipeline/azure-devops/azureml/create-endpoint.yml`
- `pipeline/azure-devops/azureml/install-aml-cli.yml`
- `pipeline/azure-devops/azureml/install-az-cli.yml`
- `pipeline/azure-devops/azureml/register-data.yml`
- `pipeline/azure-devops/azureml/register-environment.yml`
- `pipeline/azure-devops/azureml/run-pipeline.yml`
- `pipeline/azure-devops/azureml/test-deployment.yml`

The remaining Azure DevOps configuration (`pipeline/azure-devops/` outside
`azureml/`, for example `config.yml`, `outlier-mlops-pipeline.yml`, and
`deploy/batch/*.yml`) and the AML definitions under `pipeline/aml/` were
**adapted** from the same upstream project. Everything else in this repository
is original work by the authors.

The full upstream MIT licence text and attribution are reproduced in
[`../NOTICE`](../NOTICE). Because the upstream project is distributed under the
MIT License and this repository's code under the Apache License 2.0, and these
two licences are compatible, the reused and adapted portions remain
licence-compatible with the rest of the code in this artifact. The upstream MIT
terms continue to apply to the reused files. No Azure DevOps, Azure ML, or
infrastructure template is asserted to be a byte-identical copy of the upstream
source.
