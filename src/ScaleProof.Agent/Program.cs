using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScaleProof.Agent;

var builder = Host.CreateApplicationBuilder(args);
// One executable owns command and persistent-agent modes (Proposed ADR-022).
// Command dispatch, enrollment, and local execution are not implemented yet.
builder.Services.AddHostedService<AgentService>();

var host = builder.Build();
host.Run();
