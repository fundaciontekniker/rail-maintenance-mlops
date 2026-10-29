using System.Globalization;
using System.Numerics;
using csmatio.io;
using csmatio.types;
using MathNet.Numerics.IntegralTransforms;

namespace MetroMaintenance.KpiCalculator;

public sealed class IndicatorManager
{
    private const double SamplingRate = 10_000.0;

    private static readonly string[] InputChannels =
    {
        "Acceleration_1", "Acceleration_2", "Acceleration_3",
        "Acceleration_4", "Acceleration_5", "Acceleration_6"
    };

    private static readonly string[] OutputChannels =
    {
        "Acceleration_1_g", "Acceleration_2_g", "Acceleration_3_g",
        "Acceleration_4_g", "Acceleration_5_g", "Acceleration_6_g"
    };

    private static readonly double[] Calibration =
    {
        9.626, 9.497, 9.548, 9.507, 9.427, 9.469
    };

    private static readonly string[] TimeHeaders =
    {
        "RMS", "Median", "Mean", "Mode", "TrimmedMean",
        "GeometricMean", "HarmonicMean", "Range",
        "StandardDeviation", "Variance", "MeanAbsoluteDeviation",
        "InterquartileRange", "Skewness", "ThirdCentralMoment", "PeakToPeak",
        "PeakValue", "Kurtosis", "CrestFactor", "ClearanceFactor",
        "ImpulseFactor", "ShapeFactor", "Power"
    };

    private static readonly string[] FrequencyHeaders =
    {
        "Peak1_FrequencyHz", "Peak1_RMS", "Peak1_Sum",
        "Peak2_FrequencyHz", "Peak2_RMS", "Peak2_Sum",
        "Peak3_FrequencyHz", "Peak3_RMS", "Peak3_Sum",
        "BandRMS_0_500Hz", "BandRMS_500_1000Hz", "BandRMS_1000_2500Hz", "BandRMS_2500_5000Hz"
    };

    public async Task WriteKpisAsync(Stream matStream, string fileName, Stream output)
    {
        var temporaryFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.mat");
        try
        {
            await using (var file = File.Create(temporaryFile))
            {
                await matStream.CopyToAsync(file);
            }

            var mat = new MatFileReader(temporaryFile);
            await using var writer = new StreamWriter(output, leaveOpen: true);
            await writer.WriteLineAsync(BuildHeader());

            for (var index = 0; index < InputChannels.Length; index++)
            {
                var inputName = InputChannels[index];
                var values = ReadVector(mat, inputName)
                    .Select(value => value * Calibration[index])
                    .ToArray();
                var analysed = Segment(values, 35.0);
                var time = CalculateTimeDescriptors(analysed);
                var envelopes = CalculateEnvelopes(analysed, 31);
                var upper = CalculateTimeDescriptors(envelopes.Upper);
                var lower = CalculateTimeDescriptors(envelopes.Lower);
                var frequency = CalculateFrequencyDescriptors(analysed, SamplingRate);

                var fields = new List<string>
                {
                    fileName,
                    ParseTimestamp(fileName),
                    OutputChannels[index]
                };
                fields.AddRange(TimeValues(time));
                fields.AddRange(TimeValues(upper));
                fields.AddRange(TimeValues(lower));
                fields.AddRange(FrequencyValues(frequency));
                await writer.WriteLineAsync(string.Join(';', fields));
            }

            await writer.FlushAsync();
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    private static double[] ReadVector(MatFileReader mat, string name) =>
        ((MLDouble)mat.Content[name]).GetArray()[0];

    private static double[] Segment(double[] values, double threshold)
    {
        var crossings = Enumerable.Range(0, values.Length - 1)
            .Where(index => values[index] < threshold && values[index + 1] > threshold)
            .ToArray();
        if (crossings.Length == 0)
        {
            return values;
        }

        var start = Math.Max(0, crossings[0] - 200);
        var end = Math.Min(values.Length, crossings[^1] + 201);
        return values[start..end];
    }

    private static SignalDescriptors CalculateTimeDescriptors(double[] values)
    {
        var sorted = values.OrderBy(value => value).ToArray();
        var absolute = values.Select(value => Math.Abs(value)).ToArray();
        var mean = values.Average();
        var variance = values.Select(value => Math.Pow(value - mean, 2)).Average();
        var standardDeviation = Math.Sqrt(variance);
        var rms = Math.Sqrt(values.Select(value => value * value).Average());
        var peakToPeak = values.Max() - values.Min();
        var peakValue = peakToPeak / 2.0;
        var absoluteMean = absolute.Average();
        var squareRootMean = absolute.Select(value => Math.Sqrt(value)).Average();
        var nonZeroAbsolute = absolute.Where(value => value > 0).ToArray();
        var geometric = nonZeroAbsolute.Length == 0
            ? 0
            : Math.Exp(nonZeroAbsolute.Select(value => Math.Log(value)).Average());
        var harmonic = nonZeroAbsolute.Length == 0
            ? double.NaN
            : nonZeroAbsolute.Length / nonZeroAbsolute.Select(value => 1.0 / value).Sum();
        var trim = (int)(values.Length * 0.33);
        var trimmed = sorted.Skip(trim).Take(Math.Max(1, values.Length - (2 * trim))).Average();
        var thirdMoment = values.Select(value => Math.Pow(value - mean, 3)).Average();

        return new SignalDescriptors(
            rms,
            sorted[sorted.Length / 2],
            mean,
            values.GroupBy(value => value).OrderByDescending(group => group.Count()).First().Key,
            trimmed,
            geometric,
            harmonic,
            peakToPeak,
            standardDeviation,
            variance,
            values.Select(value => Math.Abs(value - mean)).Average(),
            sorted[(int)(sorted.Length * 0.75)] - sorted[(int)(sorted.Length * 0.25)],
            Divide(thirdMoment, Math.Pow(standardDeviation, 3)),
            thirdMoment,
            peakToPeak,
            peakValue,
            Divide(values.Select(value => Math.Pow(value - mean, 4)).Average(), Math.Pow(standardDeviation, 4)),
            Divide(peakValue, rms),
            Divide(peakValue, squareRootMean * squareRootMean),
            Divide(peakValue, absoluteMean),
            Divide(rms, absoluteMean),
            values.Select(value => value * value).Average());
    }

    private static (double[] Upper, double[] Lower) CalculateEnvelopes(double[] values, int size)
    {
        var upper = new double[values.Length];
        var lower = new double[values.Length];
        var half = size / 2;
        for (var index = 0; index < values.Length; index++)
        {
            var start = Math.Max(0, index - half);
            var end = Math.Min(values.Length - 1, index + half);
            var maximum = values[start];
            var minimum = values[start];
            for (var window = start + 1; window <= end; window++)
            {
                maximum = Math.Max(maximum, values[window]);
                minimum = Math.Min(minimum, values[window]);
            }
            upper[index] = maximum;
            lower[index] = minimum;
        }
        return (upper, lower);
    }

    private static FrequencyDescriptors CalculateFrequencyDescriptors(double[] values, double samplingRate)
    {
        var spectrum = values.Select(value => new Complex(value, 0)).ToArray();
        Fourier.Forward(spectrum, FourierOptions.Matlab);
        var oneSidedLength = (values.Length / 2) + 1;
        var amplitude = new double[oneSidedLength];
        var frequencies = new double[oneSidedLength];
        for (var index = 0; index < oneSidedLength; index++)
        {
            amplitude[index] = spectrum[index].Magnitude / values.Length;
            if (index > 0 && index < oneSidedLength - 1)
            {
                amplitude[index] *= 2;
            }
            frequencies[index] = samplingRate * index / values.Length;
        }

        var peaks = Enumerable.Range(1, Math.Max(0, amplitude.Length - 2))
            .Where(index => amplitude[index] > amplitude[index - 1] && amplitude[index] > amplitude[index + 1])
            .OrderByDescending(index => amplitude[index])
            .Take(3)
            .ToArray();
        var peakValues = Enumerable.Range(0, 3)
            .Select(rank => PeakWindow(rank < peaks.Length ? peaks[rank] : -1, amplitude, frequencies))
            .ToArray();

        return new FrequencyDescriptors(
            peakValues[0].Frequency, peakValues[0].Rms, peakValues[0].Sum,
            peakValues[1].Frequency, peakValues[1].Rms, peakValues[1].Sum,
            peakValues[2].Frequency, peakValues[2].Rms, peakValues[2].Sum,
            BandRms(spectrum, frequencies, values.Length, 0, 500),
            BandRms(spectrum, frequencies, values.Length, 500, 1000),
            BandRms(spectrum, frequencies, values.Length, 1000, 2500),
            BandRms(spectrum, frequencies, values.Length, 2500, 5000));
    }

    private static (double Frequency, double Rms, double Sum) PeakWindow(
        int peak,
        double[] amplitude,
        double[] frequencies)
    {
        if (peak < 0)
        {
            return (0, 0, 0);
        }
        var start = Math.Max(0, peak - 15);
        var end = Math.Min(amplitude.Length - 1, peak + 15);
        var window = amplitude[start..(end + 1)];
        return (frequencies[peak], Math.Sqrt(window.Select(value => value * value).Average()), window.Sum());
    }

    private static double BandRms(
        Complex[] spectrum,
        double[] frequencies,
        int sampleCount,
        double low,
        double high)
    {
        double power = 0;
        for (var index = 0; index < frequencies.Length; index++)
        {
            if (frequencies[index] < low || frequencies[index] >= high)
            {
                continue;
            }
            var multiplier = index == 0 || (sampleCount % 2 == 0 && index == sampleCount / 2) ? 1 : 2;
            power += multiplier * spectrum[index].Magnitude * spectrum[index].Magnitude;
        }
        return Math.Sqrt(power) / sampleCount;
    }

    private static string BuildHeader()
    {
        var headers = new List<string> { "file_name", "datetime", "sensor" };
        headers.AddRange(TimeHeaders);
        headers.AddRange(TimeHeaders.Select(header => $"{header}_upper"));
        headers.AddRange(TimeHeaders.Select(header => $"{header}_lower"));
        headers.AddRange(FrequencyHeaders);
        return string.Join(';', headers);
    }

    private static IEnumerable<string> TimeValues(SignalDescriptors value) => new[]
    {
        Format(value.Rms), Format(value.Median), Format(value.Mean), Format(value.Mode),
        Format(value.TrimmedMean), Format(value.GeometricMean), Format(value.HarmonicMean),
        Format(value.Range), Format(value.StandardDeviation), Format(value.Variance),
        Format(value.MeanAbsoluteDeviation), Format(value.InterquartileRange), Format(value.Skewness),
        Format(value.ThirdCentralMoment), Format(value.PeakToPeak), Format(value.PeakValue),
        Format(value.Kurtosis), Format(value.CrestFactor), Format(value.ClearanceFactor),
        Format(value.ImpulseFactor), Format(value.ShapeFactor), Format(value.Power)
    };

    private static IEnumerable<string> FrequencyValues(FrequencyDescriptors value) => new[]
    {
        Format(value.F1), Format(value.Rms1), Format(value.Sum1),
        Format(value.F2), Format(value.Rms2), Format(value.Sum2),
        Format(value.F3), Format(value.Rms3), Format(value.Sum3),
        Format(value.Rms0To500), Format(value.Rms500To1000),
        Format(value.Rms1000To2500), Format(value.Rms2500To5000)
    };

    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static double Divide(double numerator, double denominator) =>
        denominator == 0 ? double.NaN : numerator / denominator;

    private static string ParseTimestamp(string fileName)
    {
        var formats = new[] { "yyyyMMdd_HHmmss_fff", "yyyyMMdd_HHmmssfff" };
        var stem = Path.GetFileNameWithoutExtension(fileName);
        foreach (var format in formats)
        {
            var length = format.Replace("'", string.Empty).Length;
            if (stem.Length >= length && DateTime.TryParseExact(
                    stem[..length], format, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var timestamp))
            {
                return timestamp.ToString("O", CultureInfo.InvariantCulture);
            }
        }
        return string.Empty;
    }
}
