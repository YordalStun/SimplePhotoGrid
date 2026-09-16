using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimplePhotoGrid.Model;

namespace PhotoGridDesign.Design;

/// <summary>Draws a finished design. One renderer feeds the preview, the printer and the image
/// export, so all three agree.</summary>
public sealed class DesignRenderer
{
    private readonly IReadOnlyList<PhotoItem> _photos;
    private readonly DesignSettings _settings;

    public DesignRenderer(IReadOnlyList<PhotoItem> photos, DesignSettings settings)
    {
        _photos = photos;
        _settings = settings;
    }

    public Size CanvasSize => _settings.PageSize;

    public DrawingVisual Render()
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            Draw(dc);
        }
        return visual;
    }

    private void Draw(DrawingContext dc)
    {
        var page = new Rect(_settings.PageSize);
        dc.DrawRectangle(_settings.Theme.CreateBackground(page), null, page);

        var margin = _settings.MarginDip;
        var content = new Rect(
            margin, margin,
            Math.Max(page.Width - margin * 2, 1),
            Math.Max(page.Height - margin * 2, 1));

        content = DrawHeading(dc, content);

        if (_photos.Count == 0)
        {
            var hint = DesignDraw.Text("Add photos in Simple Photo Grid, then press Design.",
                                       16, _settings.BodyFont, FontWeights.Normal,
                                       _settings.Theme.CreateBrush(_settings.Theme.BodyColor),
                                       content.Width, TextAlignment.Center);
            dc.DrawText(hint, new Point(content.X, content.Y + content.Height / 2));
            return;
        }

        _settings.Template.Draw(dc, new DesignCanvas(content, _photos, _settings));
    }

    private Rect DrawHeading(DrawingContext dc, Rect content)
    {
        if (!_settings.HasHeading) return content;

        var used = 0.0;

        if (!string.IsNullOrWhiteSpace(_settings.Title))
        {
            var title = DesignDraw.Text(_settings.Title, _settings.TitleFontSize,
                                        _settings.HeadingFont, FontWeights.Bold,
                                        _settings.Theme.CreateBrush(_settings.Theme.TitleColor),
                                        content.Width, TextAlignment.Center);
            dc.DrawText(title, new Point(content.X, content.Y));
            used += title.Height + 4;
        }

        if (!string.IsNullOrWhiteSpace(_settings.Subtitle))
        {
            var subtitle = DesignDraw.Text(_settings.Subtitle, _settings.TitleFontSize * 0.42,
                                           _settings.BodyFont, FontWeights.Normal,
                                           _settings.Theme.CreateBrush(_settings.Theme.BodyColor),
                                           content.Width, TextAlignment.Center);
            dc.DrawText(subtitle, new Point(content.X, content.Y + used));
            used += subtitle.Height;
        }

        // A short accent rule ties the heading to the theme.
        used += 10;
        var rule = new Rect(content.X + content.Width / 2 - 34, content.Y + used, 68, 3);
        dc.DrawRoundedRectangle(_settings.Theme.CreateBrush(_settings.Theme.AccentColor), null,
                                rule, 1.5, 1.5);
        used += 3 + _settings.TitleFontSize * 0.45;

        return new Rect(content.X, content.Y + used, content.Width,
                        Math.Max(content.Height - used, 1));
    }

    /// <summary>Rasterises the design for saving as a picture.</summary>
    public BitmapSource RenderToBitmap(int dpi)
    {
        var size = CanvasSize;
        var width = (int)Math.Round(size.Width / 96.0 * dpi);
        var height = (int)Math.Round(size.Height / 96.0 * dpi);

        var target = new RenderTargetBitmap(
            Math.Max(width, 1), Math.Max(height, 1), dpi, dpi, PixelFormats.Pbgra32);
        target.Render(Render());
        target.Freeze();
        return target;
    }
}

/// <summary>Single-page paginator for the print path.</summary>
public sealed class DesignPaginator : DocumentPaginator
{
    private readonly DesignRenderer _renderer;
    private Size _pageSize;

    public DesignPaginator(DesignRenderer renderer)
    {
        _renderer = renderer;
        _pageSize = renderer.CanvasSize;
    }

    public override DocumentPage GetPage(int pageNumber)
    {
        var box = new Rect(_pageSize);
        return new DocumentPage(_renderer.Render(), _pageSize, box, box);
    }

    public override bool IsPageCountValid => true;
    public override int PageCount => 1;

    public override Size PageSize
    {
        get => _pageSize;
        set => _pageSize = value;
    }

    public override IDocumentPaginatorSource? Source => null;
}
