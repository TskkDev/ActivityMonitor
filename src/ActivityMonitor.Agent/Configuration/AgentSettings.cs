namespace ActivityMonitor.Agent.Configuration;

internal sealed class AgentSettings
{
    public string ServerBaseUrl { get; set; } = "http://localhost:5253";
    public int HeartbeatIntervalSeconds { get; set; } = 5;
    public int ReconnectDelaySeconds { get; set; } = 3;
}
