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
            .WithSummary("Register agent");

        group.MapPost("/{agentId}/heartbeat", (string agentId, HeartbeatRequest request, AgentRegistry registry) =>
            {
                if (!registry.TryHeartbeat(agentId, request, out var response) || response is null)
                    return Results.NotFound(new { message = $"Agent '{agentId}' is not registered." });

                return Results.Ok(response);
            })
            .WithName("AgentHeartbeat")
            .WithSummary("Heartbeat and pending commands");

        group.MapPost("/{agentId}/screenshot/request", (string agentId, AgentRegistry registry) =>
            {
                if (!registry.TryRequestScreenshot(agentId, out var result) || result is null)
                    return Results.NotFound(new { message = $"Agent '{agentId}' is not registered." });

                return Results.Accepted(value: result);
            })
            .WithName("RequestScreenshot")
            .WithSummary("Queue screenshot capture for agent");

        group.MapPost("/{agentId}/screenshot", async (string agentId, Guid requestId, HttpRequest httpRequest, AgentRegistry registry) =>
            {
                using var memory = new MemoryStream();
                await httpRequest.Body.CopyToAsync(memory);
                var bytes = memory.ToArray();
                if (bytes.Length == 0)
                    return Results.BadRequest(new { message = "Empty screenshot body." });

                if (!registry.TryStoreScreenshot(agentId, requestId, bytes))
                    return Results.NotFound(new { message = $"Agent '{agentId}' is not registered." });

                return Results.NoContent();
            })
            .WithName("UploadScreenshot")
            .WithSummary("Upload PNG screenshot from agent")
            .DisableAntiforgery()
            .Accepts<byte[]>("image/png");

        group.MapGet("/{agentId}/screenshot", (string agentId, AgentRegistry registry) =>
            {
                if (!registry.TryGetScreenshot(agentId, out var screenshot) || screenshot is null)
                    return Results.NotFound(new { message = $"No screenshot for agent '{agentId}'." });

                return Results.File(screenshot.PngBytes, "image/png", $"screenshot-{agentId}.png");
            })
            .WithName("GetScreenshot")
            .WithSummary("Download latest screenshot");

        group.MapGet("/", (AgentRegistry registry) => Results.Ok(registry.GetAll()))
            .WithName("ListAgents")
            .WithSummary("List agents");

        return app;
    }
}
