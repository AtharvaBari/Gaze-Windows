using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Gaze.Controllers;

/// <summary>
/// Manages global keyboard shortcuts using Win32 RegisterHotKey.
/// Port of ShortcutManager.swift — maps Cmd+Option+P to Ctrl+Alt+P on Windows.
/// </summary>
public class ShortcutManager : IDisposable
{
    private const int HOTKEY_ID_TOGGLE_TIMER = 9001;
    private const int HOTKEY_ID_SHOW_ISLAND = 9002;
    private const int MOD_ALT = 0x0001;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    private const int VK_P = 0x50;
    private const int WM_HOTKEY = 0x0312;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private IntPtr _windowHandle;
    private HwndSource? _source;

    public Action? OnToggleTimer { get; set; }
    public Action? OnShowIsland { get; set; }

    public void Register(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _source = HwndSource.FromHwnd(windowHandle);
        _source?.AddHook(HwndHook);

        // Ctrl + Alt + P: Toggle Timer
        RegisterHotKey(_windowHandle, HOTKEY_ID_TOGGLE_TIMER, MOD_CONTROL | MOD_ALT, VK_P);
        
        // Shift + Ctrl + P: Show Island
        RegisterHotKey(_windowHandle, HOTKEY_ID_SHOW_ISLAND, MOD_SHIFT | MOD_CONTROL, VK_P);
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == HOTKEY_ID_TOGGLE_TIMER)
            {
                OnToggleTimer?.Invoke();
                handled = true;
            }
            else if (id == HOTKEY_ID_SHOW_ISLAND)
            {
                OnShowIsland?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _source?.RemoveHook(HwndHook);
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID_TOGGLE_TIMER);
            UnregisterHotKey(_windowHandle, HOTKEY_ID_SHOW_ISLAND);
        }
    }
}
