using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SimplePhotoGrid.Model;

namespace SimplePhotoGrid.Rendering;

/// <summary>One photo resampled to the size it occupies on paper and re-encoded, held as a file
/// on disk rather than in memory.
///
/// The file matters. WPF's XPS serializer identifies image resources by the frame's decoder, and
/// frames created from a MemoryStream all look alike to it, so it reuses the first image for
/// every cell on the sheet. A distinct file URI per photo gives each one its own identity. Disk
/// also keeps memory flat: only the images on the page being spooled are ever decoded.</summary>
public sealed class PreparedImage
{
    public PreparedImage(string cachePath, long byteCount, int pixelWidth, int pixelHeight)
    {
        CachePath = cachePath;
        ByteCount = byteCount;
        PixelWidth = pixelWidth;
        PixelHeight = pixelHeight;
    }

    public string CachePath { get; }
    public long ByteCount { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }

    public BitmapSource Decode()
    {
        var frame = BitmapFrame.Create(
            new Uri(CachePath), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        frame.Freeze();
        return frame;
    }
}

/// <summary>The prepared images for one print job, plus the scratch directory holding them.</summary>
public sealed class PreparedImageSet : IDisposable
{
    private readonly string? _directory;

    public PreparedImageSet(IReadOnlyDictionary<string, PreparedImage> images, string? directory)
    {
        Images = images;
        TotalBytes = images.Values.Sum(image => image.ByteCount);
        _directory = directory;
    }

    public IReadOnlyDictionary<string, PreparedImage> Images { get; }
    public long TotalBytes { get; }

    public static readonly PreparedImageSet Empty =
        new(new Dictionary<string, PreparedImage>(StringComparer.OrdinalIgnoreCase), null);

    public void Dispose()
    {
        if (_directory is null) return;

        try
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        }
        catch
        {
            // The spooler may still hold a handle. ScratchSpace.SweepOldJobs clears it next launch.
        }
    }
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

        var directory = ScratchSpace.CreateJobDirectory();
        var images = new Dictionary<string, PreparedImage>(StringComparer.OrdinalIgnoreCase);

        try
        {
            for (var i = 0; i < paths.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                var path = paths[i];
                progress?.Report(new PreparationProgress(i, paths.Count, Path.GetFileName(path)));

                var prepared = PrepareOne(path, directory, i, cellPixels, jpegQuality);
                if (prepared is not null) images[path] = prepared;
            }
        }
        catch
        {
            new PreparedImageSet(images, directory).Dispose();
            throw;
        }

        progress?.Report(new PreparationProgress(paths.Count, paths.Count, string.Empty));
        return new PreparedImageSet(images, directory);
    }

    private static PreparedImage? PrepareOne(string path, string directory, int index,
                                             Size cellPixels, int jpegQuality)
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
        var isPng = MayHaveAlpha(source.Format);
        BitmapEncoder encoder = isPng
            ? new PngBitmapEncoder()
            : new JpegBitmapEncoder { QualityLevel = jpegQuality };

        encoder.Frames.Add(BitmapFrame.Create(source));

        // The index keeps every file name unique even when two photos share a name.
        var file = Path.Combine(directory, $"{index:D4}{(isPng ? ".png" : ".jpg")}");
        using (var stream = File.Create(file))
        {
            encoder.Save(stream);
        }

        return new PreparedImage(file, new FileInfo(file).Length, source.PixelWidth, source.PixelHeight);
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

/// <summary>Scratch directory for prepared print images.</summary>
public static class ScratchSpace
{
    private static string Root => Path.Combine(Path.GetTempPath(), "SimplePhotoGrid");

    public static string CreateJobDirectory()
    {
        var directory = Path.Combine(Root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Clears job directories a previous run could not delete, for instance because the
    /// spooler still held a file when the app closed.</summary>
    public static void SweepOldJobs()
    {
        try
        {
            if (!Directory.Exists(Root)) return;

            foreach (var directory in Directory.EnumerateDirectories(Root))
            {
                try
                {
                    if (Directory.GetLastWriteTimeUtc(directory) < DateTime.UtcNow.AddHours(-6))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
                catch
                {
                    // Still in use, or not ours. Leave it.
                }
            }
        }
        catch
        {
            // Temp unavailable; nothing to clean up.
        }
    }
}
