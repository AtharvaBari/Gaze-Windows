using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Gaze.Models;

/// <summary>
/// Persistent settings store for the Gaze app.
/// Settings are saved to a JSON file in the user's AppData directory.
/// </summary>
public class SettingsStore : INotifyPropertyChanged
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gaze");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    private int _workDurationMinutes = 25;
    private int _breakDurationMinutes = 5;
    private int _maxCycles = 4;
    private bool _isPeriodicPeekEnabled = false;
    private int _peekIntervalMinutes = 5;
    private bool _trackCursor = false;
    private bool _autoCheckUpdates = true;
    private bool _enableSounds = true;
    private bool _hideOnInactivity = false;
    private bool _launchAtLogin = false;

    public int WorkDurationMinutes
    {
        get => _workDurationMinutes;
        set { _workDurationMinutes = value; OnPropertyChanged(); Save(); }
    }

    public int BreakDurationMinutes
    {
        get => _breakDurationMinutes;
        set { _breakDurationMinutes = value; OnPropertyChanged(); Save(); }
    }

    public int MaxCycles
    {
        get => _maxCycles;
        set { _maxCycles = value; OnPropertyChanged(); Save(); }
    }

    public bool IsPeriodicPeekEnabled
    {
        get => _isPeriodicPeekEnabled;
        set { _isPeriodicPeekEnabled = value; OnPropertyChanged(); Save(); }
    }

    public int PeekIntervalMinutes
    {
        get => _peekIntervalMinutes;
        set { _peekIntervalMinutes = value; OnPropertyChanged(); Save(); }
    }

    public bool TrackCursor
    {
        get => _trackCursor;
        set { _trackCursor = value; OnPropertyChanged(); Save(); }
    }

    public bool AutoCheckUpdates
    {
        get => _autoCheckUpdates;
        set { _autoCheckUpdates = value; OnPropertyChanged(); Save(); }
    }

    public bool EnableSounds
    {
        get => _enableSounds;
        set { _enableSounds = value; OnPropertyChanged(); Save(); }
    }

    public bool HideOnInactivity
    {
        get => _hideOnInactivity;
        set { _hideOnInactivity = value; OnPropertyChanged(); Save(); }
    }

    public bool LaunchAtLogin
    {
        get => _launchAtLogin;
        set { _launchAtLogin = value; OnPropertyChanged(); Save(); UpdateStartupRegistry(); }
    }

    // Computed
    public int WorkDurationSeconds => WorkDurationMinutes * 60;
    public int BreakDurationSeconds => BreakDurationMinutes * 60;

    public SettingsStore()
    {
        Load();
    }

    public void PlaySound(string soundName)
    {
        if (!EnableSounds) return;
        try
        {
            System.Media.SystemSounds.Asterisk.Play();
        }
        catch { /* Silently ignore sound errors */ }
    }

    public void PlayTickSound()
    {
        if (!EnableSounds) return;
        try
        {
            System.Media.SystemSounds.Beep.Play();
        }
        catch { }
    }

    public void PlayCompletionSound()
    {
        if (!EnableSounds) return;
        try
        {
            System.Media.SystemSounds.Exclamation.Play();
        }
        catch { }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var data = new SettingsData
            {
                WorkDurationMinutes = _workDurationMinutes,
                BreakDurationMinutes = _breakDurationMinutes,
                MaxCycles = _maxCycles,
                IsPeriodicPeekEnabled = _isPeriodicPeekEnabled,
                PeekIntervalMinutes = _peekIntervalMinutes,
                TrackCursor = _trackCursor,
                AutoCheckUpdates = _autoCheckUpdates,
                EnableSounds = _enableSounds,
                HideOnInactivity = _hideOnInactivity,
                LaunchAtLogin = _launchAtLogin
            };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* Silently ignore save errors */ }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return;
            var json = File.ReadAllText(SettingsPath);
            var data = JsonSerializer.Deserialize<SettingsData>(json);
            if (data == null) return;

            _workDurationMinutes = data.WorkDurationMinutes;
            _breakDurationMinutes = data.BreakDurationMinutes;
            _maxCycles = data.MaxCycles;
            _isPeriodicPeekEnabled = data.IsPeriodicPeekEnabled;
            _peekIntervalMinutes = data.PeekIntervalMinutes;
            _trackCursor = data.TrackCursor;
            _autoCheckUpdates = data.AutoCheckUpdates;
            _enableSounds = data.EnableSounds;
            _hideOnInactivity = data.HideOnInactivity;
            _launchAtLogin = data.LaunchAtLogin;
        }
        catch { /* Use defaults on load error */ }
    }

    private void UpdateStartupRegistry()
    {
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (_launchAtLogin)
            {
                var exePath = Environment.ProcessPath ?? "";
                key.SetValue("Gaze", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("Gaze", false);
            }
            key.Close();
        }
        catch { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private class SettingsData
    {
        public int WorkDurationMinutes { get; set; } = 25;
        public int BreakDurationMinutes { get; set; } = 5;
        public int MaxCycles { get; set; } = 4;
        public bool IsPeriodicPeekEnabled { get; set; }
        public int PeekIntervalMinutes { get; set; } = 5;
        public bool TrackCursor { get; set; }
        public bool AutoCheckUpdates { get; set; } = true;
        public bool EnableSounds { get; set; } = true;
        public bool HideOnInactivity { get; set; }
        public bool LaunchAtLogin { get; set; }
    }
}
