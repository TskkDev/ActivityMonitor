using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ActivityMonitor.Agent.Services;

internal static class ScreenCapture
{
    public static byte[] CapturePrimaryScreenPng()
    {
        var bounds = Screen.PrimaryScreen?.Bounds
                     ?? new Rectangle(0, 0, 1920, 1080);

        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
