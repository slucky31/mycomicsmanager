/*
 * ImageBenchmark — compares the two WebP converters of the import pipeline on a real comic
 * ------------------------------------------------------------------------------------------
 * ImageSharp (current: ImageProcessorService) vs SkiaSharp (SkiaImageProcessorService).
 *
 * Usage:
 *   ImageBenchmark <comic.cbz|cbr> [--width 1400] [--out <dir>] [--runs 1]
 *                  [--engines imagesharp,skia] [--no-quality] [--gc server|workstation]
 *
 * Each engine runs in its own child process, one after the other, so the peak memory (VmHWM:
 * the largest resident set of the process, native Skia/ImageSharp buffers included) is measured
 * without one engine polluting the other. The archive is extracted once with the real
 * ArchiveExtractorService, exactly as the import pipeline does.
 *
 * Report (console + <out>/report.md):
 *   - time (total and per page), peak memory and memory above the idle process, managed allocations;
 *   - output size, lossless pages (VP8L, the PNG bug of the ImageSharp converter);
 *   - quality: PSNR of each output against a reference (full decode + Lanczos3, no compression),
 *     on the JPEG pages (both engines encode them as lossy WebP).
 * The converted pages stay in <out>/<engine>/ for a visual comparison.
 */

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Application.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

const string workerFlag = "--worker";
const string resultPrefix = "RESULT ";

if (args.Length > 0 && args[0] == workerFlag)
{
    return await RunWorkerAsync(args[1..]);
}

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintUsage();
    return args.Length == 0 ? 1 : 0;
}

return await RunBenchmarkAsync(args);

// ─── Parent: extraction, one child process per engine and run, report ─────────────────────────────

static async Task<int> RunBenchmarkAsync(string[] args)
{
    var options = BenchmarkOptions.Parse(args);
    if (options is null)
    {
        PrintUsage();
        return 1;
    }

    if (!File.Exists(options.Archive))
    {
        Console.Error.WriteLine($"File not found: {options.Archive}");
        return 1;
    }

    Directory.CreateDirectory(options.OutputDirectory);
    var sourceDirectory = Path.Combine(options.OutputDirectory, "source");
    if (Directory.Exists(sourceDirectory))
    {
        Directory.Delete(sourceDirectory, recursive: true);
    }

    Console.WriteLine($"Extracting {Path.GetFileName(options.Archive)}...");
    var extractor = new ArchiveExtractorService(NullLogger<ArchiveExtractorService>.Instance);
    var extraction = await extractor.ExtractAsync(options.Archive, sourceDirectory);
    if (extraction.IsFailure)
    {
        Console.Error.WriteLine($"Extraction failed: {extraction.Error!.Description}");
        return 1;
    }

    var sources = Directory.GetFiles(sourceDirectory).Where(IsImage).ToList();
    var sourceBytes = sources.Sum(f => new FileInfo(f).Length);
    var sourceFormats = string.Join(", ", sources
        .GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
        .OrderByDescending(g => g.Count())
        .Select(g => $"{g.Count()} {g.Key}"));
    Console.WriteLine($"{sources.Count} pages ({sourceFormats}), {Mb(sourceBytes)} MB, target width {options.Width}px, {options.Gc} GC");

    var results = new List<EngineResult>();
    foreach (var engine in options.Engines)
    {
        var runs = new List<WorkerMetrics>();
        for (var run = 1; run <= options.Runs; run++)
        {
            Console.WriteLine($"[{engine}] run {run}/{options.Runs}...");
            var engineOutput = Path.Combine(options.OutputDirectory, engine);
            if (Directory.Exists(engineOutput))
            {
                Directory.Delete(engineOutput, recursive: true);
            }

            var metrics = await RunWorkerProcessAsync(engine, sourceDirectory, engineOutput, options);
            if (metrics is null)
            {
                return 1;
            }
            runs.Add(metrics);
        }

        var outputDirectory = Path.Combine(options.OutputDirectory, engine);
        results.Add(new EngineResult(engine, runs, AnalyzeOutput(outputDirectory)));
    }

    if (options.Quality)
    {
        Console.WriteLine("Measuring quality against the reference (full decode + Lanczos3)...");
        foreach (var result in results)
        {
            result.Psnr = await MeasureQualityAsync(sourceDirectory, Path.Combine(options.OutputDirectory, result.Engine), options.Width);
        }
    }

    var report = BuildReport(options, sources.Count, sourceFormats, sourceBytes, results);
    Console.WriteLine();
    Console.WriteLine(report);
    var reportPath = Path.Combine(options.OutputDirectory, "report.md");
    await File.WriteAllTextAsync(reportPath, report);
    Console.WriteLine($"Report: {reportPath} — converted pages: {options.OutputDirectory}/<engine>/");
    return 0;
}

static async Task<WorkerMetrics?> RunWorkerProcessAsync(string engine, string source, string output, BenchmarkOptions options)
{
    var start = new ProcessStartInfo
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };

    // Self-contained / apphost: the process is the tool itself; framework-dependent: "dotnet ImageBenchmark.dll".
    var processPath = Environment.ProcessPath!;
    start.FileName = processPath;
    if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
    {
        start.ArgumentList.Add(typeof(BenchmarkOptions).Assembly.Location);
    }

    foreach (var arg in new[] { workerFlag, engine, source, output, options.Width.ToString(CultureInfo.InvariantCulture) })
    {
        start.ArgumentList.Add(arg);
    }
    start.Environment["DOTNET_gcServer"] = options.Gc == "server" ? "1" : "0";

    using var process = Process.Start(start)!;
    var stdout = await process.StandardOutput.ReadToEndAsync();
    var stderr = await process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    var line = stdout.Split('\n').FirstOrDefault(l => l.StartsWith(resultPrefix, StringComparison.Ordinal));
    if (process.ExitCode != 0 || line is null)
    {
        Console.Error.WriteLine($"[{engine}] failed (exit code {process.ExitCode})");
        Console.Error.WriteLine(stdout);
        Console.Error.WriteLine(stderr);
        return null;
    }

    var metrics = JsonSerializer.Deserialize<WorkerMetrics>(line[resultPrefix.Length..])!;
    if (!string.IsNullOrEmpty(metrics.Error))
    {
        Console.Error.WriteLine($"[{engine}] conversion failed: {metrics.Error}");
        return null;
    }

    Console.WriteLine($"[{engine}] {metrics.ElapsedMs / 1000.0:F1} s, peak {Mb(metrics.PeakRssBytes)} MB");
    return metrics;
}

// ─── Worker: one engine, one run, in a fresh process ─────────────────────────────────────────────

static async Task<int> RunWorkerAsync(string[] args)
{
    var (engine, source, output) = (args[0], args[1], args[2]);
    var width = int.Parse(args[3], CultureInfo.InvariantCulture);

    IImageProcessor processor = engine switch
    {
        "imagesharp" => new ImageProcessorService(NullLogger<ImageProcessorService>.Instance),
        "skia" => new SkiaImageProcessorService(NullLogger<SkiaImageProcessorService>.Instance),
        _ => throw new ArgumentException($"Unknown engine '{engine}' (imagesharp, skia)")
    };

    using var self = Process.GetCurrentProcess();
    self.Refresh();
    var idleRss = self.WorkingSet64;
    var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
    var gen2Before = GC.CollectionCount(2);

    var stopwatch = Stopwatch.StartNew();
    var result = await processor.ProcessImagesAsync(source, output, width);
    stopwatch.Stop();

    self.Refresh();
    var metrics = new WorkerMetrics
    {
        ElapsedMs = stopwatch.ElapsedMilliseconds,
        IdleRssBytes = idleRss,
        PeakRssBytes = self.PeakWorkingSet64,
        AllocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore,
        Gen2Collections = GC.CollectionCount(2) - gen2Before,
        Pages = result.IsSuccess ? result.Value!.ProcessedCount + result.Value.SkippedCount : 0,
        Error = result.IsFailure ? result.Error!.Description : null
    };

    Console.WriteLine(resultPrefix + JsonSerializer.Serialize(metrics));
    return 0;
}

// ─── Output analysis and quality ─────────────────────────────────────────────────────────────────

static OutputStats AnalyzeOutput(string directory)
{
    var pages = Directory.GetFiles(directory, "*.webp");
    var lossless = pages.Count(IsLosslessWebp);
    return new OutputStats(pages.Length, pages.Sum(f => new FileInfo(f).Length), lossless);
}

// The WebP bitstream is "VP8 " (lossy) or "VP8L" (lossless), possibly after a "VP8X" extended header.
static bool IsLosslessWebp(string path)
{
    using var stream = File.OpenRead(path);
    var header = new byte[Math.Min(4096, stream.Length)];
    stream.ReadExactly(header);
    return Encoding.ASCII.GetString(header).Contains("VP8L", StringComparison.Ordinal);
}

static async Task<double> MeasureQualityAsync(string sourceDirectory, string outputDirectory, int width)
{
    var sources = Directory.GetFiles(sourceDirectory).Where(IsImage).OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    var psnrs = new List<double>();
    for (var i = 0; i < sources.Count; i++)
    {
        // PNG pages are left out: ImageSharp encodes them losslessly, which would not compare like for like.
        var converted = Path.Combine(outputDirectory, $"page-{i + 1:D3}.webp");
        if (!File.Exists(converted) || Path.GetExtension(sources[i]).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        using var reference = await Image.LoadAsync<Rgb24>(sources[i]);
        var effectiveWidth = reference.Width > reference.Height ? width * 2 : width;
        var height = (int)Math.Round((double)reference.Height * effectiveWidth / reference.Width);
        reference.Mutate(x => x.Resize(effectiveWidth, height, KnownResamplers.Lanczos3));

        using var output = await Image.LoadAsync<Rgb24>(converted);
        if (output.Width == reference.Width && output.Height == reference.Height)
        {
            psnrs.Add(Psnr(reference, output));
        }
    }

    return psnrs.Count == 0 ? double.NaN : psnrs.Average();
}

static double Psnr(Image<Rgb24> expected, Image<Rgb24> actual)
{
    double sum = 0;
    long count = 0;
    expected.ProcessPixelRows(actual, (a, b) =>
    {
        for (var y = 0; y < a.Height; y++)
        {
            var rowA = a.GetRowSpan(y);
            var rowB = b.GetRowSpan(y);
            for (var x = 0; x < rowA.Length; x++)
            {
                double dr = rowA[x].R - rowB[x].R, dg = rowA[x].G - rowB[x].G, db = rowA[x].B - rowB[x].B;
                sum += dr * dr + dg * dg + db * db;
            }
            count += rowA.Length * 3L;
        }
    });

    var mse = sum / count;
    return mse == 0 ? 99 : 10 * Math.Log10(255.0 * 255.0 / mse);
}

// ─── Report ──────────────────────────────────────────────────────────────────────────────────────

static string BuildReport(BenchmarkOptions options, int pages, string formats, long sourceBytes, List<EngineResult> results)
{
    var sb = new StringBuilder();
    sb.AppendLine(CultureInfo.InvariantCulture, $"# ImageBenchmark — {Path.GetFileName(options.Archive)}");
    sb.AppendLine();
    sb.AppendLine(CultureInfo.InvariantCulture, $"- Machine: {Environment.MachineName}, {Environment.ProcessorCount} cores, {System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}, .NET {Environment.Version}");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- Source: {pages} pages ({formats}), {Mb(sourceBytes)} MB");
    sb.AppendLine(CultureInfo.InvariantCulture, $"- Target width: {options.Width}px (double pages: {options.Width * 2}px), {options.Gc} GC, {options.Runs} run(s) per engine");
    sb.AppendLine();
    sb.AppendLine("| Engine | Time | Per page | Peak memory | Above idle | Managed alloc. | Output | Lossless pages | PSNR JPEG pages (dB) |");
    sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
    foreach (var r in results)
    {
        var time = Median(r.Runs.Select(m => (double)m.ElapsedMs)) / 1000.0;
        var peak = r.Runs.Max(m => m.PeakRssBytes);
        var aboveIdle = r.Runs.Max(m => m.PeakRssBytes - m.IdleRssBytes);
        var alloc = r.Runs.Max(m => m.AllocatedBytes);
        var psnr = double.IsNaN(r.Psnr) ? "-" : r.Psnr.ToString("F2", CultureInfo.InvariantCulture);
        sb.AppendLine(CultureInfo.InvariantCulture,
            $"| {r.Engine} | {time:F1} s | {time * 1000 / Math.Max(1, pages):F0} ms | {Mb(peak)} MB | {Mb(aboveIdle)} MB | {Mb(alloc)} MB | {Mb(r.Output.TotalBytes)} MB | {r.Output.LosslessPages}/{r.Output.Pages} | {psnr} |");
    }

    sb.AppendLine();
    sb.AppendLine("Peak memory = VmHWM of the process (native buffers included). Time = median of the runs.");
    sb.AppendLine("PSNR against a reference resized with Lanczos3 without compression: the higher the better (> 35 dB: differences hard to see).");
    return sb.ToString();
}

static double Median(IEnumerable<double> values)
{
    var sorted = values.Order().ToList();
    return sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2;
}

static string Mb(long bytes) => (bytes / 1024.0 / 1024.0).ToString("F1", CultureInfo.InvariantCulture);

static bool IsImage(string path) =>
    Path.GetExtension(path).ToLowerInvariant() is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"
    && !Path.GetFileName(path).StartsWith('.');

static void PrintUsage() => Console.WriteLine("""
    Usage: ImageBenchmark <comic.cbz|cbr> [--width 1400] [--out <dir>] [--runs 1]
                          [--engines imagesharp,skia] [--no-quality] [--gc server|workstation]
    """);

internal sealed record BenchmarkOptions(
    string Archive, int Width, string OutputDirectory, int Runs, IReadOnlyList<string> Engines, bool Quality, string Gc)
{
    public static BenchmarkOptions? Parse(string[] args)
    {
        var archive = args[0];
        var width = 1400;
        var output = Path.Combine(Directory.GetCurrentDirectory(), "image-benchmark");
        var runs = 1;
        IReadOnlyList<string> engines = ["imagesharp", "skia"];
        var quality = true;
        var gc = "server";

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--width" when i + 1 < args.Length:
                    width = int.Parse(args[++i], CultureInfo.InvariantCulture);
                    break;
                case "--out" when i + 1 < args.Length:
                    output = Path.GetFullPath(args[++i]);
                    break;
                case "--runs" when i + 1 < args.Length:
                    runs = Math.Max(1, int.Parse(args[++i], CultureInfo.InvariantCulture));
                    break;
                case "--engines" when i + 1 < args.Length:
                    engines = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    break;
                case "--no-quality":
                    quality = false;
                    break;
                case "--gc" when i + 1 < args.Length && args[i + 1] is "server" or "workstation":
                    gc = args[++i];
                    break;
                default:
                    return null;
            }
        }

        return new BenchmarkOptions(Path.GetFullPath(archive), width, output, runs, engines, quality, gc);
    }
}

internal sealed record WorkerMetrics
{
    public long ElapsedMs { get; init; }
    public long IdleRssBytes { get; init; }
    public long PeakRssBytes { get; init; }
    public long AllocatedBytes { get; init; }
    public int Gen2Collections { get; init; }
    public int Pages { get; init; }
    public string? Error { get; init; }
}

internal sealed record OutputStats(int Pages, long TotalBytes, int LosslessPages);

internal sealed class EngineResult(string engine, List<WorkerMetrics> runs, OutputStats output)
{
    public string Engine { get; } = engine;
    public List<WorkerMetrics> Runs { get; } = runs;
    public OutputStats Output { get; } = output;
    public double Psnr { get; set; } = double.NaN;
}
