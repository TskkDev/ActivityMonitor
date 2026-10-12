using ActivityMonitor.Server.Services;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Server.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agents").WithTags("Agents");

        group.MapPost("/register", (RegisterAgentRequest request, HttpContext http, AgentRegistry registry) =>
            {
                var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var info = registry.Register(request, ip);
                return Results.Ok(info);
            })
            .WithName("RegisterAgent")
            .WithSummary("Регистрация / повторное подключение агента");

        group.MapPost("/{agentId}/heartbeat", (string agentId, HeartbeatRequest request, AgentRegistry registry) =>
            {
                if (!registry.TryHeartbeat(agentId, request, out var info) || info is null)
                    return Results.NotFound(new { message = $"Agent '{agentId}' is not registered." });

                return Results.Ok(info);
            })
            .WithName("AgentHeartbeat")
            .WithSummary("Heartbeat + время последней активности");

        group.MapGet("/", (AgentRegistry registry) => Results.Ok(registry.GetAll()))
            .WithName("ListAgents")
            .WithSummary("Список всех известных агентов");

        return app;
    }
}
