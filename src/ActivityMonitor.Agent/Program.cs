using System.Text.Json;
using ActivityMonitor.Agent.Configuration;
using ActivityMonitor.Agent.Services;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Agent;

/// <summary>
/// Точка входа агента.
/// Пока это обычная консоль (удобно отлаживать).
/// Позже спрячем окно и добавим автозапуск при логине Windows.
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static async Task<int> Main(string[] args)
    {
        Console.Title = "ActivityMonitor Agent";
        Console.WriteLine("ActivityMonitor Agent starting...");

        var settings = LoadSettings();
        Console.WriteLine($"Server: {settings.ServerBaseUrl}");
        Console.WriteLine($"Identity: {WorkstationIdentity.Domain}\\{WorkstationIdentity.UserName} @ {WorkstationIdentity.MachineName}");
        Console.WriteLine($"AgentId:  {WorkstationIdentity.AgentId}");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Главный цикл: если сервер недоступен — ждём и пробуем снова.
        while (!cts.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(settings, cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Session error: {ex.Message}");
                Console.WriteLine($"Reconnect in {settings.ReconnectDelaySeconds}s...");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(settings.ReconnectDelaySeconds), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        Console.WriteLine("Agent stopped.");
        return 0;
    }

    private static async Task RunSessionAsync(AgentSettings settings, CancellationToken ct)
    {
        using var api = new ServerApiClient(settings.ServerBaseUrl);

        var registerRequest = new RegisterAgentRequest
        {
            AgentId = WorkstationIdentity.AgentId,
            Domain = WorkstationIdentity.Domain,
            MachineName = WorkstationIdentity.MachineName,
            UserName = WorkstationIdentity.UserName
        };

        var registered = await api.RegisterAsync(registerRequest, ct);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Registered. IP seen by server: {registered.IpAddress}");

        while (!ct.IsCancellationRequested)
        {
            var heartbeat = new HeartbeatRequest
            {
                LastActivityUtc = UserActivityTracker.GetLastInputUtc()
            };

            await api.HeartbeatAsync(WorkstationIdentity.AgentId, heartbeat, ct);
            Console.WriteLine(
                $"[{DateTime.Now:HH:mm:ss}] Heartbeat OK | last activity (local): {heartbeat.LastActivityUtc.ToLocalTime():HH:mm:ss}");

            await Task.Delay(TimeSpan.FromSeconds(settings.HeartbeatIntervalSeconds), ct);
        }
    }

    private static AgentSettings LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AgentSettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AgentSettings>(json, JsonOptions) ?? new AgentSettings();
    }
}
