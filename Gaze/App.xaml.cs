using System.Drawing;
using System.IO;

using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Gaze.Controllers;
using Gaze.Models;
using Gaze.Utilities;
using Gaze.Windows;

namespace Gaze;

/// <summary>
/// Application entry point. Sets up the system tray, overlay window,
/// hotkey manager, and welcome screen.
/// Port of AppDelegate.swift.
/// </summary>
public partial class App : Application
{
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private OverlayWindow? _overlayWindow;
    private SettingsWindow? _settingsWindow;
    private WelcomeWindow? _welcomeWindow;

    private readonly SettingsStore _settings = new();
    private TimerEngine _timerEngine = null!;
    private ShortcutManager _shortcutManager = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        // Force software rendering to fix transparency issues in emulators/VMs
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

        base.OnStartup(e);

        _timerEngine = new TimerEngine(_settings);

        // Setup system tray icon
        SetupTrayIcon();

        // Setup overlay window
        _overlayWindow = new OverlayWindow();
        _overlayWindow.Bind(_timerEngine, _settings);
        _overlayWindow.Show();

        // Register global hotkeys
        _overlayWindow.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(_overlayWindow).Handle;
            
            // Ctrl + Alt + P: Toggle Timer
            _shortcutManager.OnToggleTimer = () =>
            {
                if (_timerEngine.IsRunning) _timerEngine.Pause();
                else _timerEngine.Start();
            };

            // Shift + Alt + P: Show Island
            _shortcutManager.OnShowIsland = () =>
            {
                _overlayWindow.Dispatcher.Invoke(() => _overlayWindow.ShowIsland());
            };

            _shortcutManager.Register(hwnd);
        };

        // Welcome screen on first launch
        var launchManager = new LaunchManager();
        if (launchManager.IsFirstLaunch)
        {
            ShowWelcome(launchManager);
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "Gaze",
            Visible = true
        };

        // Create a simple eye icon programmatically
        _trayIcon.Icon = CreateTrayIcon();

        // Context menu
        var menu = new System.Windows.Forms.ContextMenuStrip();

        var settingsItem = new System.Windows.Forms.ToolStripMenuItem("Settings…");
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        var showIslandItem = new System.Windows.Forms.ToolStripMenuItem("Show Island (Test)");
        showIslandItem.Click += (_, _) => _overlayWindow?.Dispatcher.Invoke(() => _overlayWindow.ShowIsland());
        menu.Items.Add(showIslandItem);

        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

        var exitItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        _trayIcon.ContextMenuStrip = menu;
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                ShowSettings();
            }
        };
    }

    private static Icon CreateTrayIcon()
    {
        try
        {
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.png");
            if (!File.Exists(iconPath))
                iconPath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.FullName ?? "", "icon.png");

            if (File.Exists(iconPath))
            {
                using var bmp = new Bitmap(iconPath);
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
        catch { }

        var fallbackBmp = new Bitmap(16, 16);
        using (var g = Graphics.FromImage(fallbackBmp))
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);
            g.FillEllipse(System.Drawing.Brushes.White, 1, 3, 14, 10);
            g.FillEllipse(new SolidBrush(System.Drawing.Color.FromArgb(30, 30, 30)), 5, 5, 6, 6);
        }
        return Icon.FromHandle(fallbackBmp.GetHicon());
    }

    private void ShowSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow(_settings);
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        }
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    private void ShowWelcome(LaunchManager launchManager)
    {
        _welcomeWindow = new WelcomeWindow();
        _welcomeWindow.OnDismissed = () =>
        {
            launchManager.MarkLaunched();
            _welcomeWindow = null;
        };
        _welcomeWindow.Show();
    }

    private void ExitApp()
    {
        _shortcutManager.Dispose();
        _trayIcon?.Dispose();
        _overlayWindow?.Close();
        _settingsWindow?.Close();
        _welcomeWindow?.Close();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shortcutManager.Dispose();
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}
