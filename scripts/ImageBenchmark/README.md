# ImageBenchmark

Compares the two WebP converters of the import pipeline on a real comic:

| Engine | Class | Notes |
|---|---|---|
| `imagesharp` | `ImageProcessorService` | Current converter: full decode, then resize. PNG pages come out as **lossless** WebP. |
| `skia` | `SkiaImageProcessorService` | JPEG decoded at reduced scale (1/2, 1/4, 1/8), halving steps above a 2× ratio, then a Mitchell resize; always lossy WebP (quality 75, ImageSharp's default). |

Each engine runs in its **own child process**, one after the other: the peak memory (VmHWM, native
buffers included) of one engine is not polluted by the other. The workers use the Server GC, like
the web app.

## Run on the Raspberry Pi (64-bit OS)

On a development machine:

```bash
dotnet publish scripts/ImageBenchmark -c Release -r linux-arm64 --self-contained -o out/image-benchmark
scp -r out/image-benchmark pi@<pi>:~/image-benchmark
```

On the Pi (no .NET install needed, the tool is self-contained):

```bash
cd ~/image-benchmark
chmod +x ImageBenchmark   # the execute bit is lost when copying through Windows or a zip
./ImageBenchmark ~/comics/MyComic.cbz --runs 3 --out ~/bench
```

Stop the MCM container first (or run when no import is in progress) so both measures share the
same conditions.

## Options

| Option | Default | Description |
|---|---|---|
| `--width` | `1400` | Target width (`Import:ImageTargetWidth`); double pages get twice the width |
| `--out` | `./image-benchmark` | Working directory: extracted pages, converted pages per engine, `report.md` |
| `--runs` | `1` | Runs per engine (time = median, memory = max) |
| `--engines` | `imagesharp,skia` | Engines to run |
| `--no-quality` | | Skip the PSNR measure (it decodes every page again, slow on the Pi) |
| `--gc` | `server` | `server` (like the web app) or `workstation` |

## Report

- **Time** and **per page**: conversion only (extraction excluded).
- **Peak memory**: largest resident set of the worker process; **above idle**: minus the process at rest.
- **Managed alloc.**: bytes allocated by the .NET GC (ImageSharp works in managed memory, Skia in native memory).
- **Output**: total size of the WebP pages; **lossless pages**: pages encoded as VP8L.
- **PSNR JPEG pages**: quality against a reference (full decode + Lanczos3, no compression), on JPEG pages only.

The converted pages stay in `<out>/imagesharp/` and `<out>/skia/` for a visual comparison.
