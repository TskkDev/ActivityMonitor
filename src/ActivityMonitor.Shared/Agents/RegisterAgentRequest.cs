namespace ActivityMonitor.Shared.Agents;

public sealed class RegisterAgentRequest
{
    public required string AgentId { get; init; }
    public required string Domain { get; init; }
    public required string MachineName { get; init; }
    public required string UserName { get; init; }
}
