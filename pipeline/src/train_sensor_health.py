import argparse
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
from scipy.cluster.hierarchy import dendrogram, fcluster, linkage
from sklearn.metrics import davies_bouldin_score, silhouette_score

from common import ACCELEROMETERS, read_kpis, write_json


def main() -> None:
    parser = argparse.ArgumentParser(description="Cluster sensors by RMS correlation")
    parser.add_argument("--kpi-data", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--threshold", type=float, default=0.6)
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    frame = read_kpis(args.kpi_data)
    pivoted = frame[frame["sensor"].isin(ACCELEROMETERS)].pivot_table(
        index="file_name", columns="sensor", values="RMS"
    ).dropna()
    correlations = pivoted.corr()

    best = None
    for method in ("ward", "complete", "average", "single"):
        tree = linkage(correlations, method=method)
        labels = fcluster(tree, t=args.threshold, criterion="distance")
        cluster_count = len(np.unique(labels))
        if cluster_count < 2 or cluster_count >= len(labels):
            continue
        candidate = {
            "method": method,
            "silhouette": silhouette_score(correlations, labels),
            "davies_bouldin": davies_bouldin_score(correlations, labels),
            "tree": tree,
            "labels": labels,
        }
        if best is None or (candidate["silhouette"], -candidate["davies_bouldin"]) > (
            best["silhouette"],
            -best["davies_bouldin"],
        ):
            best = candidate
    if best is None:
        raise ValueError("No linkage method produced a valid multi-cluster solution")

    write_json(
        args.output / "metrics.json",
        {
            "linkage": best["method"],
            "silhouette": best["silhouette"],
            "davies_bouldin": best["davies_bouldin"],
            "distance_threshold": args.threshold,
            "clusters": dict(zip(correlations.columns, map(int, best["labels"]))),
            "synthetic_or_sample_metrics_are_not_paper_results": True,
        },
    )
    dendrogram(best["tree"], labels=correlations.columns, leaf_rotation=45)
    plt.ylabel("Distance")
    plt.tight_layout()
    plt.savefig(args.output / "dendrogram.png", dpi=150)
    plt.close()


if __name__ == "__main__":
    main()
