# Data dictionary

## Raw acquisitions

Raw variable shapes and sampling rates are documented in
[`../sample-data/README.md`](../sample-data/README.md). The KPI extractor
applies fixed per-channel scaling constants to the raw amplitudes before
feature extraction. These constants came from the research acquisition
configuration, are installation-specific, and must not be assumed valid for
another installation.

All six arrays use the 10000 Hz sampling rate reported for the study.

Accelerometer signals are segmented around upward crossings of a fixed
amplitude threshold, retaining 200 samples of context on each side. This is a
site-specific event-isolation heuristic, not a universal fault threshold.

## KPI columns

The CSV delimiter is `;`. `file_name`, `datetime`, and `sensor` identify a row.
Pseudonymised sample filenames do not encode dates, so their `datetime` value
is empty.

| Column or family | Definition |
|---|---|
| `RMS` | Root mean square of the calibrated signal |
| `Median`, `Mean`, `Mode` | Median, arithmetic mean, and sample mode |
| `TrimmedMean` | 33% trimmed arithmetic mean |
| `GeometricMean`, `HarmonicMean` | Geometric mean of absolute nonzero values; harmonic mean of absolute nonzero values |
| `Range`, `PeakToPeak` | Maximum minus minimum; retained duplicate measurements from the source implementation |
| `StandardDeviation`, `Variance` | Population standard deviation and variance |
| `MeanAbsoluteDeviation` | Mean absolute deviation from the mean |
| `InterquartileRange` | Interquartile range |
| `Skewness` | Biased sample skewness |
| `ThirdCentralMoment` | Third central moment |
| `PeakValue` | Half of peak-to-peak amplitude |
| `Kurtosis` | Pearson kurtosis (normal distribution equals 3) |
| `CrestFactor`, `ClearanceFactor`, `ImpulseFactor`, `ShapeFactor` | Standard vibration factors |
| `Power` | Mean squared amplitude |
| `<feature>_upper`, `<feature>_lower` | Same time feature on 31-sample moving upper/lower envelopes |
| `Peak1_FrequencyHz` ... `Peak3_FrequencyHz` | Frequencies of the three largest local FFT-amplitude peaks |
| `Peak1_RMS` ... `Peak3_RMS` | RMS of single-sided amplitudes in +/-15 FFT bins around each peak |
| `Peak1_Sum` ... `Peak3_Sum` | Sum of amplitudes in the same peak window |
| `BandRMS_0_500Hz` | Parseval-consistent RMS after ideal FFT selection from 0 to <500 Hz |
| `BandRMS_500_1000Hz` | Same, 500 to <1000 Hz |
| `BandRMS_1000_2500Hz` | Same, 1000 to <2500 Hz |
| `BandRMS_2500_5000Hz` | Same, 2500 to <5000 Hz |

The KPI extractor computes a broad feature set (82 KPI columns per sensor channel in this artifact) so the preprocessing output is reusable for multiple analyses. The paper reports and discusses only a subset of these features, and the public modelling scripts intentionally select only the features they use (see `pipeline/src/train_outlier.py` and `pipeline/src/train_lubrication.py`).

## Model-side data

`outlier` is an integer manual class: 0 normal, 1 physical anomaly, 2
sensor/acquisition malfunction, and 3 acquisition re-triggering.

This artifact also includes a small labelled subset for the 11 shared samples in `sample-data/labels/outlier-labels.csv`. The full operational label corpus used for the paper’s reported metrics remains restricted.

`lubrication_percentage` is the scheduled flange-lubricator dosage percentage
associated with an acquisition. It is an operational setting, not a directly
measured rail-friction value. Real schedules are restricted and are not part of
this public artifact.
