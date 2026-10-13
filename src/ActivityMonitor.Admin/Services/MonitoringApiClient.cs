using ActivityMonitor.Shared.Agents;
using Microsoft.Extensions.Options;

namespace ActivityMonitor.Admin.Services;

public sealed class MonitoringApiClient
{
    private readonly HttpClient _http;
    private readonly MonitoringApiOptions _options;

    public MonitoringApiClient(HttpClient http, IOptions<MonitoringApiOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AgentInfo>> GetAgentsAsync(CancellationToken ct = default)
    {
        var agents = await _http.GetFromJsonAsync<List<AgentInfo>>("api/agents", ct);
        return agents ?? [];
    }

    public async Task<ScreenshotRequestResult> RequestScreenshotAsync(string agentId, CancellationToken ct = default)
    {
        var path = $"api/agents/{Uri.EscapeDataString(agentId)}/screenshot/request";
        using var response = await _http.PostAsync(path, content: null, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ScreenshotRequestResult>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Empty screenshot request response.");
    }

    public async Task<ScreenshotPayload?> TryGetScreenshotAsync(string agentId, CancellationToken ct = default)
    {
        var path = $"api/agents/{Uri.EscapeDataString(agentId)}/screenshot";
        using var response = await _http.GetAsync(path, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        Guid? requestId = null;
        if (response.Headers.TryGetValues("X-Request-Id", out var values))
        {
            var raw = values.FirstOrDefault();
            if (Guid.TryParse(raw, out var parsed))
                requestId = parsed;
        }

        return new ScreenshotPayload(agentId, bytes, requestId);
    }

    public async Task<ScreenshotPayload?> RequestAndWaitScreenshotAsync(string agentId, CancellationToken ct = default)
    {
        var requested = await RequestScreenshotAsync(agentId, ct);
        var deadline = DateTime.UtcNow.AddSeconds(_options.ScreenshotWaitSeconds);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var shot = await TryGetScreenshotAsync(agentId, ct);
            if (shot is not null && shot.RequestId == requested.RequestId)
                return shot;

            await Task.Delay(1000, ct);
        }

        return null;
    }
}

public sealed record ScreenshotPayload(string AgentId, byte[] PngBytes, Guid? RequestId);
