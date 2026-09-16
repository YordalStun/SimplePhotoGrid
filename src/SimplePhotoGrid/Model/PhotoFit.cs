namespace SimplePhotoGrid.Model;

/// <summary>How a photo is placed inside its cell when their shapes disagree.</summary>
public sealed class PhotoFit
{
    private PhotoFit(string name, bool rotates, bool crops)
    {
        Name = name;
        Rotates = rotates;
        Crops = crops;
    }

    public string Name { get; }

    /// <summary>Turn the photo 90 degrees when that lets it run along the cell's long edge.</summary>
    public bool Rotates { get; }

    /// <summary>Scale until the cell is filled edge to edge and clip the overflow.</summary>
    public bool Crops { get; }

    public override string ToString() => Name;

    public static readonly PhotoFit Fit = new("Fit whole photo", false, false);
    public static readonly PhotoFit Rotate = new("Rotate to fit", true, false);
    public static readonly PhotoFit Crop = new("Crop to fill", false, true);
    public static readonly PhotoFit RotateAndCrop = new("Rotate and crop", true, true);

    public static IReadOnlyList<PhotoFit> All { get; } = new[] { Fit, Rotate, Crop, RotateAndCrop };

    /// <summary>True when the photo and the cell disagree about which way up they are, so turning
    /// the photo would let it run along the cell's long edge.</summary>
    public static bool ShouldRotate(double photoWidth, double photoHeight,
                                    double cellWidth, double cellHeight)
    {
        if (photoWidth <= 0 || photoHeight <= 0) return false;
        return photoWidth >= photoHeight != cellWidth >= cellHeight;
    }

    /// <summary>Scale factor from source pixels to cell, for this fit mode.</summary>
    public double ScaleFor(double sourceWidth, double sourceHeight,
                           double cellWidth, double cellHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0) return 1;

        var rotate = Rotates && ShouldRotate(sourceWidth, sourceHeight, cellWidth, cellHeight);
        var width = rotate ? sourceHeight : sourceWidth;
        var height = rotate ? sourceWidth : sourceHeight;

        return Crops
            ? Math.Max(cellWidth / width, cellHeight / height)
            : Math.Min(cellWidth / width, cellHeight / height);
    }
}
