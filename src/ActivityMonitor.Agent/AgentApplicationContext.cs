using System.Text.Json;
using ActivityMonitor.Agent.Configuration;
using ActivityMonitor.Agent.Services;
using ActivityMonitor.Shared.Agents;

namespace ActivityMonitor.Agent;

internal sealed class AgentApplicationContext : ApplicationContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly NotifyIcon _tray;
    private readonly CancellationTokenSource _cts = new();
    private readonly AgentSettings _settings;
    private int _shuttingDown;

    public AgentApplicationContext()
    {
        Autostart.EnsureEnabled();
        _settings = LoadSettings();

        _tray = new NotifyIcon
        {
            Visible = true,
            Icon = SystemIcons.Application,
            Text = Truncate($"ActivityMonitor: {WorkstationIdentity.AgentId}"),
            ContextMenuStrip = BuildMenu()
        };

        Application.ApplicationExit += (_, _) => _ = DisconnectBestEffortAsync();
        _ = Task.Run(() => RunLoopAsync(_cts.Token));
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Status", null, (_, _) =>
        {
            MessageBox.Show(
                $"AgentId: {WorkstationIdentity.AgentId}\n" +
                $"Server: {_settings.ServerBaseUrl}\n" +
                $"User: {WorkstationIdentity.Domain}\\{WorkstationIdentity.UserName}\n" +
                $"Machine: {WorkstationIdentity.MachineName}",
                "ActivityMonitor Agent",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        });
        menu.Items.Add("Exit", null, async (_, _) => await ShutdownAsync());
        return menu;
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_settings.ReconnectDelaySeconds), ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task RunSessionAsync(CancellationToken ct)
    {
        using var api = new ServerApiClient(_settings.ServerBaseUrl);

        var registerRequest = new RegisterAgentRequest
        {
            AgentId = WorkstationIdentity.AgentId,
            Domain = WorkstationIdentity.Domain,
            MachineName = WorkstationIdentity.MachineName,
            UserName = WorkstationIdentity.UserName
        };

        var registered = await api.RegisterAsync(registerRequest, ct);
        SetStatus($"Online ({registered.IpAddress})");

        while (!ct.IsCancellationRequested)
        {
            var heartbeat = new HeartbeatRequest
            {
                LastActivityUtc = UserActivityTracker.GetLastInputUtc()
            };

            var response = await api.HeartbeatAsync(WorkstationIdentity.AgentId, heartbeat, ct);
            SetStatus($"Online | activity {heartbeat.LastActivityUtc.ToLocalTime():HH:mm:ss}");

            if (response.PendingScreenshotRequestId is Guid requestId)
            {
                SetStatus("Capturing screenshot...");
                var png = await Task.Run(ScreenCapture.CapturePrimaryScreenPng, ct);
                await api.UploadScreenshotAsync(WorkstationIdentity.AgentId, requestId, png, ct);
                SetStatus($"Screenshot sent ({png.Length} bytes)");
            }

            await Task.Delay(TimeSpan.FromSeconds(_settings.HeartbeatIntervalSeconds), ct);
        }
    }

    private void SetStatus(string status)
    {
        void Apply()
        {
            _tray.Text = Truncate($"AM: {status}");
        }

        if (_tray.ContextMenuStrip?.InvokeRequired == true)
            _tray.ContextMenuStrip.BeginInvoke(Apply);
        else
            Apply();
    }

    private async Task ShutdownAsync()
    {
        if (Interlocked.Exchange(ref _shuttingDown, 1) == 1)
            return;

        SetStatus("Disconnecting...");
        await DisconnectBestEffortAsync();
        _cts.Cancel();
        _tray.Visible = false;
        _tray.Dispose();
        _cts.Dispose();
        ExitThread();
    }

    private async Task DisconnectBestEffortAsync()
    {
        try
        {
            using var api = new ServerApiClient(_settings.ServerBaseUrl);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await api.DisconnectAsync(WorkstationIdentity.AgentId, timeout.Token);
        }
        catch
        {
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Interlocked.Exchange(ref _shuttingDown, 1) == 0)
                DisconnectBestEffortAsync().GetAwaiter().GetResult();

            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
            _tray.Visible = false;
            _tray.Dispose();
            _cts.Dispose();
        }

        base.Dispose(disposing);
    }

    private static AgentSettings LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            return new AgentSettings();

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AgentSettings>(json, JsonOptions) ?? new AgentSettings();
    }

    private static string Truncate(string text)
    {
        const int max = 63;
        return text.Length <= max ? text : text[..max];
    }
}
