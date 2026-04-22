namespace Gaze.Models;

/// <summary>
/// Represents the emotional state displayed by the eye mascot.
/// Each emotion can optionally show an emoji overlay on the pupil.
/// </summary>
public enum EyeEmotion
{
    Normal,
    Fire,
    Lightning,
    Code,
    Coffee,
    Burger,
    Music,
    Charging,
    LowBattery
}

public static class EyeEmotionExtensions
{
    /// <summary>
    /// Returns the emoji string for this emotion, or null for normal eyes.
    /// </summary>
    public static string? GetEmoji(this EyeEmotion emotion) => emotion switch
    {
        EyeEmotion.Normal => null,
        EyeEmotion.Fire => "🔥",
        EyeEmotion.Lightning => "⚡",
        EyeEmotion.Code => "💻",
        EyeEmotion.Coffee => "☕",
        EyeEmotion.Burger => "🍔",
        EyeEmotion.Music => "🎵",
        EyeEmotion.Charging => "⚡",
        EyeEmotion.LowBattery => "🪫",
        _ => null
    };

    private static readonly Random _rng = new();

    public static EyeEmotion RandomFocused()
    {
        var options = new[] { EyeEmotion.Normal, EyeEmotion.Normal, EyeEmotion.Fire, EyeEmotion.Lightning, EyeEmotion.Code };
        return options[_rng.Next(options.Length)];
    }

    public static EyeEmotion RandomRelaxed()
    {
        var options = new[] { EyeEmotion.Normal, EyeEmotion.Coffee, EyeEmotion.Burger, EyeEmotion.Music };
        return options[_rng.Next(options.Length)];
    }
}
