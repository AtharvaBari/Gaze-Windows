using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Gaze.Controllers;
using Gaze.Models;
using Gaze.Utilities;

namespace Gaze.Controls;

/// <summary>
/// The Dynamic Island control. Hidden by default.
/// 
/// Reveal animation sequence:
///   1. Tiny ball (12px) appears from above the top edge (opacity fade in)
///   2. Ball drops down to its resting position (translate Y animation)
///   3. Ball grows into a larger circle (~36px)
///   4. Circle stretches horizontally into the full island pill shape (340px × 36px)
///   5. Content (eyes + timer) fades in
///
/// Dismiss animation is the reverse: content fades → shrink to ball → rise up → fade out.
/// </summary>
public partial class IslandControl : UserControl
{
    private TimerEngine? _timerEngine;
    private SettingsStore? _settings;

    private bool _isVisible;
    private bool _isHovered;
    private bool _isAnimating;

    // Dismiss timer — hides the island after mouse leaves for a while
    private DispatcherTimer? _dismissTimer;
    private const int DismissDelayMs = 3000;

    public IslandControl()
    {
        InitializeComponent();
    }

    public void Bind(TimerEngine timerEngine, SettingsStore settings)
    {
        _timerEngine = timerEngine;
        _settings = settings;

        Companion.Bind(timerEngine, settings);
        TimerDisplay.Bind(timerEngine);

        _timerEngine.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TimerEngine.Mode) ||
                e.PropertyName == nameof(TimerEngine.IsRunning))
            {
                Dispatcher.Invoke(() =>
                {
                    UpdatePlayPauseIcon();
                    // Auto-show island when timer starts
                    if (_timerEngine.IsRunning && !_isVisible)
                        ShowIsland();
                });
            }
        };
    }

    /// <summary>
    /// Triggers the ball-drop → expand reveal animation.
    /// </summary>
    public void ShowIsland()
    {
        if (_isVisible || _isAnimating) return;
        _isAnimating = true;
        _isVisible = true;

        // Cancel any pending dismiss
        _dismissTimer?.Stop();

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var bounceEase = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 8 };

        // --- Phase 1: Tiny ball appears and drops down (0ms - 300ms) ---
        // Start as tiny ball, above the edge
        IslandPill.Width = 12;
        IslandPill.Height = 12;
        IslandPill.CornerRadius = new CornerRadius(6);
        IslandPill.Margin = new Thickness(0, -20, 0, 0);
        IslandPill.Opacity = 0;
        ContentGrid.Opacity = 0;

        // Fade in the ball
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)) { EasingFunction = ease };
        IslandPill.BeginAnimation(OpacityProperty, fadeIn);

        // Drop down (margin top: -20 → 8)
        var dropAnim = new ThicknessAnimation(
            new Thickness(0, -20, 0, 0),
            new Thickness(0, 8, 0, 0),
            TimeSpan.FromMilliseconds(300))
        { EasingFunction = bounceEase, BeginTime = TimeSpan.FromMilliseconds(50) };
        IslandPill.BeginAnimation(MarginProperty, dropAnim);

        // --- Phase 2: Ball grows into a circle (300ms - 550ms) ---
        var growWidth = new DoubleAnimation(12, 36, TimeSpan.FromMilliseconds(250))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(300) };
        var growHeight = new DoubleAnimation(12, 36, TimeSpan.FromMilliseconds(250))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(300) };

        IslandPill.BeginAnimation(WidthProperty, growWidth);
        IslandPill.BeginAnimation(HeightProperty, growHeight);

        // Update corner radius to stay circular
        DelayAction(300, () => IslandPill.CornerRadius = new CornerRadius(18));

        // --- Phase 3: Circle stretches into island pill (550ms - 900ms) ---
        var stretchWidth = new DoubleAnimation(36, ScreenHelper.IslandExpandedWidth, TimeSpan.FromMilliseconds(350))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }, BeginTime = TimeSpan.FromMilliseconds(550) };
        IslandPill.BeginAnimation(WidthProperty, stretchWidth);

        // Keep corner radius as perfect pill
        DelayAction(550, () => IslandPill.CornerRadius = new CornerRadius(18));

        // --- Phase 4: Content fades in (850ms - 1100ms) ---
        var contentFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(850) };
        contentFade.Completed += (_, _) =>
        {
            _isAnimating = false;
            StartDismissTimer();
        };
        ContentGrid.BeginAnimation(OpacityProperty, contentFade);
    }

    /// <summary>
    /// Triggers the reverse animation: shrink → ball → rise → disappear.
    /// </summary>
    private void HideIsland()
    {
        if (!_isVisible || _isAnimating) return;
        _isAnimating = true;

        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

        // --- Phase 1: Content fades out (0ms - 200ms) ---
        var contentFade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
        ContentGrid.BeginAnimation(OpacityProperty, contentFade);

        // --- Phase 2: Island shrinks to ball (200ms - 450ms) ---
        var shrinkWidth = new DoubleAnimation(ScreenHelper.IslandExpandedWidth, 12, TimeSpan.FromMilliseconds(250))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(200) };
        var shrinkHeight = new DoubleAnimation(36, 12, TimeSpan.FromMilliseconds(250))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(200) };
        IslandPill.BeginAnimation(WidthProperty, shrinkWidth);
        IslandPill.BeginAnimation(HeightProperty, shrinkHeight);

        DelayAction(300, () => IslandPill.CornerRadius = new CornerRadius(6));

        // --- Phase 3: Ball rises up and fades out (450ms - 650ms) ---
        var riseAnim = new ThicknessAnimation(
            new Thickness(0, 8, 0, 0),
            new Thickness(0, -20, 0, 0),
            TimeSpan.FromMilliseconds(200))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(450) };
        IslandPill.BeginAnimation(MarginProperty, riseAnim);

        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200))
        { EasingFunction = ease, BeginTime = TimeSpan.FromMilliseconds(450) };
        fadeOut.Completed += (_, _) =>
        {
            _isVisible = false;
            _isAnimating = false;
        };
        IslandPill.BeginAnimation(OpacityProperty, fadeOut);
    }

    // --- Mouse hover handling ---

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isHovered = true;
        _dismissTimer?.Stop();

        if (!_isVisible)
        {
            ShowIsland();
            return;
        }

        // Show hover controls
        Companion.Opacity = 0;
        TimerDisplay.Opacity = 0;
        PlayPauseBtn.Opacity = 1;
        StopBtn.Opacity = 1;
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _isHovered = false;

        // Restore normal content
        Companion.Opacity = 1;
        TimerDisplay.Opacity = 1;
        PlayPauseBtn.Opacity = 0;
        StopBtn.Opacity = 0;

        StartDismissTimer();
    }

    private void StartDismissTimer()
    {
        _dismissTimer?.Stop();

        // Don't auto-dismiss while timer is running
        if (_timerEngine != null && _timerEngine.IsRunning) return;

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DismissDelayMs) };
        _dismissTimer.Tick += (_, _) =>
        {
            _dismissTimer.Stop();
            if (!_isHovered && !(_timerEngine?.IsRunning ?? false))
            {
                HideIsland();
            }
        };
        _dismissTimer.Start();
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_timerEngine == null) return;
        if (_timerEngine.IsRunning) _timerEngine.Pause();
        else _timerEngine.Start();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _timerEngine?.Reset();
    }

    private void UpdatePlayPauseIcon()
    {
        if (_timerEngine == null) return;
        PlayPauseBtn.Content = _timerEngine.IsRunning ? "⏸" : "▶";
    }

    private static void DelayAction(int ms, Action action)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(ms) };
        timer.Tick += (_, _) => { timer.Stop(); action(); };
        timer.Start();
    }
}
