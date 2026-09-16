using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimplePhotoGrid.Model;

namespace PhotoGridDesign.Design;

/// <summary>Shared drawing for the templates: framed photo cards, cropped images and text.</summary>
public static class DesignDraw
{
    /// <summary>Draws one photo as a card: soft shadow, frame, rounded corners, optional caption
    /// strip below the image, optionally tilted about its own centre.</summary>
    public static void Card(DrawingContext dc, Rect bounds, PhotoItem? photo,
                            DesignSettings settings, double tiltDegrees = 0,
                            double captionStripHeight = 0)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2) return;

        var tilted = Math.Abs(tiltDegrees) > 0.01;
        if (tilted)
        {
            dc.PushTransform(new RotateTransform(
                tiltDegrees, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2));
        }

        var radius = Math.Min(settings.CornerRadius, Math.Min(bounds.Width, bounds.Height) / 2);

        if (settings.Shadow) Shadow(dc, bounds, radius);

        var frame = settings.FrameWidth;
        if (frame > 0 || captionStripHeight > 0)
        {
            var card = new SolidColorBrush(settings.Theme.FrameColor);
            card.Freeze();
            dc.DrawRoundedRectangle(card, null, bounds, radius, radius);
        }

        var inner = new Rect(
            bounds.X + frame,
            bounds.Y + frame,
            Math.Max(bounds.Width - frame * 2, 1),
            Math.Max(bounds.Height - frame * 2 - captionStripHeight, 1));

        var image = photo?.PrintImage;
        if (image is null)
        {
            var missing = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xE2));
            missing.Freeze();
            dc.DrawRectangle(missing, null, inner);
        }
        else
        {
            var innerRadius = Math.Max(radius - frame / 2, 0);
            dc.PushClip(new RectangleGeometry(inner, innerRadius, innerRadius));
            Fill(dc, inner, image);
            dc.Pop();
        }

        if (captionStripHeight > 0 && photo is not null)
        {
            var strip = new Rect(inner.X, inner.Bottom, inner.Width, captionStripHeight);
            var text = Text(photo.FileNameNoExtension,
                            Math.Min(captionStripHeight * 0.42, 13),
                            settings.BodyFont, FontWeights.Normal,
                            settings.Theme.CreateBrush(Color.FromRgb(0x44, 0x44, 0x44)),
                            strip.Width, TextAlignment.Center);
            text.MaxLineCount = 1;
            text.Trimming = TextTrimming.CharacterEllipsis;
            dc.DrawText(text, new Point(strip.X, strip.Y + (strip.Height - text.Height) / 2));
        }

        if (tilted) dc.Pop();
    }

    /// <summary>Stacked translucent rounded rectangles. A DropShadowEffect is a render-time
    /// bitmap effect and does not survive the print path, so the shadow is drawn as geometry.</summary>
    private static void Shadow(DrawingContext dc, Rect bounds, double radius)
    {
        for (var step = 4; step >= 1; step--)
        {
            var brush = new SolidColorBrush(Color.FromArgb((byte)(7 * step), 0, 0, 0));
            brush.Freeze();
            var offset = step * 1.6;
            var spread = Rect.Inflate(bounds, offset * 0.5, offset * 0.5);
            dc.DrawRoundedRectangle(brush, null,
                new Rect(spread.X, spread.Y + offset * 0.6, spread.Width, spread.Height),
                radius + offset * 0.5, radius + offset * 0.5);
        }
    }

    /// <summary>Scales the image to cover the area completely, centred, overflow clipped by the
    /// caller.</summary>
    public static void Fill(DrawingContext dc, Rect area, BitmapSource image)
    {
        var scale = Math.Max(area.Width / image.PixelWidth, area.Height / image.PixelHeight);
        var width = image.PixelWidth * scale;
        var height = image.PixelHeight * scale;

        dc.DrawImage(image, new Rect(
            area.X + (area.Width - width) / 2,
            area.Y + (area.Height - height) / 2,
            width, height));
    }

    public static FormattedText Text(string content, double size, FontFamily family,
                                     FontWeight weight, Brush brush, double maxWidth,
                                     TextAlignment alignment)
    {
        return new FormattedText(
            content,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(family, FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(size, 1),
            brush,
            1.0)
        {
            MaxTextWidth = Math.Max(maxWidth, 1),
            TextAlignment = alignment
        };
    }

    /// <summary>Deterministic jitter so a design looks hand-placed but redraws identically.</summary>
    public static double Jitter(int seed, int index, int salt, double amount)
    {
        unchecked
        {
            var hash = seed * 73856093 ^ index * 19349663 ^ salt * 83492791;
            hash = Math.Abs(hash % 2001);
            return (hash / 1000.0 - 1.0) * amount;
        }
    }
}
