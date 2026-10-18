using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScaleProof.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<OrchestrationService>();

var host = builder.Build();
host.Run();
