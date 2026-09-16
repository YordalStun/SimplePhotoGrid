using System.Windows;
using System.Windows.Media;

namespace PhotoGridDesign.Design;

/// <summary>Even grid of framed cards.</summary>
public sealed class GridTemplate : DesignTemplate
{
    public override string Name => "Neat grid";
    public override string Description => "Even rows and columns, every photo the same size.";

    public override void Draw(DrawingContext dc, DesignCanvas canvas)
    {
        var photos = canvas.Photos;
        if (photos.Count == 0) return;

        var (columns, rows) = BalancedGrid(photos.Count, canvas.Area);
        var gap = canvas.Settings.Spacing;

        var cellWidth = (canvas.Area.Width - gap * (columns - 1)) / columns;
        var cellHeight = (canvas.Area.Height - gap * (rows - 1)) / rows;
        if (cellWidth <= 2 || cellHeight <= 2) return;

        var caption = canvas.Settings.ShowCaptions ? Math.Min(cellHeight * 0.14, 22) : 0;

        for (var i = 0; i < photos.Count; i++)
        {
            var column = i % columns;
            var row = i / columns;

            // Centre a short final row rather than leaving it hanging to the left.
            var inRow = Math.Min(columns, photos.Count - row * columns);
            var rowWidth = inRow * cellWidth + (inRow - 1) * gap;
            var offset = (canvas.Area.Width - rowWidth) / 2;

            var bounds = new Rect(
                canvas.Area.X + offset + column * (cellWidth + gap),
                canvas.Area.Y + row * (cellHeight + gap),
                cellWidth, cellHeight);

            DesignDraw.Card(dc, bounds, photos[i], canvas.Settings, 0, caption);
        }
    }
}
