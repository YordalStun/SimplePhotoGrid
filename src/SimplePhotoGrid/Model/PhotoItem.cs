using System.ComponentModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace SimplePhotoGrid.Model;

/// <summary>One photo in the sheet. Thumbnail is eager (small); the print-resolution
/// bitmap is loaded on first use and cached for the lifetime of the item.</summary>
public sealed class PhotoItem : INotifyPropertyChanged
{
    private BitmapSource? _thumbnail;
    private BitmapSource? _print;
    private bool _failed;

    public PhotoItem(string path) => FilePath = path;

    public string FilePath { get; }

    public string FileName => Path.GetFileName(FilePath);

    public string FileNameNoExtension => Path.GetFileNameWithoutExtension(FilePath);

    public bool Failed
    {
        get => _failed;
        private set { _failed = value; OnPropertyChanged(nameof(Failed)); }
    }

    /// <summary>Small bitmap for the file list. Never null once loading has been attempted.</summary>
    public BitmapSource? Thumbnail
    {
        get
        {
            if (_thumbnail is null && !Failed)
            {
                _thumbnail = ImageLoader.Load(FilePath, 160);
                if (_thumbnail is null) Failed = true;
                OnPropertyChanged(nameof(Thumbnail));
            }
            return _thumbnail;
        }
    }

    /// <summary>Bitmap used for preview and printing. Capped at <see cref="ImageLoader.PrintDecodeWidth"/>
    /// pixels on the long edge, which is well beyond 300dpi for any cell on an A3 sheet.</summary>
    public BitmapSource? PrintImage
    {
        get
        {
            if (_print is null && !Failed)
            {
                _print = ImageLoader.Load(FilePath, ImageLoader.PrintDecodeWidth);
                if (_print is null) Failed = true;
            }
            return _print;
        }
    }

    public string CaptionFor(bool includeExtension) => includeExtension ? FileName : FileNameNoExtension;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
