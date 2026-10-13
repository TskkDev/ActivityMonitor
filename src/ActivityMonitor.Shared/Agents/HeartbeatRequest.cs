namespace ActivityMonitor.Shared.Agents;

public sealed class HeartbeatRequest
{
    public required DateTimeOffset LastActivityUtc { get; init; }
}
