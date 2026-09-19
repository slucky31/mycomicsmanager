using Application.ImportJobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Web.Infrastructure;

internal sealed class ImportDirectoryHealthCheck(IOptions<ImportSettings> settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var dir = settings.Value.ImportDirectory;
        try
        {
            Directory.CreateDirectory(dir);
            var testFile = Path.Combine(dir, ".healthcheck");
            await File.WriteAllTextAsync(testFile, "ok", cancellationToken);
            File.Delete(testFile);
            return HealthCheckResult.Healthy($"Import directory '{dir}' is writable.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy($"Import directory '{dir}' is not writable.", ex);
        }
    }
}
