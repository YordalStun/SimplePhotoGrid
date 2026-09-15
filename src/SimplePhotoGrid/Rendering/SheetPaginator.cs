using System.Windows;
using System.Windows.Documents;

namespace SimplePhotoGrid.Rendering;

/// <summary>Feeds <see cref="SheetRenderer"/> pages to the print system.</summary>
public sealed class SheetPaginator : DocumentPaginator
{
    private readonly SheetRenderer _renderer;

    public SheetPaginator(SheetRenderer renderer)
    {
        _renderer = renderer;
        _pageSize = renderer.PageSize;
    }

    public override DocumentPage GetPage(int pageNumber)
    {
        var visual = _renderer.RenderPage(pageNumber);
        var box = new Rect(_pageSize);
        return new DocumentPage(visual, _pageSize, box, box);
    }

    public override bool IsPageCountValid => true;

    public override int PageCount => _renderer.PageCount;

    private Size _pageSize;
    public override Size PageSize
    {
        get => _pageSize;
        set => _pageSize = value;
    }

    public override IDocumentPaginatorSource? Source => null;
}
