import argparse
from pathlib import Path

import joblib
import numpy as np
import pandas as pd
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score
from sklearn.model_selection import GridSearchCV, GroupKFold, GroupShuffleSplit, TimeSeriesSplit
from xgboost import XGBRegressor

from common import ACCELEROMETERS, read_kpis, write_json


def metrics(actual: pd.Series, predicted: np.ndarray) -> dict[str, float]:
    return {
        "r2": r2_score(actual, predicted),
        "mae": mean_absolute_error(actual, predicted),
        "rmse": float(np.sqrt(mean_squared_error(actual, predicted))),
    }


def prepare(kpis: pd.DataFrame, lubrication: pd.DataFrame) -> pd.DataFrame:
    features = [
        column
        for column in [
            "RMS",
            "Median",
            "Skewness",
            "Kurtosis",
            "BandRMS_0_500Hz",
            "BandRMS_500_1000Hz",
            "BandRMS_1000_2500Hz",
            "BandRMS_2500_5000Hz",
        ]
        if column in kpis.columns
    ]
    selected = kpis[kpis["sensor"].isin(ACCELEROMETERS[:4])]
    pivoted = selected.pivot_table(index="file_name", columns="sensor", values=features)
    pivoted.columns = [f"{feature}_{sensor}" for feature, sensor in pivoted.columns]
    lubrication["datetime"] = pd.to_datetime(lubrication["datetime"])
    return pivoted.reset_index().merge(lubrication, on="file_name", validate="one_to_one").dropna()


def fit_search(
    x: pd.DataFrame,
    y: pd.Series,
    groups: pd.Series,
    seed: int,
    temporal: bool = False,
) -> GridSearchCV:
    group_count = groups.nunique()
    if group_count < 3:
        raise ValueError("At least three temporal groups are required")
    cross_validation = TimeSeriesSplit(n_splits=3) if temporal else GroupKFold(n_splits=min(3, group_count))
    search = GridSearchCV(
        XGBRegressor(objective="reg:squarederror", random_state=seed, n_jobs=1),
        {"learning_rate": [0.05, 0.1], "max_depth": [3, 5], "n_estimators": [100, 200]},
        cv=cross_validation,
        scoring="neg_mean_absolute_error",
        n_jobs=-1,
    )
    if temporal:
        search.fit(x, y)
    else:
        search.fit(x, y, groups=groups)
    return search


def main() -> None:
    parser = argparse.ArgumentParser(description="Evaluate lubrication-level regression")
    parser.add_argument("--kpi-data", type=Path, required=True)
    parser.add_argument("--lubrication-data", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--seed", type=int, default=42)
    args = parser.parse_args()

    args.output.mkdir(parents=True, exist_ok=True)
    data = prepare(read_kpis(args.kpi_data), pd.read_csv(args.lubrication_data))
    data = data.sort_values("datetime").reset_index(drop=True)
    feature_columns = [column for column in data.columns if column not in {
        "file_name", "datetime", "lubrication_percentage"
    }]
    x = data[feature_columns]
    y = data["lubrication_percentage"]
    groups = data["datetime"].dt.floor("6h")

    splitter = GroupShuffleSplit(n_splits=1, test_size=0.25, random_state=args.seed)
    train_index, test_index = next(splitter.split(x, y, groups))
    grouped_model = fit_search(x.iloc[train_index], y.iloc[train_index], groups.iloc[train_index], args.seed)
    grouped_metrics = metrics(y.iloc[test_index], grouped_model.predict(x.iloc[test_index]))

    boundary = int(len(data) * 0.75)
    temporal_model = fit_search(
        x.iloc[:boundary],
        y.iloc[:boundary],
        groups.iloc[:boundary],
        args.seed,
        temporal=True,
    )
    temporal_metrics = metrics(y.iloc[boundary:], temporal_model.predict(x.iloc[boundary:]))

    write_json(
        args.output / "metrics.json",
        {
            "grouped_random_testing": grouped_metrics,
            "temporal_forecasting": temporal_metrics,
            "grouping_window_hours": 6,
            "temporal_model_selection": "forward_chaining_time_series_split",
            "best_grouped_parameters": grouped_model.best_params_,
            "best_temporal_parameters": temporal_model.best_params_,
            "synthetic_or_sample_metrics_are_not_paper_results": True,
        },
    )
    joblib.dump(
        {"model": temporal_model.best_estimator_, "features": feature_columns},
        args.output / "model.joblib",
    )


if __name__ == "__main__":
    main()
