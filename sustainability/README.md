# Sustainability analysis

The paper used CodeCarbon 2.4.1 to measure emissions from the random-forest
model (`ML`) and deep-learning model (`DL`). The original CodeCarbon export was
not found in the research workspace.

The file [`emissions.csv`](emissions.csv) preserves the standard 32-column
CodeCarbon CSV schema from the example supplied by the authors. Its rows have
been reconstructed for this repository using only values reported in the
paper:

| Reconstructed record | `emissions` value | Source |
|---|---:|---|
| ML training | 0.00143 kgCO2e | Table 3 |
| DL training | 0.01291 kgCO2e | Table 3 |
| ML single inference | 1.04e-7 kgCO2e | Section 5.2 (ADEC divided by 100 daily predictions) |
| DL single inference | 1.24e-7 kgCO2e | Section 5.2 (ADEC divided by 100 daily predictions) |

The `timestamp` column is retained only for compatibility with the standard
CodeCarbon schema, but its values are empty because the original measurement
timestamps are unavailable. Project names end in `-reconstructed` to make the
record status explicit. Other unknown CodeCarbon fields are also empty.
Hardware, duration, energy, geographic location, run IDs, and experiment IDs
from the unrelated example were removed rather than incorrectly attributed to this study. Consequently,
`emissions.csv` is a CodeCarbon-compatible reconstruction, not an original raw
measurement log.

## Calculate ADEC and YEEC

Run from the repository root:

```powershell
python sustainability/analyze_emissions.py
```

There are no command-line parameters. The script reads `emissions.csv` and
writes [`adec-yeec.csv`](adec-yeec.csv) using:

```text
ADEC = emissions per inference * 100 daily predictions
YEEC = ADEC * 365 + training emissions * 12 monthly training cycles
```

The 12-cycle YEEC is a constant-cost monthly scenario based on the retraining
schedule described in Section 3.2. It assumes each monthly training cycle has
the emissions measured for the corresponding model in Table 3.

This calculation does not reproduce Figure 13. That figure used a separate
simulation involving dataset growth and incremental deep learning, but its raw
CodeCarbon measurements were not found. Those missing values are not inferred
or fabricated here.

New to the repository: the root [`README.md`](../README.md) includes a quick command to run this analysis after setting up Python.
