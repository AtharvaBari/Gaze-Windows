using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using Gaze.Models;

namespace Gaze.Controllers;

/// <summary>
/// Controls the eye mascot's behavior: blinking, looking around, cursor tracking,
/// sleep mode with peeking, and emotional states.
/// Direct port of EyeController.swift.
/// </summary>
public class EyeController : INotifyPropertyChanged
{
    private static readonly Random _rng = new();
    private const double MaxLookDistance = 4.0;

    private DispatcherTimer? _blinkTimer;
    private DispatcherTimer? _lookTimer;
    private DispatcherTimer? _trackingTimer;
    private DispatcherTimer? _sleepPeekTimer;
    private DispatcherTimer? _emotionClearTimer;

    private EyeState _state = EyeState.Idle;
    private bool _isCursorTrackingEnabled;
    private double _lookOffsetX;
    private double _lookOffsetY;
    private double _leftBlinkScale = 1.0;
    private double _rightBlinkScale = 1.0;
    private bool _showsMagnifyingGlass;
    private bool _isLeftEyePeeking;
    private EyeEmotion _currentEmotion = EyeEmotion.Normal;

    // Target values for smooth animation
    private double _targetLookX;
    private double _targetLookY;
    private double _targetLeftBlink = 1.0;
    private double _targetRightBlink = 1.0;

    private readonly DispatcherTimer _animationTimer;

    public EyeState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                OnPropertyChanged();
                RestartTimers();
            }
        }
    }

    public bool IsCursorTrackingEnabled
    {
        get => _isCursorTrackingEnabled;
        set
        {
            if (_isCursorTrackingEnabled != value)
            {
                _isCursorTrackingEnabled = value;
                OnPropertyChanged();
                RestartTimers();
            }
        }
    }

    public double LookOffsetX
    {
        get => _lookOffsetX;
        private set { _lookOffsetX = value; OnPropertyChanged(); }
    }

    public double LookOffsetY
    {
        get => _lookOffsetY;
        private set { _lookOffsetY = value; OnPropertyChanged(); }
    }

    public double LeftBlinkScale
    {
        get => _leftBlinkScale;
        private set { _leftBlinkScale = value; OnPropertyChanged(); }
    }

    public double RightBlinkScale
    {
        get => _rightBlinkScale;
        private set { _rightBlinkScale = value; OnPropertyChanged(); }
    }

    public bool ShowsMagnifyingGlass
    {
        get => _showsMagnifyingGlass;
        private set { _showsMagnifyingGlass = value; OnPropertyChanged(); }
    }

    public bool IsLeftEyePeeking
    {
        get => _isLeftEyePeeking;
        private set { _isLeftEyePeeking = value; OnPropertyChanged(); }
    }

    public EyeEmotion CurrentEmotion
    {
        get => _currentEmotion;
        private set { _currentEmotion = value; OnPropertyChanged(); }
    }

    public EyeController()
    {
        // Smooth animation interpolation timer (60fps)
        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _animationTimer.Tick += AnimationTick;
        _animationTimer.Start();

        StartTimers();
    }

    private void AnimationTick(object? sender, EventArgs e)
    {
        // Smooth lerp toward targets
        const double lerpSpeed = 0.15;
        const double blinkLerpSpeed = 0.3;

        LookOffsetX += (_targetLookX - LookOffsetX) * lerpSpeed;
        LookOffsetY += (_targetLookY - LookOffsetY) * lerpSpeed;
        LeftBlinkScale += (_targetLeftBlink - LeftBlinkScale) * blinkLerpSpeed;
        RightBlinkScale += (_targetRightBlink - RightBlinkScale) * blinkLerpSpeed;
    }

    private void RestartTimers()
    {
        StopTimers();
        SetTempEmotion(EyeEmotion.Normal);

        if (_state == EyeState.Sleeping)
        {
            _targetLeftBlink = 0.1;
            _targetRightBlink = 0.1;
            _targetLookX = 0;
            _targetLookY = 0;
        }
        else
        {
            _targetLeftBlink = 1.0;
            _targetRightBlink = 1.0;
        }

        StartTimers();
    }

    private void StopTimers()
    {
        _blinkTimer?.Stop();
        _lookTimer?.Stop();
        _trackingTimer?.Stop();
        _sleepPeekTimer?.Stop();
    }

    private void StartTimers()
    {
        if (_state == EyeState.Sleeping)
        {
            ScheduleSleepPeek();
            return;
        }

        ScheduleNextBlink();
        if (_isCursorTrackingEnabled)
        {
            StartTrackingCursor();
        }
        else
        {
            ScheduleNextLook();
        }
    }

    private void StartTrackingCursor()
    {
        _trackingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _trackingTimer.Tick += (_, _) => UpdateLookTargetForCursor();
        _trackingTimer.Start();
    }

    private void UpdateLookTargetForCursor()
    {
        if (_state == EyeState.Sleeping) return;

        var cursorPos = System.Windows.Forms.Cursor.Position;
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        if (screen == null) return;

        double eyeCenterX = screen.Bounds.Width / 2.0;
        double eyeCenterY = 16; // Near top of screen

        double dx = cursorPos.X - eyeCenterX;
        double dy = cursorPos.Y - eyeCenterY;

        const double maxDistance = 800.0;

        double mappedX = (dx / maxDistance) * MaxLookDistance;
        double mappedY = (dy / maxDistance) * MaxLookDistance;

        double clampedX = Math.Clamp(mappedX, -MaxLookDistance, MaxLookDistance);
        double clampedY = Math.Clamp(mappedY, -MaxLookDistance / 2, MaxLookDistance / 2);

        _targetLookX = clampedX;
        _targetLookY = clampedY;
    }

    private void ScheduleNextBlink()
    {
        var interval = _state.GetBlinkInterval();
        double delay = _rng.NextDouble() * (interval.Max - interval.Min) + interval.Min;

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delay) };
        _blinkTimer.Tick += (_, _) => { _blinkTimer.Stop(); PerformBlink(); };
        _blinkTimer.Start();
    }

    private void PerformBlink()
    {
        if (_state == EyeState.Sleeping) return;

        _targetLeftBlink = 0.1;
        _targetRightBlink = 0.1;

        var reopenTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        reopenTimer.Tick += (_, _) =>
        {
            reopenTimer.Stop();
            if (_state == EyeState.Sleeping) return;
            _targetLeftBlink = 1.0;
            _targetRightBlink = 1.0;
            ScheduleNextBlink();
        };
        reopenTimer.Start();
    }

    private void ScheduleSleepPeek()
    {
        _sleepPeekTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _sleepPeekTimer.Tick += (_, _) => { _sleepPeekTimer.Stop(); PerformSleepPeek(); };
        _sleepPeekTimer.Start();
    }

    private void PerformSleepPeek()
    {
        if (_state != EyeState.Sleeping) return;

        bool peekLeft = _rng.Next(2) == 0;
        bool hasMagnifier = _rng.Next(3) == 0;

        // Crack open one eye
        if (peekLeft) _targetLeftBlink = 1.0;
        else _targetRightBlink = 1.0;

        ShowsMagnifyingGlass = hasMagnifier;
        IsLeftEyePeeking = peekLeft;
        _targetLookX = _rng.NextDouble() * 4 - 2;
        _targetLookY = _rng.NextDouble() * 2 - 1;

        // Look left
        DelayAction(600, () =>
        {
            if (_state != EyeState.Sleeping) return;
            _targetLookX = -2.5;
            _targetLookY = 0;
        });

        // Look right
        DelayAction(1200, () =>
        {
            if (_state != EyeState.Sleeping) return;
            _targetLookX = 2.5;
            _targetLookY = 0;
        });

        // Look up
        DelayAction(1800, () =>
        {
            if (_state != EyeState.Sleeping) return;
            _targetLookX = 0;
            _targetLookY = -1.5;
        });

        // Return to sleep
        DelayAction(2500, () =>
        {
            if (_state != EyeState.Sleeping) return;
            _targetLeftBlink = 0.1;
            _targetRightBlink = 0.1;
            _targetLookX = 0;
            _targetLookY = 0;
            ShowsMagnifyingGlass = false;
            ScheduleSleepPeek();
        });
    }

    private void ScheduleNextLook()
    {
        var interval = _state.GetLookInterval();
        double delay = _rng.NextDouble() * (interval.Max - interval.Min) + interval.Min;

        _lookTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(delay) };
        _lookTimer.Tick += (_, _) => { _lookTimer.Stop(); PerformLook(); };
        _lookTimer.Start();
    }

    private void PerformLook()
    {
        if (_state == EyeState.Sleeping) return;

        if (_state == EyeState.Focused)
        {
            // High frequency erratic shaking
            _targetLookX = _rng.NextDouble() * 4 - 2;
            _targetLookY = _rng.NextDouble() * 3 - 1.5;

            // 20% chance to squint
            if (_rng.Next(5) == 0)
            {
                _targetLeftBlink = 0.5;
                _targetRightBlink = 0.5;
                DelayAction(300, () =>
                {
                    if (_state != EyeState.Focused) return;
                    _targetLeftBlink = 1.0;
                    _targetRightBlink = 1.0;
                });
            }
        }
        else
        {
            // Random look around
            bool isCentered = _rng.Next(3) == 0;
            if (isCentered)
            {
                _targetLookX = 0;
                _targetLookY = 0;
            }
            else
            {
                _targetLookX = _rng.NextDouble() * MaxLookDistance * 2 - MaxLookDistance;
                _targetLookY = _rng.NextDouble() * MaxLookDistance - MaxLookDistance / 2;
            }
        }

        ScheduleNextLook();
    }

    private void SetTempEmotion(EyeEmotion emotion)
    {
        CurrentEmotion = emotion;
        _emotionClearTimer?.Stop();

        if (emotion == EyeEmotion.Normal) return;

        _emotionClearTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _emotionClearTimer.Tick += (_, _) =>
        {
            _emotionClearTimer.Stop();
            if (CurrentEmotion == emotion)
                RestartTimers();
        };
        _emotionClearTimer.Start();
    }

    private static void DelayAction(int milliseconds, Action action)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); action(); };
        timer.Start();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
