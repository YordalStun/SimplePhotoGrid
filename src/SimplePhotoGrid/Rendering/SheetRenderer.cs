using System.Globalization;
using System.Windows;
using System.Windows.Media;
using SimplePhotoGrid.Model;

namespace SimplePhotoGrid.Rendering;

/// <summary>Draws a contact sheet page. The same renderer feeds both the on-screen preview and
/// the printer, so what you see is what comes out.</summary>
public sealed class SheetRenderer
{
    private static readonly Brush CaptionBrush = Brushes.Black;
    private static readonly Brush FooterBrush = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));
    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(0xBB, 0xBB, 0xBB)), 0.75);
    private static readonly Brush PlaceholderFill = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0));

    static SheetRenderer()
    {
        FooterBrush.Freeze();
        BorderPen.Freeze();
        PlaceholderFill.Freeze();
    }

    private readonly IReadOnlyList<PhotoItem> _photos;
    private readonly SheetSettings _settings;

    public SheetRenderer(IReadOnlyList<PhotoItem> photos, SheetSettings settings)
    {
        _photos = photos;
        _settings = settings;
        PerPage = ResolvePerPage(out var columns, out var rows);
        Columns = columns;
        Rows = rows;
        PageCount = _photos.Count == 0 ? 1 : (int)Math.Ceiling(_photos.Count / (double)PerPage);
    }

    public int Columns { get; }
    public int Rows { get; }
    public int PerPage { get; }
    public int PageCount { get; }
    public Size PageSize => _settings.PageSize;

    private int ResolvePerPage(out int columns, out int rows)
    {
        (columns, rows) = _settings.Grid.Resolve(_photos.Count, _settings.Landscape);
        return Math.Min(Math.Max(columns * rows, 1), GridPreset.MaxPerPage);
    }

    public DrawingVisual RenderPage(int pageIndex)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            Draw(dc, pageIndex);
        }
        return visual;
    }

    private void Draw(DrawingContext dc, int pageIndex)
    {
        var page = _settings.PageSize;
        dc.DrawRectangle(Brushes.White, null, new Rect(page));

        var margin = _settings.MarginDip;
        var content = new Rect(
            margin, margin,
            Math.Max(page.Width - margin * 2, 1),
            Math.Max(page.Height - margin * 2, 1));

        var showTitle = !string.IsNullOrWhiteSpace(_settings.Title) &&
                        (_settings.TitleOnEveryPage || pageIndex == 0);

        if (showTitle)
        {
            var title = FormatText(_settings.Title, _settings.TitleFontSize, FontWeights.SemiBold,
                                   Brushes.Black, content.Width, TextAlignment.Center);
            dc.DrawText(title, new Point(content.X, content.Y));
            var used = title.Height + _settings.TitleFontSize * 0.5;
            content = new Rect(content.X, content.Y + used, content.Width, Math.Max(content.Height - used, 1));
        }

        if (_settings.ShowPageNumbers && PageCount > 1)
        {
            var footerHeight = 12.0;
            var footer = FormatText($"Page {pageIndex + 1} of {PageCount}", 8, FontWeights.Normal,
                                    FooterBrush, content.Width, TextAlignment.Center);
            dc.DrawText(footer, new Point(content.X, content.Bottom - footer.Height));
            content = new Rect(content.X, content.Y, content.Width, Math.Max(content.Height - footerHeight, 1));
        }

        DrawGrid(dc, content, pageIndex);
    }

    private void DrawGrid(DrawingContext dc, Rect area, int pageIndex)
    {
        var gap = _settings.GapDip;
        var cellWidth = (area.Width - gap * (Columns - 1)) / Columns;
        var cellHeight = (area.Height - gap * (Rows - 1)) / Rows;
        if (cellWidth <= 1 || cellHeight <= 1) return;

        var captionHeight = _settings.ShowCaptions ? _settings.CaptionFontSize * 1.45 : 0;

        var first = pageIndex * PerPage;
        for (var slot = 0; slot < PerPage; slot++)
        {
            var index = first + slot;
            if (index >= _photos.Count) break;

            var column = slot % Columns;
            var row = slot / Columns;
            var cell = new Rect(
                area.X + column * (cellWidth + gap),
                area.Y + row * (cellHeight + gap),
                cellWidth, cellHeight);

            DrawCell(dc, cell, _photos[index], captionHeight);
        }
    }

    private void DrawCell(DrawingContext dc, Rect cell, PhotoItem photo, double captionHeight)
    {
        var imageArea = new Rect(cell.X, cell.Y, cell.Width, Math.Max(cell.Height - captionHeight, 1));
        var bitmap = photo.PrintImage;

        if (bitmap is null)
        {
            dc.DrawRectangle(PlaceholderFill, BorderPen, imageArea);
            var message = FormatText("Could not read image", Math.Min(9, imageArea.Height / 3),
                                     FontWeights.Normal, FooterBrush, imageArea.Width, TextAlignment.Center);
            dc.DrawText(message, new Point(imageArea.X, imageArea.Y + (imageArea.Height - message.Height) / 2));
        }
        else
        {
            var scale = Math.Min(imageArea.Width / bitmap.PixelWidth, imageArea.Height / bitmap.PixelHeight);
            var width = bitmap.PixelWidth * scale;
            var height = bitmap.PixelHeight * scale;
            var placed = new Rect(
                imageArea.X + (imageArea.Width - width) / 2,
                imageArea.Y + (imageArea.Height - height) / 2,
                width, height);

            dc.DrawImage(bitmap, placed);
            if (_settings.ShowBorders) dc.DrawRectangle(null, BorderPen, placed);
        }

        if (captionHeight <= 0) return;

        var caption = FormatText(photo.CaptionFor(_settings.CaptionsIncludeExtension),
                                 _settings.CaptionFontSize, FontWeights.Normal, CaptionBrush,
                                 cell.Width, TextAlignment.Center);
        caption.MaxLineCount = 1;
        caption.Trimming = TextTrimming.CharacterEllipsis;
        dc.DrawText(caption, new Point(cell.X, cell.Bottom - captionHeight + 1));
    }

    private FormattedText FormatText(string text, double size, FontWeight weight, Brush brush,
                                     double maxWidth, TextAlignment alignment)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(_settings.FontFamily, FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(size, 1),
            brush,
            96.0 / 96.0)
        {
            MaxTextWidth = Math.Max(maxWidth, 1),
            TextAlignment = alignment
        };
        return formatted;
    }
}
