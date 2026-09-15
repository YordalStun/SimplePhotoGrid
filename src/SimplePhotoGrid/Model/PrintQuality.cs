namespace SimplePhotoGrid.Model;

/// <summary>How much resolution to send to the printer. Photos are resampled to the size the
/// cell actually occupies on paper at this DPI, then JPEG compressed, so the spool file stays
/// proportional to the paper rather than to the source photos.</summary>
public sealed class PrintQuality
{
    private PrintQuality(string name, int dpi, int jpegQuality)
    {
        Name = name;
        Dpi = dpi;
        JpegQuality = jpegQuality;
    }

    public string Name { get; }

    /// <summary>Dots per inch the photos are resampled to.</summary>
    public int Dpi { get; }

    public int JpegQuality { get; }

    public override string ToString() => Name;

    public static readonly PrintQuality Normal = new("Normal (300 dpi)", 300, 85);
    public static readonly PrintQuality Draft = new("Draft (150 dpi)", 150, 78);
    public static readonly PrintQuality High = new("High (600 dpi)", 600, 92);

    public static IReadOnlyList<PrintQuality> All { get; } = new[] { Normal, Draft, High };

    /// <summary>Safety net so a single huge cell cannot resample to a gigapixel bitmap.</summary>
    public const int MaxPixelsPerSide = 4000;
}
