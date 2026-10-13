namespace ActivityMonitor.Agent.Services;

internal static class WorkstationIdentity
{
    public static string Domain => Environment.UserDomainName;
    public static string MachineName => Environment.MachineName;
    public static string UserName => Environment.UserName;
    public static string AgentId => $"{MachineName}@{UserName}".ToLowerInvariant();
}
