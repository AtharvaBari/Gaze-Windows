using System.Windows.Media;

namespace Gaze.Models;

/// <summary>
/// Represents the current mode of the Pomodoro timer.
/// </summary>
public enum TimerMode
{
    Idle,
    Countdown,
    Work,
    Break,
    Completed
}

public static class TimerModeExtensions
{
    /// <summary>
    /// Returns the display color for timer text in this mode.
    /// </summary>
    public static Color GetTextColor(this TimerMode mode) => mode switch
    {
        TimerMode.Break => Color.FromArgb(230, 255, 165, 0),     // Orange
        TimerMode.Completed => Color.FromArgb(230, 0, 200, 80),  // Green
        _ => Color.FromArgb(255, 255, 255, 255)                   // White
    };
}
