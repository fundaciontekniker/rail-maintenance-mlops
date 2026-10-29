# Reproducibility and claims

This artifact follows a tiered reproducibility model so that public software
checks are not confused with independent replication of restricted-data
results. See [`../sample-data/README.md`](../sample-data/README.md) for the data
restriction.

| Claim or artifact | Publicly reproducible? | Limitation |
|---|---|---|
| Raw file parsing, KPI extraction, model scripts and interfaces | Yes | Runs locally on the 11 shared samples; no Azure required |
| Azure Function structure and AML pipeline submission | Yes, with your own Azure resources | Cloud account, compute, permissions, and cost are the user's |
| Reported outlier F1, lubrication regression, and CO2 values | No | Require the restricted dataset, labels, and original cloud context |
| Figure 13 retraining simulation | No | No raw CodeCarbon measurements |

Determinism: seeds are explicit (42 by default), direct dependencies are pinned
in `requirements.txt` and `pipeline/environment/conda.yml`, and the AML base
image is pinned by digest. Transitive dependencies are not hash-locked, and
floating-point results may differ slightly across platforms.