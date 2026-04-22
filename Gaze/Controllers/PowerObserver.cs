using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace Gaze.Controllers;

/// <summary>
/// Monitors battery state on Windows.
/// Port of PowerObserver.swift.
/// </summary>
public class PowerObserver : INotifyPropertyChanged
{
    private readonly DispatcherTimer _timer;

    private bool _isCharging;
    private int _batteryPercentage = 100;
    private bool _isLowPower;

    public bool IsCharging
    {
        get => _isCharging;
        private set { _isCharging = value; OnPropertyChanged(); }
    }

    public int BatteryPercentage
    {
        get => _batteryPercentage;
        private set { _batteryPercentage = value; OnPropertyChanged(); }
    }

    public bool IsLowPower
    {
        get => _isLowPower;
        private set { _isLowPower = value; OnPropertyChanged(); }
    }

    public PowerObserver()
    {
        CheckBatteryState();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _timer.Tick += (_, _) => CheckBatteryState();
        _timer.Start();
    }

    private void CheckBatteryState()
    {
        try
        {
            var status = System.Windows.Forms.SystemInformation.PowerStatus;
            IsCharging = status.PowerLineStatus == System.Windows.Forms.PowerLineStatus.Online;
            BatteryPercentage = (int)(status.BatteryLifePercent * 100);
            IsLowPower = BatteryPercentage <= 20 && !IsCharging;
        }
        catch
        {
            // Desktop PCs may not report battery
            IsCharging = true;
            BatteryPercentage = 100;
            IsLowPower = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
