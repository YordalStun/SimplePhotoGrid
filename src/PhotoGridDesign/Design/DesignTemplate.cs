using System.Windows;
using System.Windows.Media;
using SimplePhotoGrid.Model;

namespace PhotoGridDesign.Design;

/// <summary>Everything a template needs to lay out the photo area of a design.</summary>
public sealed record DesignCanvas(Rect Area, IReadOnlyList<PhotoItem> Photos, DesignSettings Settings);

public abstract class DesignTemplate
{
    public abstract string Name { get; }

    /// <summary>A one-line description shown under the picker.</summary>
    public abstract string Description { get; }

    public abstract void Draw(DrawingContext dc, DesignCanvas canvas);

    public override string ToString() => Name;

    protected static double Aspect(PhotoItem photo)
    {
        var image = photo.PrintImage;
        if (image is null || image.PixelHeight <= 0) return 1.0;
        return Math.Clamp((double)image.PixelWidth / image.PixelHeight, 0.3, 3.5);
    }

    /// <summary>Columns that make the cells sit closest to square for this many photos.</summary>
    protected static (int Columns, int Rows) BalancedGrid(int count, Rect area)
    {
        if (count <= 1) return (1, 1);

        var best = (Columns: 1, Rows: count);
        var bestScore = double.MaxValue;

        for (var columns = 1; columns <= count; columns++)
        {
            var rows = (int)Math.Ceiling(count / (double)columns);
            var cellWidth = area.Width / columns;
            var cellHeight = area.Height / rows;

            // Prefer near-square cells, and penalise a mostly empty last row.
            var squareness = Math.Abs(Math.Log(cellWidth / cellHeight));
            var waste = (columns * rows - count) * 0.12;
            var score = squareness + waste;

            if (score < bestScore)
            {
                bestScore = score;
                best = (columns, rows);
            }
        }

        return best;
    }

    public static IReadOnlyList<DesignTemplate> All { get; } = new DesignTemplate[]
    {
        new MosaicTemplate(),
        new GridTemplate(),
        new ScatterTemplate(),
        new HeroTemplate(),
        new FilmstripTemplate()
    };
}
