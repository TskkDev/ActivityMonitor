namespace ActivityMonitor.Shared.Agents;

/// <summary>
/// Тело запроса при первом подключении агента к серверу.
/// </summary>
public sealed class RegisterAgentRequest
{
    public required string AgentId { get; init; }
    public required string Domain { get; init; }
    public required string MachineName { get; init; }
    public required string UserName { get; init; }
}
