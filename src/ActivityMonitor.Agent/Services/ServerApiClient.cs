using System.Net.Http.Json;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Agent.Services;

/// <summary>
/// Тонкая обёртка над HttpClient: только запросы, которые агент шлёт на сервер.
/// </summary>
internal sealed class ServerApiClient : IDisposable
{
    private readonly HttpClient _http;

    public ServerApiClient(string serverBaseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(serverBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<AgentInfo> RegisterAsync(RegisterAgentRequest request, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("api/agents/register", request, ct);
        response.EnsureSuccessStatusCode();
        var info = await response.Content.ReadFromJsonAsync<AgentInfo>(cancellationToken: ct);
        return info ?? throw new InvalidOperationException("Server returned empty registration response.");
    }

    public async Task HeartbeatAsync(string agentId, HeartbeatRequest request, CancellationToken ct)
    {
        var path = $"api/agents/{Uri.EscapeDataString(agentId)}/heartbeat";
        using var response = await _http.PostAsJsonAsync(path, request, ct);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}
