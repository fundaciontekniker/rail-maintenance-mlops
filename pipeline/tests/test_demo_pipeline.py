import pandas as pd

from train_outlier import prepare


def test_outlier_prepare_pivots_four_accelerometers() -> None:
    rows = []
    for file_name, label in (("a.mat", 0), ("b.mat", 1)):
        for index in range(1, 5):
            rows.append(
                {
                    "file_name": file_name,
                    "sensor": f"Acceleration_{index}_g",
                    "RMS": float(index),
                    "Mean": 0.0,
                    "Median": 0.0,
                    "InterquartileRange": 1.0,
                    "Skewness": 0.0,
                    "Kurtosis": 3.0,
                }
            )
    x, y = prepare(pd.DataFrame(rows), pd.DataFrame({"file_name": ["a.mat", "b.mat"], "outlier": [0, 1]}))
    assert x.shape == (2, 24)
    assert y.tolist() == [0, 1]
