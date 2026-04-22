namespace Gaze.Models;

/// <summary>
/// Represents the behavioral state of the eye mascot.
/// Each state defines different blink and look timing intervals.
/// </summary>
public enum EyeState
{
    Idle,
    Focused,
    Relaxed,
    Sleeping
}

public static class EyeStateExtensions
{
    /// <summary>
    /// Returns the min/max blink interval in seconds for this state.
    /// </summary>
    public static (double Min, double Max) GetBlinkInterval(this EyeState state) => state switch
    {
        EyeState.Idle => (3.0, 6.0),
        EyeState.Focused => (5.0, 12.0),
        EyeState.Relaxed => (2.0, 5.0),
        EyeState.Sleeping => (86400.0, 86400.0), // Never blink
        _ => (3.0, 6.0)
    };

    /// <summary>
    /// Returns the min/max look-around interval in seconds for this state.
    /// </summary>
    public static (double Min, double Max) GetLookInterval(this EyeState state) => state switch
    {
        EyeState.Idle => (2.0, 4.0),
        EyeState.Focused => (0.5, 2.0),
        EyeState.Relaxed => (4.0, 8.0),
        EyeState.Sleeping => (86400.0, 86400.0), // Never look
        _ => (2.0, 4.0)
    };
}
