using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Gaze.Controllers;
using Gaze.Models;

namespace Gaze.Controls;

/// <summary>
/// The eye mascot companion that displays two animated eyes with
/// Z-particles during sleep and magnifying glass peek.
/// Port of CompanionView.swift.
/// </summary>
public partial class CompanionControl : UserControl
{
    private EyeController _controller = new();
    private TimerEngine? _timerEngine;
    private SettingsStore? _settings;

    private DispatcherTimer? _zAnimTimer;
    private double _zPhase;

    public CompanionControl()
    {
        InitializeComponent();
        _controller.PropertyChanged += Controller_PropertyChanged;
        StartZAnimation();
    }

    public void Bind(TimerEngine timerEngine, SettingsStore settings)
    {
        // Unsubscribe from old
        if (_timerEngine != null)
            _timerEngine.PropertyChanged -= TimerEngine_PropertyChanged;
        if (_settings != null)
            _settings.PropertyChanged -= Settings_PropertyChanged;

        _timerEngine = timerEngine;
        _settings = settings;

        _timerEngine.PropertyChanged += TimerEngine_PropertyChanged;
        _settings.PropertyChanged += Settings_PropertyChanged;

        // Initial state
        _controller.IsCursorTrackingEnabled = settings.TrackCursor;
        if (timerEngine.Mode == TimerMode.Idle)
        {
            _controller.State = EyeState.Sleeping;
        }
    }

    private void TimerEngine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerEngine.Mode))
        {
            _controller.State = _timerEngine!.Mode switch
            {
                TimerMode.Countdown => EyeState.Idle,
                TimerMode.Idle or TimerMode.Completed => EyeState.Sleeping,
                TimerMode.Work => EyeState.Focused,
                TimerMode.Break => EyeState.Relaxed,
                _ => EyeState.Idle
            };
        }
    }

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsStore.TrackCursor))
        {
            _controller.IsCursorTrackingEnabled = _settings!.TrackCursor;
        }
    }

    private void Controller_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(EyeController.LookOffsetX):
                case nameof(EyeController.LookOffsetY):
                    LeftEye.LookOffsetX = _controller.LookOffsetX;
                    LeftEye.LookOffsetY = _controller.LookOffsetY;
                    RightEye.LookOffsetX = _controller.LookOffsetX;
                    RightEye.LookOffsetY = _controller.LookOffsetY;
                    break;

                case nameof(EyeController.LeftBlinkScale):
                    LeftEye.BlinkScale = _controller.LeftBlinkScale;
                    LeftEye.IsSleeping = _controller.State == EyeState.Sleeping && _controller.LeftBlinkScale < 0.5;
                    break;

                case nameof(EyeController.RightBlinkScale):
                    RightEye.BlinkScale = _controller.RightBlinkScale;
                    RightEye.IsSleeping = _controller.State == EyeState.Sleeping && _controller.RightBlinkScale < 0.5;
                    break;

                case nameof(EyeController.CurrentEmotion):
                    LeftEye.Emotion = _controller.CurrentEmotion;
                    RightEye.Emotion = _controller.CurrentEmotion;
                    break;

                case nameof(EyeController.State):
                    ZCanvas.Visibility = _controller.State == EyeState.Sleeping
                        ? Visibility.Visible : Visibility.Collapsed;
                    break;

                case nameof(EyeController.ShowsMagnifyingGlass):
                case nameof(EyeController.IsLeftEyePeeking):
                    UpdateMagnifier();
                    break;
            }
        });
    }

    private void UpdateMagnifier()
    {
        if (_controller.ShowsMagnifyingGlass && _controller.State == EyeState.Sleeping)
        {
            if (_controller.IsLeftEyePeeking)
            {
                LeftMagnifier.Visibility = Visibility.Visible;
                RightMagnifier.Visibility = Visibility.Collapsed;
            }
            else
            {
                LeftMagnifier.Visibility = Visibility.Collapsed;
                RightMagnifier.Visibility = Visibility.Visible;
            }
        }
        else
        {
            LeftMagnifier.Visibility = Visibility.Collapsed;
            RightMagnifier.Visibility = Visibility.Collapsed;
        }
    }

    private void StartZAnimation()
    {
        _zAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _zAnimTimer.Tick += (_, _) =>
        {
            _zPhase += 0.05;
            if (_zPhase > 4.0) _zPhase = 0;

            double smallX = Math.Sin(_zPhase) * 5 + 2;
            double smallY = -_zPhase * 4 + 4;
            double smallOpacity = Math.Max(0, 1.0 - _zPhase / 4.0);
            double smallScale = 0.5 + _zPhase / 4.0;

            Canvas.SetLeft(SmallZ, 30 + smallX);
            Canvas.SetTop(SmallZ, smallY);
            SmallZ.Opacity = smallOpacity;
            SmallZ.RenderTransform = new ScaleTransform(smallScale, smallScale);

            double bigX = Math.Sin(_zPhase + Math.PI) * 6 - 2;
            double bigY = -_zPhase * 3 - 2;
            double bigOpacity = Math.Max(0, 1.0 - Math.Max(0, _zPhase - 1.0) / 3.0);
            double bigScale = 0.5 + _zPhase / 5.0;

            Canvas.SetLeft(BigZ, 22 + bigX);
            Canvas.SetTop(BigZ, bigY);
            BigZ.Opacity = bigOpacity;
            BigZ.RenderTransform = new ScaleTransform(bigScale, bigScale);
        };
        _zAnimTimer.Start();
    }
}
