namespace ActivityMonitor.Shared.Agents;

public sealed class HeartbeatResponse
{
    public required AgentInfo Agent { get; init; }
    public Guid? PendingScreenshotRequestId { get; init; }
}
