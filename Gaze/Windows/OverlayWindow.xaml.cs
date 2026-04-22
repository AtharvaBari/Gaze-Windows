using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Gaze.Controllers;
using Gaze.Models;
using Gaze.Utilities;

namespace Gaze.Windows;

/// <summary>
/// Borderless, transparent, topmost overlay window.
/// Contains an invisible detection zone at the top-center — when the mouse touches it,
/// the Dynamic Island appears with its ball-drop animation.
/// </summary>
public partial class OverlayWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WS_EX_TRANSPARENT = 0x00000020;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private IntPtr _hwnd;

    public OverlayWindow()
    {
        InitializeComponent();
        PositionOverlay();

        SourceInitialized += (_, _) =>
        {
            _hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
        };

        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (_, _) =>
            Dispatcher.Invoke(PositionOverlay);
    }

    private void PositionOverlay()
    {
        var (x, y, width, height) = ScreenHelper.GetOverlayRect();
        Left = x;
        Top = y;
        Width = width;
        Height = height;
    }

    private void DetectionZone_MouseEnter(object sender, MouseEventArgs e)
    {
        Island.ShowIsland();
    }

    public void ShowIsland()
    {
        Island.ShowIsland();
    }

    public void Bind(TimerEngine timerEngine, SettingsStore settings)
    {
        Island.Bind(timerEngine, settings);
    }
}
