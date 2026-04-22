using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Gaze.Utilities;

namespace Gaze.Windows;

/// <summary>
/// Cinematic full-screen welcome overlay shown on first launch.
/// 
/// Animation sequence:
///   1. Bottom glow expands
///   2. Logo + welcome text fade in
///   3. Enter button appears
///   4. A tiny ball drops from above the top edge
///   5. Ball grows into a larger circle
///   6. Circle stretches into the Dynamic Island pill
///   7. Eyes and timer fade in inside the island
/// </summary>
public partial class WelcomeWindow : Window
{
    public Action? OnDismissed { get; set; }

    public WelcomeWindow()
    {
        InitializeComponent();
        Loaded += WelcomeWindow_Loaded;
    }

    private void WelcomeWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PositionGlow();
        PositionIsland();
        StartAnimationSequence();
    }

    private void PositionGlow()
    {
        double screenW = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;
        double screenH = ActualHeight > 0 ? ActualHeight : SystemParameters.PrimaryScreenHeight;

        Canvas.SetLeft(GlowEllipse, (screenW - 1200) / 2);
        Canvas.SetTop(GlowEllipse, screenH - 300);
    }

    private void PositionIsland()
    {
        double screenW = ActualWidth > 0 ? ActualWidth : SystemParameters.PrimaryScreenWidth;

        // Center the island horizontally
        Canvas.SetLeft(IslandShape, (screenW - 12) / 2);
        Canvas.SetTop(IslandShape, -20);
    }

    private void StartAnimationSequence()
    {
        double screenW = SystemParameters.PrimaryScreenWidth;
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        var bounceEase = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 8 };

        // 1. Glow appears (0s - 2.5s)
        AnimateDouble(GlowEllipse, OpacityProperty, 0.7, 2500, 0, ease);
        AnimateDouble(GlowScale, ScaleTransform.ScaleXProperty, 1.0, 2500, 0, ease);
        AnimateDouble(GlowScale, ScaleTransform.ScaleYProperty, 1.0, 2500, 0, ease);

        // Glow pulsing
        DelayAction(3000, () =>
        {
            var pulse = new DoubleAnimation(0.7, 1.0, TimeSpan.FromSeconds(3))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase()
            };
            GlowEllipse.BeginAnimation(OpacityProperty, pulse);
        });

        // 2. Logo + text appear (1.5s)
        AnimateDouble(LogoBorder, OpacityProperty, 1.0, 1500, 1500, ease);
        AnimateDouble(LogoScale, ScaleTransform.ScaleXProperty, 1.0, 1500, 1500, ease);
        AnimateDouble(LogoScale, ScaleTransform.ScaleYProperty, 1.0, 1500, 1500, ease);

        AnimateDouble(WelcomeText, OpacityProperty, 1.0, 1500, 1500, ease);
        AnimateDouble(TextScale, ScaleTransform.ScaleXProperty, 1.0, 1500, 1500, ease);
        AnimateDouble(TextScale, ScaleTransform.ScaleYProperty, 1.0, 1500, 1500, ease);

        // 3. Enter button appears (2.5s)
        AnimateDouble(EnterBorder, OpacityProperty, 1.0, 1000, 2500, ease);

        // --- 4. Dynamic Island: Ball drops in (4s) ---
        // Fade in the ball
        DelayAction(4000, () =>
        {
            IslandShape.Opacity = 1;

            // Drop from -20 to 8
            var drop = new DoubleAnimation(-20, 8, TimeSpan.FromMilliseconds(350))
            {
                EasingFunction = bounceEase
            };
            IslandShape.BeginAnimation(Canvas.TopProperty, drop);
        });

        // --- 5. Ball grows into a circle (4.4s) ---
        DelayAction(4400, () =>
        {
            double islandCenterX = screenW / 2;

            var growW = new DoubleAnimation(12, 36, TimeSpan.FromMilliseconds(250))
            { EasingFunction = ease };
            var growH = new DoubleAnimation(12, 36, TimeSpan.FromMilliseconds(250))
            { EasingFunction = ease };
            IslandShape.BeginAnimation(WidthProperty, growW);
            IslandShape.BeginAnimation(HeightProperty, growH);

            // Keep centered
            var centerX = new DoubleAnimation(Canvas.GetLeft(IslandShape), islandCenterX - 18, TimeSpan.FromMilliseconds(250))
            { EasingFunction = ease };
            IslandShape.BeginAnimation(Canvas.LeftProperty, centerX);

            IslandShape.CornerRadius = new CornerRadius(18);
        });

        // --- 6. Circle stretches into island pill (4.8s) ---
        DelayAction(4800, () =>
        {
            double islandCenterX = screenW / 2;
            double expandedWidth = ScreenHelper.IslandExpandedWidth;

            var stretchW = new DoubleAnimation(36, expandedWidth, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            IslandShape.BeginAnimation(WidthProperty, stretchW);

            // Recenter as it expands
            var recenterX = new DoubleAnimation(
                islandCenterX - 18,
                islandCenterX - expandedWidth / 2,
                TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            IslandShape.BeginAnimation(Canvas.LeftProperty, recenterX);

            // Subtle bounce
            DelayAction(350, () =>
            {
                var popW = new DoubleAnimation(expandedWidth, expandedWidth + 8, TimeSpan.FromMilliseconds(100));
                popW.Completed += (_, _) =>
                {
                    var retract = new DoubleAnimation(expandedWidth + 8, expandedWidth, TimeSpan.FromMilliseconds(150))
                    { EasingFunction = ease };
                    IslandShape.BeginAnimation(WidthProperty, retract);
                };
                IslandShape.BeginAnimation(WidthProperty, popW);
            });
        });

        // --- 7. Content fades in (5.4s) ---
        DelayAction(5400, () =>
        {
            var contentFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            { EasingFunction = ease };
            IslandContent.BeginAnimation(OpacityProperty, contentFade);
        });
    }

    private void Enter_Click(object sender, MouseButtonEventArgs e)
    {
        DismissWindow();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape)
        {
            DismissWindow();
        }
    }

    private void DismissWindow()
    {
        var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(800))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
        };
        fadeOut.Completed += (_, _) =>
        {
            OnDismissed?.Invoke();
            Close();
        };
        BeginAnimation(OpacityProperty, fadeOut);
    }

    private static void AnimateDouble(DependencyObject target, DependencyProperty property,
        double to, int durationMs, int delayMs, IEasingFunction? easing = null)
    {
        var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(durationMs))
        {
            BeginTime = TimeSpan.FromMilliseconds(delayMs),
            EasingFunction = easing
        };

        if (target is Animatable animatable)
            animatable.BeginAnimation(property, anim);
        else if (target is UIElement uiElement)
            uiElement.BeginAnimation(property, anim);
    }

    private static void DelayAction(int milliseconds, Action action)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); action(); };
        timer.Start();
    }
}
