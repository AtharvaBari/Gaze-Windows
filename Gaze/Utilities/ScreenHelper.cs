namespace Gaze.Utilities;

/// <summary>
/// Provides screen geometry calculations for the Dynamic Island overlay.
/// </summary>
public static class ScreenHelper
{
    /// <summary>
    /// Height of the Dynamic Island pill.
    /// </summary>
    public const double IslandHeight = 36;

    /// <summary>
    /// Width of the fully expanded Dynamic Island.
    /// </summary>
    public const double IslandExpandedWidth = 340;

    /// <summary>
    /// Width of the initial tiny ball.
    /// </summary>
    public const double BallSize = 12;

    /// <summary>
    /// Width of the mouse detection zone at the top-center of screen.
    /// </summary>
    public const double DetectionZoneWidth = 400;

    /// <summary>
    /// Height of the mouse detection zone.
    /// </summary>
    public const double DetectionZoneHeight = 20;

    /// <summary>
    /// Top margin for the island from screen edge.
    /// </summary>
    public const double IslandTopMargin = 8;

    /// <summary>
    /// Gets the overlay window rect — a wide transparent area at the top-center.
    /// The actual island is rendered inside this zone.
    /// </summary>
    public static (double X, double Y, double Width, double Height) GetOverlayRect()
    {
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null)
            return (0, 0, IslandExpandedWidth + 100, IslandHeight + IslandTopMargin + 60);

        double dpiScale = GetDpiScale();
        double screenWidth = screen.Bounds.Width / dpiScale;

        // Make window wide enough for the island + some detection margin
        double width = IslandExpandedWidth + 200;
        double height = IslandHeight + IslandTopMargin + 60; // Extra space for the drop animation
        double x = (screenWidth - width) / 2;
        double y = 0;

        return (x, y, width, height);
    }

    /// <summary>
    /// Gets the system DPI scale factor.
    /// </summary>
    public static double GetDpiScale()
    {
        try
        {
            using var graphics = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            return graphics.DpiX / 96.0;
        }
        catch
        {
            return 1.0;
        }
    }
}
