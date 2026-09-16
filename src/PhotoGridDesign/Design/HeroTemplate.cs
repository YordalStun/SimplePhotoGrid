using System.Windows;
using System.Windows.Media;

namespace PhotoGridDesign.Design;

/// <summary>One photo given the top of the page, the rest in a band beneath it.</summary>
public sealed class HeroTemplate : DesignTemplate
{
    public override string Name => "Hero + band";
    public override string Description => "First photo large, the others in rows underneath.";

    public override void Draw(DrawingContext dc, DesignCanvas canvas)
    {
        var photos = canvas.Photos;
        if (photos.Count == 0) return;

        var settings = canvas.Settings;
        var gap = settings.Spacing;

        if (photos.Count == 1)
        {
            DesignDraw.Card(dc, canvas.Area, photos[0], settings);
            return;
        }

        var rest = photos.Skip(1).ToList();

        // More supporting photos means more rows, so the hero gives up some height.
        var bandRows = rest.Count <= 4 ? 1 : rest.Count <= 12 ? 2 : 3;
        var heroShare = bandRows switch { 1 => 0.62, 2 => 0.50, _ => 0.42 };

        var heroHeight = (canvas.Area.Height - gap) * heroShare;
        var hero = new Rect(canvas.Area.X, canvas.Area.Y, canvas.Area.Width, heroHeight);
        DesignDraw.Card(dc, hero, photos[0], settings);

        var bandTop = hero.Bottom + gap;
        var bandHeight = canvas.Area.Bottom - bandTop;
        if (bandHeight <= 4) return;

        var perRow = (int)Math.Ceiling(rest.Count / (double)bandRows);
        var cellHeight = (bandHeight - gap * (bandRows - 1)) / bandRows;

        for (var row = 0; row < bandRows; row++)
        {
            var slice = rest.Skip(row * perRow).Take(perRow).ToList();
            if (slice.Count == 0) break;

            var cellWidth = (canvas.Area.Width - gap * (slice.Count - 1)) / slice.Count;
            var y = bandTop + row * (cellHeight + gap);

            for (var i = 0; i < slice.Count; i++)
            {
                var bounds = new Rect(
                    canvas.Area.X + i * (cellWidth + gap), y, cellWidth, cellHeight);
                DesignDraw.Card(dc, bounds, slice[i], settings);
            }
        }
    }
}
