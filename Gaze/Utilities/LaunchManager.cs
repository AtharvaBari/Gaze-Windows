using System.IO;

namespace Gaze.Utilities;

/// <summary>
/// Manages first-launch state for the app.
/// Port of LaunchManager.swift.
/// </summary>
public class LaunchManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gaze");
    private static readonly string FlagPath = Path.Combine(SettingsDir, ".launched");

    public bool IsFirstLaunch { get; private set; }

    public LaunchManager()
    {
        IsFirstLaunch = !File.Exists(FlagPath);
    }

    /// <summary>
    /// Call this when the welcome animation has completed.
    /// </summary>
    public void MarkLaunched()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            File.WriteAllText(FlagPath, "1");
            IsFirstLaunch = false;
        }
        catch { }
    }
}
