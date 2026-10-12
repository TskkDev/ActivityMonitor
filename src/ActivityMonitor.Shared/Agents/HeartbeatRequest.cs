namespace ActivityMonitor.Shared.Agents;

/// <summary>
/// Периодический «пульс» агента: я жив + когда была последняя активность пользователя.
/// </summary>
public sealed class HeartbeatRequest
{
    public required DateTimeOffset LastActivityUtc { get; init; }
}
