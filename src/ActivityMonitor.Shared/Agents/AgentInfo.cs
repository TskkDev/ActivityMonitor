namespace ActivityMonitor.Shared.Agents;

/// <summary>
/// Полное состояние агента, как его видит сервер (и потом админка).
/// </summary>
public sealed class AgentInfo
{
    public required string AgentId { get; init; }
    public required string Domain { get; init; }
    public required string MachineName { get; init; }
    public required string UserName { get; init; }
    public required string IpAddress { get; set; }
    public DateTimeOffset RegisteredAtUtc { get; init; }
    public DateTimeOffset LastSeenUtc { get; set; }
    public DateTimeOffset LastActivityUtc { get; set; }
    public bool IsOnline { get; set; }
}
