namespace ActivityMonitor.Admin.Services;

public sealed class MonitoringApiOptions
{
    public const string SectionName = "MonitoringApi";

    public string BaseUrl { get; set; } = "http://localhost:5253";
    public int ScreenshotWaitSeconds { get; set; } = 20;
}
