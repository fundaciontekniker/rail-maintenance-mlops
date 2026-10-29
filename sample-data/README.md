# Shareable sample data

`raw/` contains 11 pseudonymised accelerometer acquisitions selected from the
same type of files processed in the study. The original timestamps and
operationally identifying filenames were replaced by sequential sample IDs.
Variables outside the scope of the paper were removed: each file contains only
the six acceleration arrays. No lubrication schedule, train identifier,
passenger data, or precise infrastructure location is included.

## Scope and intended use

The files may be used to inspect signal characteristics, test the input
contract, plot channels, and run the public KPI extractor. They are not a
statistically representative subset and cannot reproduce the model metrics in
the article.

## Sample labels (outliers)

This artifact includes a small set of illustrative outlier labels for the 11 shared samples in [`labels/outlier-labels.csv`](labels/outlier-labels.csv).

These labels are provided to:

- demonstrate the file/feature/label contract on public data
- help readers understand what an "outlier" means in the context of the paper

They are not the full operational annotation set used to report the paper’s performance metrics.

New to the repository: follow the root [`README.md`](../README.md) "Quick start" to plot one acquisition and run KPI extraction locally.

For the end-to-end file interfaces, see the root [`README.md`](../README.md) "Data contracts" section.

These 11 files may be redistributed under the terms in [`../DATA_LICENSE.md`](../DATA_LICENSE.md).

## MATLAB schema

All shared files use the same variable contract:

| Variable | Type | Typical shape | Meaning |
|---|---|---:|---|
| `Acceleration_1` ... `Acceleration_6` | float64 | `1 x 204000` to `1 x 308000` | Six rail accelerometer channels |

The accelerometer sampling rate used in the study was 10000 Hz. It is
documented rather than stored as an additional MATLAB variable so the public
files contain only the arrays requested for this artifact. Their lengths
represent approximately 20.4 to 30.8 seconds.

Signal traces can still be quasi-identifiers for an operating event. Their
redistribution therefore remains subject to data-owner and security review;
replacing the filename and removing unrelated variables are not asserted to
guarantee anonymisation by themselves.

## Plotting

Run the notebook `plot_raw_signals.ipynb`, or use the KPI command documented
in the root README. Each acquisition contains approximately 1.2 to 1.85
million acceleration values across its six channels.

## KPI tables for the shared samples

The folder `kpi/` contains KPI CSV files computed from the 11 shared `.mat` acquisitions using the public C# extractor.

`kpi/kpis.csv` is a convenience concatenation of all per-sample KPI tables, suitable as direct input to the Python scripts.

You can regenerate them from the repository root with:

```powershell
dotnet run --project services/kpi-calculator/local-runner -- --samples
```

## Restricted parent dataset

The parent dataset covers an active metro installation and includes dates,
maintenance context, manual labels, and operational records. It is not public
because it is owned or controlled by third parties and is subject to
confidentiality, security, and contractual restrictions. The authors cannot
grant access unilaterally and are not accepting access requests through this
repository. This statement can be revised only if the data owner establishes
an approved access procedure.
