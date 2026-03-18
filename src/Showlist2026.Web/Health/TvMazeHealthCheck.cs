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
                var response = await $"{_options.TvMazeBaseUrl}/shows/1"
                    .WithTimeout(10)
                    .GetAsync(cancellationToken: cancellationToken);

                if (response.StatusCode == 200)
                    return HealthCheckResult.Healthy("TVMaze API is reachable");

                if (response.StatusCode == 429)
                    return HealthCheckResult.Degraded("TVMaze API is rate-limiting");

                return HealthCheckResult.Unhealthy($"TVMaze API returned status {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("TVMaze API is unreachable", ex);
            }
        }
    }
}
