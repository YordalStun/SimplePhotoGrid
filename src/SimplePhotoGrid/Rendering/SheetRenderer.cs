using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

    private const double FooterHeightDip = 12.0;

    static SheetRenderer()
    {
        FooterBrush.Freeze();
        BorderPen.Freeze();
        PlaceholderFill.Freeze();
    }

    private readonly IReadOnlyList<PhotoItem> _photos;
    private readonly SheetSettings _settings;
    private readonly PreparedImageSet _prepared;

    /// <param name="prepared">Print-sized, compressed images. When empty the renderer falls back
    /// to each photo's preview bitmap, which is what the on-screen preview uses.</param>
    public SheetRenderer(IReadOnlyList<PhotoItem> photos, SheetSettings settings,
                         PreparedImageSet? prepared = null)
    {
        _photos = photos;
        _settings = settings;
        _prepared = prepared ?? PreparedImageSet.Empty;

        var (columns, rows) = _settings.Grid.Resolve(_photos.Count, _settings.Landscape);
        Columns = Math.Max(columns, 1);
        Rows = Math.Max(rows, 1);
        PerPage = Math.Min(Columns * Rows, GridPreset.MaxPerPage);
        PageCount = _photos.Count == 0 ? 1 : (int)Math.Ceiling(_photos.Count / (double)PerPage);
    }

    public int Columns { get; }
    public int Rows { get; }
    public int PerPage { get; }
    public int PageCount { get; }
    public Size PageSize => _settings.PageSize;

    private sealed record PageLayout(Rect Grid, FormattedText? Title, Point TitleOrigin,
                                     FormattedText? Footer, Point FooterOrigin);

    private PageLayout Layout(int pageIndex)
    {
        var page = _settings.PageSize;
        var margin = _settings.MarginDip;
        var content = new Rect(
            margin, margin,
            Math.Max(page.Width - margin * 2, 1),
            Math.Max(page.Height - margin * 2, 1));

        FormattedText? title = null;
        var titleOrigin = new Point();
        var showTitle = !string.IsNullOrWhiteSpace(_settings.Title) &&
                        (_settings.TitleOnEveryPage || pageIndex == 0);

        if (showTitle)
        {
            title = FormatText(_settings.Title, _settings.TitleFontSize, FontWeights.SemiBold,
                               Brushes.Black, content.Width, TextAlignment.Center);
            titleOrigin = new Point(content.X, content.Y);
            var used = title.Height + _settings.TitleFontSize * 0.5;
            content = new Rect(content.X, content.Y + used, content.Width, Math.Max(content.Height - used, 1));
        }

        FormattedText? footer = null;
        var footerOrigin = new Point();
        if (_settings.ShowPageNumbers && PageCount > 1)
        {
            footer = FormatText($"Page {pageIndex + 1} of {PageCount}", 8, FontWeights.Normal,
                                FooterBrush, content.Width, TextAlignment.Center);
            footerOrigin = new Point(content.X, content.Bottom - footer.Height);
            content = new Rect(content.X, content.Y, content.Width, Math.Max(content.Height - FooterHeightDip, 1));
        }

        return new PageLayout(content, title, titleOrigin, footer, footerOrigin);
    }

    private double CaptionHeight => _settings.ShowCaptions ? _settings.CaptionFontSize * 1.45 : 0;

    private Size CellSize(Rect grid)
    {
        var gap = _settings.GapDip;
        return new Size(
            Math.Max((grid.Width - gap * (Columns - 1)) / Columns, 1),
            Math.Max((grid.Height - gap * (Rows - 1)) / Rows, 1));
    }

    /// <summary>Area one photo occupies on paper, in DIPs (1/96"). Drives how far the print
    /// images are downsampled, so the spool carries paper-sized pixels rather than camera-sized
    /// ones.</summary>
    public Size PhotoAreaDip(int pageIndex = 0)
    {
        var cell = CellSize(Layout(pageIndex).Grid);
        return new Size(cell.Width, Math.Max(cell.Height - CaptionHeight, 1));
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
        dc.DrawRectangle(Brushes.White, null, new Rect(_settings.PageSize));

        var layout = Layout(pageIndex);
        if (layout.Title is not null) dc.DrawText(layout.Title, layout.TitleOrigin);
        if (layout.Footer is not null) dc.DrawText(layout.Footer, layout.FooterOrigin);

        DrawGrid(dc, layout.Grid, pageIndex);
    }

    private void DrawGrid(DrawingContext dc, Rect area, int pageIndex)
    {
        var gap = _settings.GapDip;
        var cell = CellSize(area);
        if (cell.Width <= 1 || cell.Height <= 1) return;

        var captionHeight = CaptionHeight;
        var first = pageIndex * PerPage;

        for (var slot = 0; slot < PerPage; slot++)
        {
            var index = first + slot;
            if (index >= _photos.Count) break;

            var column = slot % Columns;
            var row = slot / Columns;
            var bounds = new Rect(
                area.X + column * (cell.Width + gap),
                area.Y + row * (cell.Height + gap),
                cell.Width, cell.Height);

            DrawCell(dc, bounds, _photos[index], captionHeight);
        }
    }

    private BitmapSource? ImageFor(PhotoItem photo) =>
        _prepared.Images.TryGetValue(photo.FilePath, out var prepared)
            ? prepared.Decode()
            : photo.PrintImage;

    private void DrawCell(DrawingContext dc, Rect cell, PhotoItem photo, double captionHeight)
    {
        var imageArea = new Rect(cell.X, cell.Y, cell.Width, Math.Max(cell.Height - captionHeight, 1));
        var bitmap = ImageFor(photo);

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
        return new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(_settings.FontFamily, FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(size, 1),
            brush,
            1.0)
        {
            MaxTextWidth = Math.Max(maxWidth, 1),
            TextAlignment = alignment
        };
    }
}
