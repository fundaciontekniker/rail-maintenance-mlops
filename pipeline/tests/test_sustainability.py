import importlib.util
from pathlib import Path

import pandas as pd
import pytest


MODULE_PATH = Path(__file__).resolve().parents[2] / "sustainability" / "analyze_emissions.py"
SPEC = importlib.util.spec_from_file_location("analyze_emissions", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


def test_paper_emissions_calculation() -> None:
    frame = pd.DataFrame(
        {
            "project_name": [
                "metro-maintenance-ML-training-reconstructed",
                "metro-maintenance-ML-inference-reconstructed",
            ],
            "emissions": [0.00143, 1.04e-7],
        }
    )
    result = MODULE.calculate(frame).iloc[0]
    assert result["adec_kg_co2e_per_day"] == pytest.approx(1.04e-5)
    assert result["yeec_kg_co2e_per_year"] == pytest.approx(0.020956)
