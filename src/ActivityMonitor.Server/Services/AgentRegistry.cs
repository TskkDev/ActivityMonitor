using System.Collections.Concurrent;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Server.Services;

public sealed class AgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentInfo> _agents =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, Guid> _pendingScreenshots =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, StoredScreenshot> _screenshots =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly TimeSpan _offlineAfter;

    public AgentRegistry(IConfiguration configuration)
    {
        var seconds = configuration.GetValue("AgentOfflineAfterSeconds", 15);
        _offlineAfter = TimeSpan.FromSeconds(Math.Max(seconds, 5));
    }

    public AgentInfo Register(RegisterAgentRequest request, string ipAddress)
    {
        var now = DateTimeOffset.UtcNow;
        var info = new AgentInfo
        {
            AgentId = request.AgentId,
            Domain = request.Domain,
            MachineName = request.MachineName,
            UserName = request.UserName,
            IpAddress = ipAddress,
            RegisteredAtUtc = now,
            LastSeenUtc = now,
            LastActivityUtc = now,
            IsOnline = true
        };

        _agents.AddOrUpdate(request.AgentId, info, (_, existing) =>
        {
            existing.IpAddress = ipAddress;
            existing.LastSeenUtc = now;
            existing.IsOnline = true;
            return existing;
        });

        return Snapshot(_agents[request.AgentId]);
    }

    public bool TryHeartbeat(string agentId, HeartbeatRequest request, out HeartbeatResponse? response)
    {
        response = null;
        if (!_agents.TryGetValue(agentId, out var info))
            return false;

        info.LastSeenUtc = DateTimeOffset.UtcNow;
        info.LastActivityUtc = request.LastActivityUtc.ToUniversalTime();
        info.IsOnline = true;

        _pendingScreenshots.TryGetValue(agentId, out var pendingId);

        response = new HeartbeatResponse
        {
            Agent = Snapshot(info),
            PendingScreenshotRequestId = pendingId == Guid.Empty ? null : pendingId
        };
        return true;
    }

    public bool TryDisconnect(string agentId)
    {
        if (!_agents.TryGetValue(agentId, out var info))
            return false;

        info.IsOnline = false;
        info.LastSeenUtc = DateTimeOffset.UtcNow;
        _pendingScreenshots.TryRemove(agentId, out _);
        return true;
    }

    public bool TryRequestScreenshot(string agentId, out ScreenshotRequestResult? result)
    {
        result = null;
        RefreshOnlineFlags();
        if (!_agents.TryGetValue(agentId, out var info) || !info.IsOnline)
            return false;

        var requestId = Guid.NewGuid();
        _pendingScreenshots[agentId] = requestId;
        result = new ScreenshotRequestResult
        {
            AgentId = agentId,
            RequestId = requestId
        };
        return true;
    }

    public bool TryStoreScreenshot(string agentId, Guid requestId, byte[] pngBytes)
    {
        if (!_agents.TryGetValue(agentId, out _))
            return false;

        if (_pendingScreenshots.TryGetValue(agentId, out var pending) && pending == requestId)
            _pendingScreenshots.TryRemove(agentId, out _);

        _screenshots[agentId] = new StoredScreenshot(requestId, pngBytes, DateTimeOffset.UtcNow);
        return true;
    }

    public bool TryGetScreenshot(string agentId, out StoredScreenshot? screenshot)
    {
        return _screenshots.TryGetValue(agentId, out screenshot);
    }

    public IReadOnlyCollection<AgentInfo> GetAll()
    {
        RefreshOnlineFlags();
        return _agents.Values
            .Select(Snapshot)
            .OrderByDescending(a => a.IsOnline)
            .ThenBy(a => a.MachineName)
            .ThenBy(a => a.UserName)
            .ToArray();
    }

    private void RefreshOnlineFlags()
    {
        var threshold = DateTimeOffset.UtcNow - _offlineAfter;
        foreach (var agent in _agents.Values)
        {
            if (agent.IsOnline && agent.LastSeenUtc < threshold)
                agent.IsOnline = false;
        }
    }

    private static AgentInfo Snapshot(AgentInfo source) => new()
    {
        AgentId = source.AgentId,
        Domain = source.Domain,
        MachineName = source.MachineName,
        UserName = source.UserName,
        IpAddress = source.IpAddress,
        RegisteredAtUtc = source.RegisteredAtUtc,
        LastSeenUtc = source.LastSeenUtc,
        LastActivityUtc = source.LastActivityUtc,
        IsOnline = source.IsOnline
    };
}

public sealed record StoredScreenshot(Guid RequestId, byte[] PngBytes, DateTimeOffset CapturedAtUtc);
