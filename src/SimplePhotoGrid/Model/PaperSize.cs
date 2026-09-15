using System.Printing;
using System.Windows;

namespace SimplePhotoGrid.Model;

/// <summary>A selectable paper size. Dimensions are stored portrait, in DIPs (1/96").</summary>
public sealed class PaperSize
{
    private PaperSize(string name, double widthMm, double heightMm, PageMediaSizeName? mediaName)
    {
        Name = name;
        Width = MmToDip(widthMm);
        Height = MmToDip(heightMm);
        MediaName = mediaName;
    }

    public string Name { get; }

    /// <summary>Portrait width in DIPs.</summary>
    public double Width { get; }

    /// <summary>Portrait height in DIPs.</summary>
    public double Height { get; }

    /// <summary>Standard media name, so the print ticket can ask the driver for real A4 rather
    /// than a custom size the driver may round oddly.</summary>
    public PageMediaSizeName? MediaName { get; }

    public Size SizeFor(bool landscape) =>
        landscape ? new Size(Height, Width) : new Size(Width, Height);

    public override string ToString() => Name;

    private static double MmToDip(double mm) => mm / 25.4 * 96.0;

    public static readonly PaperSize A3 = new("A3 (297 x 420 mm)", 297, 420, PageMediaSizeName.ISOA3);
    public static readonly PaperSize A4 = new("A4 (210 x 297 mm)", 210, 297, PageMediaSizeName.ISOA4);
    public static readonly PaperSize A5 = new("A5 (148 x 210 mm)", 148, 210, PageMediaSizeName.ISOA5);
    public static readonly PaperSize Letter = new("Letter (8.5 x 11 in)", 215.9, 279.4, PageMediaSizeName.NorthAmericaLetter);
    public static readonly PaperSize Legal = new("Legal (8.5 x 14 in)", 215.9, 355.6, PageMediaSizeName.NorthAmericaLegal);
    public static readonly PaperSize Photo6x4 = new("Photo 6 x 4 in", 101.6, 152.4, PageMediaSizeName.NorthAmerica4x6);
    public static readonly PaperSize Photo7x5 = new("Photo 7 x 5 in", 127.0, 177.8, PageMediaSizeName.NorthAmerica5x7);

    public static IReadOnlyList<PaperSize> All { get; } =
        new[] { A4, A3, A5, Letter, Legal, Photo6x4, Photo7x5 };
}
