using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Gaze.Controllers;
using Gaze.Models;

namespace Gaze.Windows;

/// <summary>
/// Premium settings window with sidebar navigation, dark theme, and GitHub updater.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsStore _store;
    private readonly UpdateManager _updater = new();
    private string _currentTab = "General";

    public SettingsWindow(SettingsStore store)
    {
        _store = store;
        InitializeComponent();
        LoadSettings();

        VersionLabel.Text = $"v{_updater.CurrentVersion}";
        CurrentVersionText.Text = $"v{_updater.CurrentVersion}";

        _updater.StateChanged += () => Dispatcher.Invoke(UpdateUI);
    }

    private void LoadSettings()
    {
        LaunchToggle.IsChecked = _store.LaunchAtLogin;
        SoundToggle.IsChecked = _store.EnableSounds;
        HideToggle.IsChecked = _store.HideOnInactivity;
        CursorToggle.IsChecked = _store.TrackCursor;
        PeekToggle.IsChecked = _store.IsPeriodicPeekEnabled;

        UpdateStepperDisplays();
        UpdatePeekIntervalVisibility();
    }

    private void UpdateStepperDisplays()
    {
        WorkValue.Text = $"{_store.WorkDurationMinutes} min";
        BreakValue.Text = $"{_store.BreakDurationMinutes} min";
        CyclesValue.Text = $"{_store.MaxCycles}×";
        PeekValue.Text = $"{_store.PeekIntervalMinutes} min";
    }

    private void UpdatePeekIntervalVisibility()
    {
        PeekIntervalRow.Visibility = _store.IsPeriodicPeekEnabled ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Tab Navigation ──────────────────────────────────────────

    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tab)
        {
            _currentTab = tab;

            // Update all tab button foregrounds
            var tabs = new[] { GeneralTab, TimerTab, InteractionTab, UpdatesTab };
            foreach (var t in tabs)
            {
                t.Foreground = new SolidColorBrush(
                    t.Tag as string == tab 
                        ? Colors.White 
                        : Color.FromRgb(136, 136, 136));
            }

            // Show/hide panels
            GeneralPanel.Visibility = tab == "General" ? Visibility.Visible : Visibility.Collapsed;
            TimerPanel.Visibility = tab == "Timer" ? Visibility.Visible : Visibility.Collapsed;
            InteractionPanel.Visibility = tab == "Interaction" ? Visibility.Visible : Visibility.Collapsed;
            UpdatesPanel.Visibility = tab == "Updates" ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    // ── Toggle Changes ──────────────────────────────────────────

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggle)
        {
            bool isChecked = toggle.IsChecked == true;

            if (toggle == LaunchToggle) _store.LaunchAtLogin = isChecked;
            else if (toggle == SoundToggle) _store.EnableSounds = isChecked;
            else if (toggle == HideToggle) _store.HideOnInactivity = isChecked;
            else if (toggle == CursorToggle) _store.TrackCursor = isChecked;
            else if (toggle == PeekToggle)
            {
                _store.IsPeriodicPeekEnabled = isChecked;
                UpdatePeekIntervalVisibility();
            }
        }
    }

    // ── Stepper Clicks ──────────────────────────────────────────

    private void WorkMinus_Click(object sender, RoutedEventArgs e)
    {
        _store.WorkDurationMinutes = Math.Max(1, _store.WorkDurationMinutes - 1);
        UpdateStepperDisplays();
    }
    private void WorkPlus_Click(object sender, RoutedEventArgs e)
    {
        _store.WorkDurationMinutes = Math.Min(60, _store.WorkDurationMinutes + 1);
        UpdateStepperDisplays();
    }
    private void BreakMinus_Click(object sender, RoutedEventArgs e)
    {
        _store.BreakDurationMinutes = Math.Max(1, _store.BreakDurationMinutes - 1);
        UpdateStepperDisplays();
    }
    private void BreakPlus_Click(object sender, RoutedEventArgs e)
    {
        _store.BreakDurationMinutes = Math.Min(30, _store.BreakDurationMinutes + 1);
        UpdateStepperDisplays();
    }
    private void CyclesMinus_Click(object sender, RoutedEventArgs e)
    {
        _store.MaxCycles = Math.Max(1, _store.MaxCycles - 1);
        UpdateStepperDisplays();
    }
    private void CyclesPlus_Click(object sender, RoutedEventArgs e)
    {
        _store.MaxCycles = Math.Min(10, _store.MaxCycles + 1);
        UpdateStepperDisplays();
    }
    private void PeekMinus_Click(object sender, RoutedEventArgs e)
    {
        _store.PeekIntervalMinutes = Math.Max(1, _store.PeekIntervalMinutes - 1);
        UpdateStepperDisplays();
    }
    private void PeekPlus_Click(object sender, RoutedEventArgs e)
    {
        _store.PeekIntervalMinutes = Math.Min(60, _store.PeekIntervalMinutes + 1);
        UpdateStepperDisplays();
    }

    // ── Updates ─────────────────────────────────────────────────

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
    {
        await _updater.CheckForUpdatesAsync();
    }

    private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
    {
        await _updater.DownloadAndInstallAsync();
    }

    private void UpdateUI()
    {
        UpdateStatusCard.Visibility = Visibility.Visible;

        // Reset all sub-panels
        CheckingStatus.Visibility = Visibility.Collapsed;
        UpToDateStatus.Visibility = Visibility.Collapsed;
        UpdateAvailableStatus.Visibility = Visibility.Collapsed;
        ErrorStatus.Visibility = Visibility.Collapsed;

        if (_updater.IsChecking)
        {
            CheckingStatus.Visibility = Visibility.Visible;
            CheckUpdateBtn.IsEnabled = false;
        }
        else if (_updater.ErrorMessage != null)
        {
            ErrorStatus.Visibility = Visibility.Visible;
            ErrorText.Text = _updater.ErrorMessage;
            CheckUpdateBtn.IsEnabled = true;
        }
        else if (_updater.UpdateAvailable)
        {
            UpdateAvailableStatus.Visibility = Visibility.Visible;
            NewVersionText.Text = $"v{_updater.LatestVersion}";
            ReleaseNotesText.Text = _updater.ReleaseNotes ?? "No release notes.";
            CheckUpdateBtn.IsEnabled = true;

            if (_updater.IsDownloading)
            {
                InstallUpdateBtn.IsEnabled = false;
                InstallUpdateBtn.Content = "Downloading…";
                DownloadProgress.Visibility = Visibility.Visible;

                double pct = _updater.DownloadProgress;
                ProgressFill.Width = 140 * pct;
                ProgressText.Text = $"{(int)(pct * 100)}%";
            }
            else
            {
                InstallUpdateBtn.IsEnabled = true;
                InstallUpdateBtn.Content = "Download & Install";
                DownloadProgress.Visibility = Visibility.Collapsed;
            }
        }
        else if (_updater.LatestVersion != null)
        {
            UpToDateStatus.Visibility = Visibility.Visible;
            CheckUpdateBtn.IsEnabled = true;
        }
        else
        {
            UpdateStatusCard.Visibility = Visibility.Collapsed;
            CheckUpdateBtn.IsEnabled = true;
        }
    }
}
