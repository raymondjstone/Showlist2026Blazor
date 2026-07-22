using System;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Showlist2026.Configuration;

namespace Showlist2026.Web.Health
{
    public class TvMazeHealthCheck : IHealthCheck
    {
        private readonly ShowlistOptions _options;

        public TvMazeHealthCheck(IOptions<ShowlistOptions> options)
        {
            _options = options.Value;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Any 2xx reaches here without throwing; a non-2xx status throws
                // FlurlHttpException below instead (Flurl's default behavior), which is why the
                // 429/other-status handling lives in the catch clauses, not as response checks.
                await $"{_options.TvMazeBaseUrl}/shows/1"
                    .WithTimeout(10)
                    .GetAsync(cancellationToken: cancellationToken);

                return HealthCheckResult.Healthy("TVMaze API is reachable");
            }
            catch (FlurlHttpException ex) when (ex.StatusCode == 429)
            {
                return HealthCheckResult.Degraded("TVMaze API is rate-limiting", ex);
            }
            catch (FlurlHttpException ex)
            {
                return HealthCheckResult.Unhealthy($"TVMaze API returned status {ex.StatusCode}", ex);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("TVMaze API is unreachable", ex);
            }
        }
    }
}
