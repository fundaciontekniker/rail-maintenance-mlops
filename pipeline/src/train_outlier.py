import argparse
from pathlib import Path

import joblib
import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score, precision_score, recall_score
from sklearn.base import clone
from sklearn.model_selection import RandomizedSearchCV, StratifiedKFold

from common import ACCELEROMETERS, read_kpis, write_json


def prepare(kpis: pd.DataFrame, labels: pd.DataFrame) -> tuple[pd.DataFrame, pd.Series]:
    features = [
        column
        for column in ["RMS", "Mean", "Median", "InterquartileRange", "Skewness", "Kurtosis"]
        if column in kpis.columns
    ]
    selected = kpis[kpis["sensor"].isin(ACCELEROMETERS[:4])]
    pivoted = selected.pivot_table(index="file_name", columns="sensor", values=features)
    pivoted.columns = [f"{feature}_{sensor}" for feature, sensor in pivoted.columns]
    merged = pivoted.reset_index().merge(labels, on="file_name", validate="one_to_one").dropna()
    return merged.drop(columns=["file_name", "outlier"]), merged["outlier"]


def main() -> None:
    parser = argparse.ArgumentParser(description="Train the four-class random forest")
    parser.add_argument("--kpi-data", type=Path, required=True)
    parser.add_argument("--labels", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    x, y = prepare(read_kpis(args.kpi_data), pd.read_csv(args.labels))
    folds = StratifiedKFold(n_splits=5, shuffle=True, random_state=args.seed)
    search = RandomizedSearchCV(
        RandomForestClassifier(random_state=args.seed, n_jobs=-1),
        {
            "n_estimators": [50, 100, 200],
            "max_depth": [None, 10, 20, 30],
            "min_samples_split": [2, 5, 10],
            "min_samples_leaf": [1, 2, 4],
        },
        n_iter=20,
        scoring="f1_macro",
        cv=folds,
        random_state=args.seed,
        n_jobs=-1,
    )
    predictions = pd.Series(index=y.index, dtype=y.dtype)
    selected_parameters = []
    for train_index, test_index in folds.split(x, y):
        nested_search = clone(search)
        nested_search.fit(x.iloc[train_index], y.iloc[train_index])
        predictions.iloc[test_index] = nested_search.predict(x.iloc[test_index])
        selected_parameters.append(nested_search.best_params_)
    metrics = {
        "accuracy": accuracy_score(y, predictions),
        "f1_macro": f1_score(y, predictions, average="macro"),
        "precision_macro": precision_score(y, predictions, average="macro", zero_division=0),
        "recall_macro": recall_score(y, predictions, average="macro", zero_division=0),
        "samples": len(y),
        "evaluation": "nested_stratified_5_fold_cross_validation",
        "outer_fold_parameters": selected_parameters,
        "synthetic_or_sample_metrics_are_not_paper_results": True,
    }
    write_json(args.output / "metrics.json", metrics)
    search.fit(x, y)
    search.best_estimator_.fit(x, y)
    joblib.dump({"model": search.best_estimator_, "features": list(x.columns)}, args.output / "model.joblib")

    matrix = confusion_matrix(y, predictions, labels=sorted(y.unique()))
    sns.heatmap(matrix, annot=True, fmt="d", cmap="Blues", cbar=False)
    plt.xlabel("Predicted")
    plt.ylabel("True")
    plt.tight_layout()
    plt.savefig(args.output / "confusion-matrix.png", dpi=150)
    plt.close()


if __name__ == "__main__":
    main()
