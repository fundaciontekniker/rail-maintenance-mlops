using MetroMaintenance.KpiCalculator;

static void Usage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Single file: dotnet run --project local-runner -- <input.mat> <output.csv>");
    Console.Error.WriteLine("  Directory:   dotnet run --project local-runner -- <input-dir> <output-dir>");
    Console.Error.WriteLine("  Samples:     dotnet run --project local-runner -- --samples");
}

static async Task WriteOneAsync(IndicatorManager manager, string inputPath, string outputPath)
{
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    await using var input = File.OpenRead(inputPath);
    await using var output = File.Create(outputPath);
    await manager.WriteKpisAsync(input, Path.GetFileName(inputPath), output);
    Console.WriteLine($"Wrote KPI data to {outputPath}");
}

static async Task<int> WriteDirectoryAsync(IndicatorManager manager, string inputDir, string outputDir)
{
    if (!Directory.Exists(inputDir))
    {
        Console.Error.WriteLine($"Input directory not found: {inputDir}");
        return 2;
    }
    Directory.CreateDirectory(outputDir);

    var files = Directory.EnumerateFiles(inputDir, "*.mat").OrderBy(path => path).ToArray();
    if (files.Length == 0)
    {
        Console.Error.WriteLine($"No .mat files found in {inputDir}");
        return 2;
    }

    var outputFiles = new List<string>(files.Length);
    foreach (var file in files)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var outputPath = Path.Combine(outputDir, $"{name}.csv");
        await WriteOneAsync(manager, file, outputPath);
        outputFiles.Add(outputPath);
    }

    // Convenience: also produce a single concatenated KPI table so Python scripts
    // can consume the sample set without extra glue code.
    var combinedPath = Path.Combine(outputDir, "kpis.csv");
    await using (var combined = new StreamWriter(File.Create(combinedPath)))
    {
        var wroteHeader = false;
        foreach (var csv in outputFiles)
        {
            using var reader = new StreamReader(File.OpenRead(csv));
            var header = await reader.ReadLineAsync();
            if (!wroteHeader && header is not null)
            {
                await combined.WriteLineAsync(header);
                wroteHeader = true;
            }
            while (await reader.ReadLineAsync() is { } line)
            {
                if (line.Length == 0)
                {
                    continue;
                }
                await combined.WriteLineAsync(line);
            }
        }
    }
    Console.WriteLine($"Wrote combined KPI table to {combinedPath}");
    return 0;
}

var manager = new IndicatorManager();

if (args.Length == 1 && args[0] == "--samples")
{
    // Convention used by this repository.
    return await WriteDirectoryAsync(
        manager,
        Path.GetFullPath(Path.Combine("sample-data", "raw")),
        Path.GetFullPath(Path.Combine("sample-data", "kpi")));
}

if (args.Length != 2)
{
    Usage();
    return 2;
}

var inputPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);

if (Directory.Exists(inputPath))
{
    // Treat second argument as an output directory.
    return await WriteDirectoryAsync(manager, inputPath, outputPath);
}

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 2;
}

// Treat second argument as an output file.
await WriteOneAsync(manager, inputPath, outputPath);
return 0;
