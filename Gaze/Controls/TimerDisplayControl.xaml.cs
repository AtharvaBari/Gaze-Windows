using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using Gaze.Controllers;
using Gaze.Models;

namespace Gaze.Controls;

/// <summary>
/// Displays the current timer state as text (Ready, countdown, MM:SS, Done!).
/// Port of TimerDisplayView.swift.
/// </summary>
public partial class TimerDisplayControl : UserControl
{
    private TimerEngine? _engine;

    public TimerDisplayControl()
    {
        InitializeComponent();
    }

    public void Bind(TimerEngine engine)
    {
        if (_engine != null)
            _engine.PropertyChanged -= Engine_PropertyChanged;

        _engine = engine;
        _engine.PropertyChanged += Engine_PropertyChanged;
        UpdateDisplay();
    }

    private void Engine_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(UpdateDisplay);
    }

    private void UpdateDisplay()
    {
        if (_engine == null) return;

        TimerText.Text = _engine.Mode switch
        {
            TimerMode.Idle => "Ready",
            TimerMode.Countdown => _engine.CountdownValue.ToString(),
            TimerMode.Work or TimerMode.Break =>
                $"{_engine.TimeRemaining / 60:D2}:{_engine.TimeRemaining % 60:D2}",
            TimerMode.Completed => "Done!",
            _ => ""
        };

        var color = _engine.Mode.GetTextColor();
        TimerText.Foreground = new SolidColorBrush(color);
    }
}
