using System.Windows;
using System.Windows.Controls;
using Gaze.Models;

namespace Gaze.Controls;

/// <summary>
/// A single animated eye with pupil tracking, blinking, sleep state, and emoji overlay.
/// Port of EyeView.swift.
/// </summary>
public partial class EyeControl : UserControl
{
    public static readonly DependencyProperty BlinkScaleProperty =
        DependencyProperty.Register(nameof(BlinkScale), typeof(double), typeof(EyeControl),
            new PropertyMetadata(1.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty LookOffsetXProperty =
        DependencyProperty.Register(nameof(LookOffsetX), typeof(double), typeof(EyeControl),
            new PropertyMetadata(0.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty LookOffsetYProperty =
        DependencyProperty.Register(nameof(LookOffsetY), typeof(double), typeof(EyeControl),
            new PropertyMetadata(0.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsSleepingProperty =
        DependencyProperty.Register(nameof(IsSleeping), typeof(bool), typeof(EyeControl),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    public static readonly DependencyProperty EmotionProperty =
        DependencyProperty.Register(nameof(Emotion), typeof(EyeEmotion), typeof(EyeControl),
            new PropertyMetadata(EyeEmotion.Normal, OnVisualPropertyChanged));

    public double BlinkScale
    {
        get => (double)GetValue(BlinkScaleProperty);
        set => SetValue(BlinkScaleProperty, value);
    }

    public double LookOffsetX
    {
        get => (double)GetValue(LookOffsetXProperty);
        set => SetValue(LookOffsetXProperty, value);
    }

    public double LookOffsetY
    {
        get => (double)GetValue(LookOffsetYProperty);
        set => SetValue(LookOffsetYProperty, value);
    }

    public bool IsSleeping
    {
        get => (bool)GetValue(IsSleepingProperty);
        set => SetValue(IsSleepingProperty, value);
    }

    public EyeEmotion Emotion
    {
        get => (EyeEmotion)GetValue(EmotionProperty);
        set => SetValue(EmotionProperty, value);
    }

    public EyeControl()
    {
        InitializeComponent();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EyeControl eye)
            eye.UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Blink / sleep scale
        double scaleY = IsSleeping ? 0.05 : BlinkScale;
        EyeScaleTransform.ScaleY = scaleY;

        // Pupil offset
        PupilTranslate.X = LookOffsetX;
        PupilTranslate.Y = LookOffsetY;
        HighlightTranslate.X = LookOffsetX;
        HighlightTranslate.Y = LookOffsetY;

        // Emoji
        var emoji = Emotion.GetEmoji();
        if (emoji != null)
        {
            EmojiText.Text = emoji;
            EmojiText.Visibility = Visibility.Visible;
            EmojiTranslate.X = LookOffsetX;
            EmojiTranslate.Y = LookOffsetY;
            Pupil.Visibility = Visibility.Collapsed;
            Highlight.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmojiText.Visibility = Visibility.Collapsed;
            Pupil.Visibility = Visibility.Visible;
            Highlight.Visibility = Visibility.Visible;
        }

        // Sleep curve
        SleepCurve.Opacity = IsSleeping ? 1.0 : 0.0;
    }
}
