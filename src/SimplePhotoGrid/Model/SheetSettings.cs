using System.Windows;
using System.Windows.Media;

namespace SimplePhotoGrid.Model;

/// <summary>Everything the renderer needs besides the photos themselves.</summary>
public sealed class SheetSettings
{
    public PaperSize Paper { get; set; } = PaperSize.A4;
    public bool Landscape { get; set; }
    public GridPreset Grid { get; set; } = GridPreset.Auto;

    /// <summary>Page margin in millimetres.</summary>
    public double MarginMm { get; set; } = 10;

    /// <summary>Gap between cells in millimetres.</summary>
    public double GapMm { get; set; } = 4;

    public string Title { get; set; } = string.Empty;
    public bool TitleOnEveryPage { get; set; } = true;
    public double TitleFontSize { get; set; } = 20;

    public bool ShowCaptions { get; set; } = true;
    public bool CaptionsIncludeExtension { get; set; }
    public double CaptionFontSize { get; set; } = 8;

    public bool ShowBorders { get; set; } = true;
    public bool ShowPageNumbers { get; set; } = true;

    public FontFamily FontFamily { get; set; } = new("Segoe UI");

    public Size PageSize => Paper.SizeFor(Landscape);

    public double MarginDip => MarginMm / 25.4 * 96.0;
    public double GapDip => GapMm / 25.4 * 96.0;
}
