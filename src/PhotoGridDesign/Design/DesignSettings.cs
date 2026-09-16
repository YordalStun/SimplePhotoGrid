using System.Windows;
using System.Windows.Media;
using SimplePhotoGrid.Model;

namespace PhotoGridDesign.Design;

public sealed class DesignSettings
{
    public DesignTemplate Template { get; set; } = DesignTemplate.All[0];
    public DesignTheme Theme { get; set; } = DesignTheme.Cream;

    public PaperSize Paper { get; set; } = PaperSize.A4;
    public bool Landscape { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public double TitleFontSize { get; set; } = 42;

    public FontFamily HeadingFont { get; set; } = new("Georgia");
    public FontFamily BodyFont { get; set; } = new("Segoe UI");

    public bool ShowCaptions { get; set; }

    /// <summary>White border around each photo, in DIPs. Zero for none.</summary>
    public double FrameWidth { get; set; } = 8;

    public double CornerRadius { get; set; } = 6;
    public bool Shadow { get; set; } = true;

    /// <summary>Gap between photos, in DIPs.</summary>
    public double Spacing { get; set; } = 14;

    /// <summary>Maximum tilt in degrees for templates that scatter photos.</summary>
    public double Tilt { get; set; } = 6;

    /// <summary>Re-rolls the pseudo-random placement of scattered templates.</summary>
    public int Seed { get; set; } = 1;

    public double MarginDip => 28;

    public Size PageSize => Paper.SizeFor(Landscape);

    public bool HasHeading =>
        !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Subtitle);
}
