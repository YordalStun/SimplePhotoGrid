using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimplePhotoGrid.Model;

namespace SimplePhotoGrid.Rendering;

/// <summary>One photo resampled to the size it occupies on paper and re-encoded. The encoded
/// bytes are kept rather than a decoded bitmap: the renderer decodes on demand, so only the
/// images on the page being spooled are ever in memory at once.</summary>
public sealed class PreparedImage
{
    private readonly byte[] _data;

    public PreparedImage(byte[] data, int pixelWidth, int pixelHeight)
    {
        _data = data;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    public int ByteCount => _data.Length;
    public int PixelWidth { get; }
    public int PixelHeight { get; }

    /// <summary>Decodes a frozen frame straight from the encoded bytes. Creating the frame from
    /// the original JPEG stream lets the XPS spooler carry those bytes through rather than
    /// re-encoding the pixels.</summary>
    public BitmapSource Decode()
    {
        using var stream = new MemoryStream(_data, writable: false);
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        frame.Freeze();
        return frame;
    }
}

public sealed class PreparedImageSet
{
    public PreparedImageSet(IReadOnlyDictionary<string, PreparedImage> images)
    {
        Images = images;
        TotalBytes = images.Values.Sum(image => (long)image.ByteCount);
    }

    public IReadOnlyDictionary<string, PreparedImage> Images { get; }
    public long TotalBytes { get; }

    public static readonly PreparedImageSet Empty =
        new(new Dictionary<string, PreparedImage>(StringComparer.OrdinalIgnoreCase));
}

public readonly record struct PreparationProgress(int Done, int Total, string FileName);

/// <summary>Resamples and compresses the photos for one print job.</summary>
public static class PrintImagePreparer
{
    public static PreparedImageSet Prepare(
        IReadOnlyList<PhotoItem> photos,
        Size cellPixels,
        int jpegQuality,
        IProgress<PreparationProgress>? progress,
        CancellationToken token)
    {
        var paths = photos
            .Select(photo => photo.FilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var images = new Dictionary<string, PreparedImage>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < paths.Count; i++)
        {
            token.ThrowIfCancellationRequested();

            var path = paths[i];
            progress?.Report(new PreparationProgress(i, paths.Count, Path.GetFileName(path)));

            var prepared = PrepareOne(path, cellPixels, jpegQuality);
            if (prepared is not null) images[path] = prepared;
        }

        progress?.Report(new PreparationProgress(paths.Count, paths.Count, string.Empty));
        return new PreparedImageSet(images);
    }

    private static PreparedImage? PrepareOne(string path, Size cellPixels, int jpegQuality)
    {
        // A photo is drawn to fit inside the cell, so its long edge on paper is never larger
        // than the cell's long edge. Decoding to that cap does the downscaling for us, and
        // never upscales a photo that is already smaller.
        var cap = (int)Math.Ceiling(Math.Max(cellPixels.Width, cellPixels.Height));
        cap = Math.Clamp(cap, 64, PrintQuality.MaxPixelsPerSide);

        var source = ImageLoader.Load(path, cap);
        if (source is null) return null;

        // JPEG has no alpha channel, so anything that might carry transparency stays PNG rather
        // than acquiring a black background.
        BitmapEncoder encoder = MayHaveAlpha(source.Format)
            ? new PngBitmapEncoder()
            : new JpegBitmapEncoder { QualityLevel = jpegQuality };

        encoder.Frames.Add(BitmapFrame.Create(source));

        using var buffer = new MemoryStream();
        encoder.Save(buffer);
        return new PreparedImage(buffer.ToArray(), source.PixelWidth, source.PixelHeight);
    }

    private static bool MayHaveAlpha(PixelFormat format) =>
        format == PixelFormats.Bgra32 ||
        format == PixelFormats.Pbgra32 ||
        format == PixelFormats.Rgba64 ||
        format == PixelFormats.Prgba64 ||
        format == PixelFormats.Rgba128Float ||
        format == PixelFormats.Prgba128Float ||
        format == PixelFormats.Indexed1 ||
        format == PixelFormats.Indexed2 ||
        format == PixelFormats.Indexed4 ||
        format == PixelFormats.Indexed8;

    public static string DescribeSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / (1024.0 * 1024.0):0.#} MB"
    };
}
