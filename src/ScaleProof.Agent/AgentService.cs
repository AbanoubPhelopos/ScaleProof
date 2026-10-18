using Microsoft.Extensions.Hosting;

namespace ScaleProof.Agent;

public sealed class AgentService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Scaffold only: enrollment, outbound control, local policy enforcement,
        // k6 execution, redacted uploads, and local AI belong in agent slices.
        // This service does not claim a server lease or determine run outcomes.
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
