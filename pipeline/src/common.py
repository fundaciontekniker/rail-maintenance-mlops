import json
from pathlib import Path
from typing import Any

import pandas as pd


ACCELEROMETERS = [f"Acceleration_{index}_g" for index in range(1, 7)]


def read_kpis(path: Path) -> pd.DataFrame:
    frame = pd.read_csv(path, sep=None, engine="python", parse_dates=["datetime"])
    required = {"file_name", "datetime", "sensor", "RMS", "Mean", "InterquartileRange"}
    missing = required.difference(frame.columns)
    if missing:
        raise ValueError(f"KPI data is missing columns: {', '.join(sorted(missing))}")
    return frame


def write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")
