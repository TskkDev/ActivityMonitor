namespace ActivityMonitor.Shared.Agents;

public sealed class ScreenshotRequestResult
{
    public required string AgentId { get; init; }
    public required Guid RequestId { get; init; }
}
