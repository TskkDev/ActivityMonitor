using System.Net.Http.Json;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Agent.Services;

internal sealed class ServerApiClient : IDisposable
{
    private readonly HttpClient _http;

    public ServerApiClient(string serverBaseUrl)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(serverBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<AgentInfo> RegisterAsync(RegisterAgentRequest request, CancellationToken ct)
    {
        using var response = await _http.PostAsJsonAsync("api/agents/register", request, ct);
        response.EnsureSuccessStatusCode();
        var info = await response.Content.ReadFromJsonAsync<AgentInfo>(cancellationToken: ct);
        return info ?? throw new InvalidOperationException("Server returned empty registration response.");
    }

    public async Task<HeartbeatResponse> HeartbeatAsync(string agentId, HeartbeatRequest request, CancellationToken ct)
    {
        var path = $"api/agents/{Uri.EscapeDataString(agentId)}/heartbeat";
        using var response = await _http.PostAsJsonAsync(path, request, ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<HeartbeatResponse>(cancellationToken: ct);
        return body ?? throw new InvalidOperationException("Server returned empty heartbeat response.");
    }

    public async Task UploadScreenshotAsync(string agentId, Guid requestId, byte[] pngBytes, CancellationToken ct)
    {
        var path = $"api/agents/{Uri.EscapeDataString(agentId)}/screenshot?requestId={requestId:D}";
        using var content = new ByteArrayContent(pngBytes);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        using var response = await _http.PostAsync(path, content, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisconnectAsync(string agentId, CancellationToken ct = default)
    {
        var path = $"api/agents/{Uri.EscapeDataString(agentId)}/disconnect";
        using var response = await _http.PostAsync(path, content: null, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}

