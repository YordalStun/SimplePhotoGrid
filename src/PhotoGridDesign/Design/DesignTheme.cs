using System.Windows;
using System.Windows.Media;

namespace PhotoGridDesign.Design;

/// <summary>A colour scheme for a design: background, text and photo framing.</summary>
public sealed class DesignTheme
{
    private DesignTheme(string name, string backgroundTop, string backgroundBottom,
                        string title, string body, string accent, string frame)
    {
        Name = name;
        BackgroundTop = Parse(backgroundTop);
        BackgroundBottom = Parse(backgroundBottom);
        TitleColor = Parse(title);
        BodyColor = Parse(body);
        AccentColor = Parse(accent);
        FrameColor = Parse(frame);
    }

    public string Name { get; }
    public Color BackgroundTop { get; }
    public Color BackgroundBottom { get; }
    public Color TitleColor { get; }
    public Color BodyColor { get; }
    public Color AccentColor { get; }
    public Color FrameColor { get; }

    public override string ToString() => Name;

    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public Brush CreateBackground(Rect area)
    {
        Brush brush = BackgroundTop == BackgroundBottom
            ? new SolidColorBrush(BackgroundTop)
            : new LinearGradientBrush(BackgroundTop, BackgroundBottom,
                                      new Point(0, 0), new Point(0.35, 1));
        brush.Freeze();
        return brush;
    }

    public Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static readonly DesignTheme Cream =
        new("Cream", "#FFFDF6EC", "#FFF3E2C7", "#FF3C2F21", "#FF6B5946", "#FFC98A3B", "#FFFFFFFF");

    public static readonly DesignTheme Charcoal =
        new("Charcoal", "#FF23262B", "#FF14161A", "#FFF5F5F5", "#FFB9BEC6", "#FFE8B23A", "#FF2F343B");

    public static readonly DesignTheme Sunset =
        new("Sunset", "#FFFF9A5A", "#FFE04E7B", "#FFFFFFFF", "#FFFFE9DC", "#FFFFD166", "#FFFFFFFF");

    public static readonly DesignTheme Ocean =
        new("Ocean", "#FF1F6FA5", "#FF0B2F4A", "#FFFFFFFF", "#FFCDE4F2", "#FF6FD3C7", "#FFFFFFFF");

    public static readonly DesignTheme Forest =
        new("Forest", "#FF3F6B4B", "#FF1E3326", "#FFF4F7F0", "#FFC9D8C4", "#FFE2B85F", "#FFF4F7F0");

    public static readonly DesignTheme Blush =
        new("Blush", "#FFFDECEF", "#FFF6CBD6", "#FF5A2B39", "#FF8A5566", "#FFD46A86", "#FFFFFFFF");

    public static readonly DesignTheme Mono =
        new("Mono", "#FFFFFFFF", "#FFFFFFFF", "#FF111111", "#FF555555", "#FF111111", "#FFFFFFFF");

    public static readonly DesignTheme Confetti =
        new("Confetti", "#FF6C5CE7", "#FF00B4D8", "#FFFFFFFF", "#FFE8E4FF", "#FFFFD166", "#FFFFFFFF");

    public static IReadOnlyList<DesignTheme> All { get; } =
        new[] { Cream, Charcoal, Sunset, Ocean, Forest, Blush, Mono, Confetti };
}
