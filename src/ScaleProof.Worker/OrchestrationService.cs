using Microsoft.Extensions.Hosting;

namespace ScaleProof.Worker;

public sealed class OrchestrationService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Scaffold only: server leases, agent dispatch, evidence admission,
        // deterministic analysis, and finalization belong in worker slices.
        // Target traffic and k6 process execution belong to the local agent.
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
