from pathlib import Path

import pandas as pd


DAILY_PREDICTIONS = 100
ANNUAL_TRAINING_CYCLES = 12
ROOT = Path(__file__).resolve().parent
INPUT = ROOT / "emissions.csv"
OUTPUT = ROOT / "adec-yeec.csv"
PROJECT_PATTERN = r"^metro-maintenance-(ML|DL)-(training|inference)-reconstructed$"


def calculate(codecarbon: pd.DataFrame) -> pd.DataFrame:
    required = {"project_name", "emissions"}
    missing = required.difference(codecarbon.columns)
    if missing:
        raise ValueError(f"Missing CodeCarbon columns: {', '.join(sorted(missing))}")

    records = codecarbon.loc[:, ["project_name", "emissions"]].copy()
    records[["model_type", "phase"]] = records["project_name"].str.extract(PROJECT_PATTERN)
    if records[["model_type", "phase"]].isna().any().any():
        raise ValueError("Every project_name must identify the reconstructed model and phase")
    if records.duplicated(["model_type", "phase"]).any():
        raise ValueError("Expected exactly one reconstructed row per model and phase")

    emissions = records.pivot(index="model_type", columns="phase", values="emissions")
    for phase in ("training", "inference"):
        if phase not in emissions:
            raise ValueError(f"Missing {phase} emissions rows")

    result = emissions.rename(
        columns={
            "training": "training_emissions_kg_co2e",
            "inference": "inference_emissions_kg_co2e_per_prediction",
        }
    ).reset_index()
    result = result[
        [
            "model_type",
            "training_emissions_kg_co2e",
            "inference_emissions_kg_co2e_per_prediction",
        ]
    ]
    result["daily_predictions"] = DAILY_PREDICTIONS
    result["annual_training_cycles"] = ANNUAL_TRAINING_CYCLES
    result["adec_kg_co2e_per_day"] = (
        result["inference_emissions_kg_co2e_per_prediction"] * DAILY_PREDICTIONS
    )
    result["annual_inference_emissions_kg_co2e"] = result["adec_kg_co2e_per_day"] * 365
    result["annual_training_emissions_kg_co2e"] = (
        result["training_emissions_kg_co2e"] * ANNUAL_TRAINING_CYCLES
    )
    result["yeec_kg_co2e_per_year"] = (
        result["annual_inference_emissions_kg_co2e"]
        + result["annual_training_emissions_kg_co2e"]
    )
    order = pd.Categorical(result["model_type"], categories=["ML", "DL"], ordered=True)
    return result.assign(_order=order).sort_values("_order").drop(columns="_order")


def main() -> None:
    result = calculate(pd.read_csv(INPUT))
    result.to_csv(OUTPUT, index=False, float_format="%.12g")
    print(result.to_string(index=False))
    print(f"\nWrote {OUTPUT.relative_to(ROOT.parent)}")


if __name__ == "__main__":
    main()
