namespace ActivityMonitor.Agent.Services;

/// <summary>
/// Собирает «кто я»: домен / машина / пользователь.
/// Это то, что потом показывается в списке клиентов на сервере.
/// </summary>
internal static class WorkstationIdentity
{
    public static string Domain => Environment.UserDomainName;
    public static string MachineName => Environment.MachineName;
    public static string UserName => Environment.UserName;

    /// <summary>
    /// Стабильный id в рамках одной машины+пользователя.
    /// </summary>
    public static string AgentId => $"{MachineName}\\{UserName}".ToLowerInvariant();
}
