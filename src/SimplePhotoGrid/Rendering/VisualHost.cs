using System.Windows;
using System.Windows.Media;

namespace SimplePhotoGrid.Rendering;

/// <summary>Hosts a raw <see cref="Visual"/> (the rendered page) inside the WPF tree so the
/// preview can show exactly the drawing that will be printed.</summary>
public sealed class VisualHost : FrameworkElement
{
    private Visual? _child;

    public Visual? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value)) return;
            if (_child is not null) RemoveVisualChild(_child);
            _child = value;
            if (_child is not null) AddVisualChild(_child);
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override int VisualChildrenCount => _child is null ? 0 : 1;

    protected override Visual GetVisualChild(int index) =>
        _child ?? throw new ArgumentOutOfRangeException(nameof(index));
}
