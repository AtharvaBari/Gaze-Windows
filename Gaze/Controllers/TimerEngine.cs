using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Gaze.Models;

namespace Gaze.Controllers;

/// <summary>
/// Pomodoro timer engine. Manages the countdown → work → break → completed cycle.
/// Direct port of TimerEngine.swift.
/// </summary>
public class TimerEngine : INotifyPropertyChanged
{
    private readonly SettingsStore _settings;
    private DispatcherTimer? _timer;

    private TimerMode _mode = TimerMode.Idle;
    private int _timeRemaining;
    private int _countdownValue = 3;
    private bool _isRunning;
    private int _currentCycle;
    private bool _isPeeking;

    public TimerMode Mode
    {
        get => _mode;
        private set { _mode = value; OnPropertyChanged(); }
    }

    public int TimeRemaining
    {
        get => _timeRemaining;
        private set { _timeRemaining = value; OnPropertyChanged(); }
    }

    public int CountdownValue
    {
        get => _countdownValue;
        private set { _countdownValue = value; OnPropertyChanged(); }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set { _isRunning = value; OnPropertyChanged(); }
    }

    public int CurrentCycle
    {
        get => _currentCycle;
        private set { _currentCycle = value; OnPropertyChanged(); }
    }

    public bool IsPeeking
    {
        get => _isPeeking;
        set { _isPeeking = value; OnPropertyChanged(); }
    }

    public TimerEngine(SettingsStore settings)
    {
        _settings = settings;
    }

    public void Start()
    {
        if (Mode == TimerMode.Idle || Mode == TimerMode.Completed)
        {
            CurrentCycle = 1;
            StartCountdown();
        }
        else if (Mode == TimerMode.Work || Mode == TimerMode.Break)
        {
            ResumeTimer();
        }
    }

    public void Pause()
    {
        IsRunning = false;
        _timer?.Stop();
    }

    public void Reset()
    {
        Pause();
        Mode = TimerMode.Idle;
        TimeRemaining = 0;
        CurrentCycle = 1;
        IsPeeking = false;
    }

    private void StartCountdown()
    {
        Mode = TimerMode.Countdown;
        CountdownValue = 3;
        IsRunning = true;

        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            if (CountdownValue > 1)
            {
                CountdownValue--;
                _settings.PlayTickSound();
            }
            else
            {
                _timer?.Stop();
                _settings.PlayCompletionSound();
                StartWorkRound();
            }
        };
        _timer.Start();
        _settings.PlayTickSound();
    }

    private void StartWorkRound()
    {
        Mode = TimerMode.Work;
        TimeRemaining = _settings.WorkDurationSeconds;
        IsPeeking = true;

        // Hide peek after 5 seconds
        var peekTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        peekTimer.Tick += (_, _) => { IsPeeking = false; peekTimer.Stop(); };
        peekTimer.Start();

        ResumeTimer();
    }

    private void StartBreakRound()
    {
        Mode = TimerMode.Break;
        TimeRemaining = _settings.BreakDurationSeconds;
        IsPeeking = true;

        var peekTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        peekTimer.Tick += (_, _) => { IsPeeking = false; peekTimer.Stop(); };
        peekTimer.Start();

        ResumeTimer();
    }

    private void ResumeTimer()
    {
        IsRunning = true;
        _timer?.Stop();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => TickTimer();
        _timer.Start();
    }

    private void TickTimer()
    {
        if (TimeRemaining > 0)
        {
            TimeRemaining--;

            // Periodic peek check during work
            if (_settings.IsPeriodicPeekEnabled && Mode == TimerMode.Work)
            {
                int elapsed = _settings.WorkDurationSeconds - TimeRemaining;
                if (elapsed > 0 && elapsed % (_settings.PeekIntervalMinutes * 60) == 0)
                {
                    IsPeeking = true;
                    var peekTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                    peekTimer.Tick += (_, _) => { IsPeeking = false; peekTimer.Stop(); };
                    peekTimer.Start();
                }
            }
        }
        else
        {
            _settings.PlayCompletionSound();

            if (Mode == TimerMode.Work)
            {
                if (CurrentCycle >= _settings.MaxCycles)
                {
                    FinishPomodoro();
                }
                else
                {
                    StartBreakRound();
                }
            }
            else if (Mode == TimerMode.Break)
            {
                CurrentCycle++;
                StartWorkRound();
            }
        }
    }

    private void FinishPomodoro()
    {
        _timer?.Stop();
        IsRunning = false;
        Mode = TimerMode.Completed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
