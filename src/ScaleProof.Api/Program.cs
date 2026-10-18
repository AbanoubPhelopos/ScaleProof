var builder = WebApplication.CreateBuilder(args);

// Composition root only. Feature endpoints, the agent gateway, persistence,
// and authorization are not implemented in this scaffold. This process must
// not launch k6 or send target traffic (Proposed ADR-022).
var app = builder.Build();

app.Run();
