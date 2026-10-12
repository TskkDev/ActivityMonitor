using System.Collections.Concurrent;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Server.Services;

/// <summary>
/// In-memory реестр подключённых агентов.
/// Позже при желании можно заменить на БД — контракт API останется тем же.
/// </summary>
public sealed class AgentRegistry
{
    private readonly ConcurrentDictionary<string, AgentInfo> _agents =
        new(StringComparer.OrdinalIgnoreCase);

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

        return _agents[request.AgentId];
    }

    public bool TryHeartbeat(string agentId, HeartbeatRequest request, out AgentInfo? info)
    {
        if (!_agents.TryGetValue(agentId, out info))
            return false;

        info.LastSeenUtc = DateTimeOffset.UtcNow;
        info.LastActivityUtc = request.LastActivityUtc.ToUniversalTime();
        info.IsOnline = true;
        return true;
    }

    public IReadOnlyCollection<AgentInfo> GetAll() =>
        _agents.Values
            .OrderByDescending(a => a.IsOnline)
            .ThenBy(a => a.MachineName)
            .ThenBy(a => a.UserName)
            .ToArray();
}
