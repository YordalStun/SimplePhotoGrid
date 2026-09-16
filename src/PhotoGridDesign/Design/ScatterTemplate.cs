using System.Windows;
using System.Windows.Media;

namespace PhotoGridDesign.Design;

/// <summary>Polaroid-style cards tilted and nudged off a loose grid so they overlap like photos
/// dropped on a table.</summary>
public sealed class ScatterTemplate : DesignTemplate
{
    public override string Name => "Polaroid scatter";
    public override string Description => "Tilted polaroid frames, overlapping. Use Shuffle to re-roll.";

    public override void Draw(DrawingContext dc, DesignCanvas canvas)
    {
        var photos = canvas.Photos;
        if (photos.Count == 0) return;

        var settings = canvas.Settings;
        var (columns, rows) = BalancedGrid(photos.Count, canvas.Area);

        var cellWidth = canvas.Area.Width / columns;
        var cellHeight = canvas.Area.Height / rows;

        // Oversize the cards so they overlap; the tilt then reads as a pile rather than a grid.
        var cardWidth = cellWidth * 1.18;
        var cardHeight = cellHeight * 1.18;

        // Polaroids have a deep bottom border whether or not a caption is printed in it.
        var captionStrip = Math.Min(cardHeight * 0.16, 30);

        var scatter = new DesignSettings
        {
            Template = settings.Template,
            Theme = settings.Theme,
            Paper = settings.Paper,
            Landscape = settings.Landscape,
            ShowCaptions = settings.ShowCaptions,
            FrameWidth = Math.Max(settings.FrameWidth, 6),
            CornerRadius = Math.Min(settings.CornerRadius, 3),
            Shadow = settings.Shadow,
            Spacing = settings.Spacing,
            BodyFont = settings.BodyFont,
            HeadingFont = settings.HeadingFont
        };

        for (var i = 0; i < photos.Count; i++)
        {
            var column = i % columns;
            var row = i / columns;

            var centreX = canvas.Area.X + (column + 0.5) * cellWidth;
            var centreY = canvas.Area.Y + (row + 0.5) * cellHeight;

            centreX += DesignDraw.Jitter(settings.Seed, i, 1, cellWidth * 0.10);
            centreY += DesignDraw.Jitter(settings.Seed, i, 2, cellHeight * 0.10);

            var tilt = DesignDraw.Jitter(settings.Seed, i, 3, settings.Tilt);

            var bounds = new Rect(
                centreX - cardWidth / 2,
                centreY - cardHeight / 2,
                cardWidth, cardHeight);

            DesignDraw.Card(dc, bounds, photos[i], scatter, tilt, captionStrip);
        }
    }
}
