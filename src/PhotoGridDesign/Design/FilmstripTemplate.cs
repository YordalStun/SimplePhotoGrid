using System.Windows;
using System.Windows.Media;

namespace PhotoGridDesign.Design;

/// <summary>Photos laid into strips of film, sprocket holes and all.</summary>
public sealed class FilmstripTemplate : DesignTemplate
{
    public override string Name => "Filmstrip";
    public override string Description => "Photos set into strips of film with sprocket holes.";

    private static readonly Color FilmColor = Color.FromRgb(0x1A, 0x1A, 0x1D);

    public override void Draw(DrawingContext dc, DesignCanvas canvas)
    {
        var photos = canvas.Photos;
        if (photos.Count == 0) return;

        var settings = canvas.Settings;

        // Four frames per strip reads as film without shrinking the photos too far.
        var perStrip = photos.Count <= 3 ? Math.Max(photos.Count, 1)
                     : photos.Count <= 8 ? 3
                     : photos.Count <= 18 ? 4 : 5;

        var strips = (int)Math.Ceiling(photos.Count / (double)perStrip);
        var stripGap = settings.Spacing * 1.2;
        var stripHeight = (canvas.Area.Height - stripGap * (strips - 1)) / strips;
        if (stripHeight <= 12) return;

        var film = new SolidColorBrush(FilmColor);
        film.Freeze();
        var hole = new SolidColorBrush(Color.FromRgb(0xEC, 0xEC, 0xEC));
        hole.Freeze();

        var frameSettings = new DesignSettings
        {
            Template = settings.Template,
            Theme = settings.Theme,
            Paper = settings.Paper,
            Landscape = settings.Landscape,
            ShowCaptions = false,
            FrameWidth = 0,
            CornerRadius = 1,
            Shadow = false,
            BodyFont = settings.BodyFont,
            HeadingFont = settings.HeadingFont
        };

        for (var strip = 0; strip < strips; strip++)
        {
            var slice = photos.Skip(strip * perStrip).Take(perStrip).ToList();
            if (slice.Count == 0) break;

            var bounds = new Rect(
                canvas.Area.X,
                canvas.Area.Y + strip * (stripHeight + stripGap),
                canvas.Area.Width, stripHeight);

            dc.DrawRoundedRectangle(film, null, bounds, 4, 4);

            var sprocket = stripHeight * 0.10;
            DrawSprockets(dc, hole, bounds, sprocket);

            var inset = sprocket * 1.9;
            var frameArea = new Rect(
                bounds.X + inset * 0.6,
                bounds.Y + inset,
                Math.Max(bounds.Width - inset * 1.2, 1),
                Math.Max(bounds.Height - inset * 2, 1));

            var gap = Math.Max(settings.Spacing * 0.4, 3);
            var frameWidth = (frameArea.Width - gap * (slice.Count - 1)) / slice.Count;

            for (var i = 0; i < slice.Count; i++)
            {
                var cell = new Rect(
                    frameArea.X + i * (frameWidth + gap), frameArea.Y,
                    frameWidth, frameArea.Height);
                DesignDraw.Card(dc, cell, slice[i], frameSettings);
            }
        }
    }

    private static void DrawSprockets(DrawingContext dc, Brush brush, Rect strip, double size)
    {
        if (size < 1.5) return;

        var pitch = size * 2.1;
        var count = Math.Max((int)(strip.Width / pitch), 1);
        var margin = (strip.Width - (count * pitch - (pitch - size))) / 2;

        for (var i = 0; i < count; i++)
        {
            var x = strip.X + margin + i * pitch;
            var radius = size * 0.3;
            dc.DrawRoundedRectangle(brush, null,
                new Rect(x, strip.Y + size * 0.5, size, size * 0.72), radius, radius);
            dc.DrawRoundedRectangle(brush, null,
                new Rect(x, strip.Bottom - size * 1.22, size, size * 0.72), radius, radius);
        }
    }
}
