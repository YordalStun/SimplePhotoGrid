using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimplePhotoGrid.Model;

/// <summary>Decodes images at a bounded size and applies the EXIF orientation flag,
/// which WPF's decoders do not do for us.</summary>
public static class ImageLoader
{
    /// <summary>Long-edge cap for the bitmaps used in preview and printing.</summary>
    public const int PrintDecodeWidth = 2400;

    public static readonly string[] SupportedExtensions =
    {
        ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".ico", ".dib"
    };

    public static bool IsSupported(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    /// <summary>Returns a frozen bitmap no larger than <paramref name="maxLongEdge"/> pixels on its
    /// long edge, or null if the file could not be decoded.</summary>
    public static BitmapSource? Load(string path, int maxLongEdge)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(
                stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            if (decoder.Frames.Count == 0) return null;
            var frame = decoder.Frames[0];

            BitmapSource image = frame;

            var orientation = ReadOrientation(frame);
            image = ApplyOrientation(image, orientation);

            var longEdge = Math.Max(image.PixelWidth, image.PixelHeight);
            if (longEdge > maxLongEdge)
            {
                var scale = (double)maxLongEdge / longEdge;
                image = new TransformedBitmap(image, new ScaleTransform(scale, scale));
            }

            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Reads the pixel dimensions from the file header without decoding the image,
    /// with EXIF orientation applied so the size matches how the photo will be drawn.</summary>
    public static System.Windows.Size? TryGetPixelSize(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            var decoder = BitmapDecoder.Create(
                stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

            if (decoder.Frames.Count == 0) return null;
            var frame = decoder.Frames[0];

            double width = frame.PixelWidth;
            double height = frame.PixelHeight;
            if (ReadOrientation(frame) is 5 or 6 or 7 or 8) (width, height) = (height, width);

            return new System.Windows.Size(width, height);
        }
        catch
        {
            return null;
        }
    }

    private static int ReadOrientation(BitmapFrame frame)
    {
        try
        {
            if (frame.Metadata is BitmapMetadata meta &&
                meta.GetQuery("System.Photo.Orientation") is ushort value)
            {
                return value;
            }
        }
        catch
        {
            // Formats without EXIF throw on the query; treat as "normal".
        }
        return 1;
    }

    private static BitmapSource ApplyOrientation(BitmapSource image, int orientation)
    {
        // EXIF orientation values 1-8. Mirrored variants need a negative scale as well as a rotation.
        var transform = new TransformGroup();
        switch (orientation)
        {
            case 2: transform.Children.Add(new ScaleTransform(-1, 1)); break;
            case 3: transform.Children.Add(new RotateTransform(180)); break;
            case 4: transform.Children.Add(new ScaleTransform(1, -1)); break;
            case 5:
                transform.Children.Add(new ScaleTransform(-1, 1));
                transform.Children.Add(new RotateTransform(90));
                break;
            case 6: transform.Children.Add(new RotateTransform(90)); break;
            case 7:
                transform.Children.Add(new ScaleTransform(-1, 1));
                transform.Children.Add(new RotateTransform(270));
                break;
            case 8: transform.Children.Add(new RotateTransform(270)); break;
            default: return image;
        }
        return new TransformedBitmap(image, transform);
    }
}
