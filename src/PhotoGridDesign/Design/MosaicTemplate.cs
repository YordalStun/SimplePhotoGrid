using System.Windows;
using System.Windows.Media;
using SimplePhotoGrid.Model;

namespace PhotoGridDesign.Design;

/// <summary>Justified rows: each row is filled edge to edge and its height falls out of the
/// photos' shapes, so wide photos get wide tiles and tall ones get tall tiles.</summary>
public sealed class MosaicTemplate : DesignTemplate
{
    public override string Name => "Mosaic";
    public override string Description => "Rows sized to each photo's shape, filling the page edge to edge.";

    public override void Draw(DrawingContext dc, DesignCanvas canvas)
    {
        var photos = canvas.Photos;
        if (photos.Count == 0) return;

        var gap = canvas.Settings.Spacing;
        var rows = BuildRows(photos, canvas.Area, gap);
        if (rows.Count == 0) return;

        var totalHeight = rows.Sum(row => row.Height) + gap * (rows.Count - 1);
        // Rows are built to a target height, so the stack rarely lands exactly on the area.
        var scale = totalHeight > 0 ? Math.Min(canvas.Area.Height / totalHeight, 1.35) : 1;

        var y = canvas.Area.Y + (canvas.Area.Height - totalHeight * scale) / 2;

        foreach (var row in rows)
        {
            var height = row.Height * scale;
            var x = canvas.Area.X;

            foreach (var photo in row.Photos)
            {
                var width = Aspect(photo) * height;
                DesignDraw.Card(dc, new Rect(x, y, width, height), photo, canvas.Settings);
                x += width + gap;
            }

            y += height + gap;
        }
    }

    private sealed record Row(List<PhotoItem> Photos, double Height);

    private static List<Row> BuildRows(IReadOnlyList<PhotoItem> photos, Rect area, double gap)
    {
        // Aim for roughly square-ish rows, then let each row's height be whatever makes its
        // photos span the full width.
        var targetRows = Math.Max(1, (int)Math.Round(Math.Sqrt(photos.Count * area.Height / Math.Max(area.Width, 1)) * 1.15));
        var targetHeight = area.Height / targetRows;

        var rows = new List<Row>();
        var current = new List<PhotoItem>();
        var aspectSum = 0.0;

        foreach (var photo in photos)
        {
            current.Add(photo);
            aspectSum += Aspect(photo);

            var available = area.Width - gap * (current.Count - 1);
            var height = available / aspectSum;

            if (height <= targetHeight && current.Count > 0)
            {
                rows.Add(new Row(current, height));
                current = new List<PhotoItem>();
                aspectSum = 0;
            }
        }

        if (current.Count > 0)
        {
            var available = area.Width - gap * (current.Count - 1);
            // A short last row would stretch absurdly tall; cap it at the target.
            rows.Add(new Row(current, Math.Min(available / aspectSum, targetHeight)));
        }

        return rows;
    }
}
