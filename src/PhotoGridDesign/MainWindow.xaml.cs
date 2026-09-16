using System.Collections.ObjectModel;
using System.IO;
using System.Printing;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using PhotoGridDesign.Design;
using SimplePhotoGrid.Model;

namespace PhotoGridDesign;

public partial class MainWindow : Window
{
    /// <summary>Matches the sheet program's per-page cap; beyond this a collage stops reading.</summary>
    private const int MaxPhotos = 32;

    private static readonly string[] HeadingFonts =
        { "Georgia", "Segoe UI", "Segoe UI Light", "Trebuchet MS", "Palatino Linotype", "Impact" };

    private readonly ObservableCollection<PhotoItem> _photos = new();
    private readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);
    private readonly DesignSettings _settings = new();
    private readonly DispatcherTimer _refreshTimer;

    private DesignRenderer? _renderer;
    private bool _loaded;

    public MainWindow()
    {
        InitializeComponent();

        TemplateList.ItemsSource = DesignTemplate.All;
        TemplateList.SelectedIndex = 0;
        ThemeCombo.ItemsSource = DesignTheme.All;
        ThemeCombo.SelectedItem = DesignTheme.Cream;
        PaperCombo.ItemsSource = PaperSize.All;
        PaperCombo.SelectedItem = PaperSize.A4;
        FontCombo.ItemsSource = HeadingFonts;
        FontCombo.SelectedIndex = 0;

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _refreshTimer.Tick += (_, _) => { _refreshTimer.Stop(); Refresh(); };

        Loaded += (_, _) => { _loaded = true; Refresh(); };
    }

    // ---------------------------------------------------------------- photos

    public void AddFiles(IEnumerable<string> paths)
    {
        var skipped = 0;

        foreach (var path in Expand(paths))
        {
            if (_photos.Count >= MaxPhotos)
            {
                skipped++;
                continue;
            }
            if (_paths.Add(path)) _photos.Add(new PhotoItem(path));
        }

        if (skipped > 0)
        {
            StatusText.Text = $"Using the first {MaxPhotos} photos; {skipped} left out.";
        }

        QueueRefresh();
    }

    private static IEnumerable<string> Expand(IEnumerable<string> paths)
    {
        foreach (var raw in paths)
        {
            var path = raw.Trim().Trim('"');
            if (path.Length == 0) continue;

            if (Directory.Exists(path))
            {
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(path).Where(ImageLoader.IsSupported).Order();
                }
                catch
                {
                    continue;
                }
                foreach (var file in files) yield return file;
            }
            else if (File.Exists(path) && ImageLoader.IsSupported(path))
            {
                yield return Path.GetFullPath(path);
            }
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files);
        e.Handled = true;
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var filter = "Images|" + string.Join(";", ImageLoader.SupportedExtensions.Select(x => "*" + x))
                     + "|All files|*.*";
        var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Filter = filter };
        if (dialog.ShowDialog(this) == true) AddFiles(dialog.FileNames);
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        _photos.Clear();
        _paths.Clear();
        QueueRefresh();
    }

    // ---------------------------------------------------------------- settings & preview

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        QueueRefresh();
    }

    private void OnShuffleClick(object sender, RoutedEventArgs e)
    {
        _settings.Seed = Random.Shared.Next(1, int.MaxValue);
        Refresh();
    }

    private void QueueRefresh()
    {
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private void ReadSettings()
    {
        _settings.Template = TemplateList.SelectedItem as DesignTemplate ?? DesignTemplate.All[0];
        _settings.Theme = ThemeCombo.SelectedItem as DesignTheme ?? DesignTheme.Cream;
        _settings.Paper = PaperCombo.SelectedItem as PaperSize ?? PaperSize.A4;
        _settings.Landscape = OrientationCombo.SelectedIndex == 1;
        _settings.Title = TitleBox.Text;
        _settings.Subtitle = SubtitleBox.Text;
        _settings.TitleFontSize = TitleSizeSlider.Value;
        _settings.ShowCaptions = CaptionsCheck.IsChecked == true;
        _settings.Shadow = ShadowCheck.IsChecked == true;
        _settings.FrameWidth = FrameSlider.Value;
        _settings.CornerRadius = RadiusSlider.Value;
        _settings.Spacing = SpacingSlider.Value;
        _settings.Tilt = TiltSlider.Value;

        if (FontCombo.SelectedItem is string font) _settings.HeadingFont = new FontFamily(font);
    }

    private void Refresh()
    {
        ReadSettings();

        _renderer = new DesignRenderer(_photos.ToList(), _settings);

        var size = _renderer.CanvasSize;
        PreviewHost.Width = size.Width;
        PreviewHost.Height = size.Height;
        PreviewHost.Child = _renderer.Render();

        EmptyHint.Visibility = _photos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TemplateHint.Text = _settings.Template.Description;

        CountText.Text = _photos.Count switch
        {
            0 => "No photos yet",
            1 => "1 photo",
            _ => $"{_photos.Count} photos"
        };
    }

    // ---------------------------------------------------------------- output

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (!EnsurePhotos()) return;
        ReadSettings();

        var dialog = new PrintDialog { UserPageRangeEnabled = false };

        try
        {
            var ticket = dialog.PrintTicket;
            if (ticket is not null)
            {
                ticket.PageOrientation = _settings.Landscape
                    ? PageOrientation.Landscape
                    : PageOrientation.Portrait;
                ticket.PageMediaSize = _settings.Paper.MediaName is { } media
                    ? new PageMediaSize(media)
                    : new PageMediaSize(_settings.Paper.Width, _settings.Paper.Height);
            }
        }
        catch
        {
            // Some drivers reject a ticket built up front; the dialog's defaults will do.
        }

        if (dialog.ShowDialog() != true) return;

        try
        {
            var renderer = new DesignRenderer(_photos.ToList(), _settings);
            dialog.PrintDocument(new DesignPaginator(renderer), "Photo Grid Design");
            StatusText.Text = $"Sent to {dialog.PrintQueue?.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Printing failed:\n\n" + ex.Message, "Photo Grid Design",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (!EnsurePhotos()) return;
        ReadSettings();

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG image|*.png|JPEG image|*.jpg",
            FileName = SuggestFileName(),
            DefaultExt = ".png"
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var renderer = new DesignRenderer(_photos.ToList(), _settings);
            var bitmap = renderer.RenderToBitmap(300);

            var jpeg = dialog.FilterIndex == 2 ||
                       Path.GetExtension(dialog.FileName).Equals(".jpg", StringComparison.OrdinalIgnoreCase);

            BitmapEncoder encoder = jpeg
                ? new JpegBitmapEncoder { QualityLevel = 92 }
                : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = File.Create(dialog.FileName))
            {
                encoder.Save(stream);
            }

            StatusText.Text = $"Saved {Path.GetFileName(dialog.FileName)} " +
                              $"({bitmap.PixelWidth} x {bitmap.PixelHeight})";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Could not save the picture:\n\n" + ex.Message,
                            "Photo Grid Design", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string SuggestFileName()
    {
        var title = _settings.Title;
        if (string.IsNullOrWhiteSpace(title)) return "design.png";

        var clean = new string(title.Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
        return (clean.Length == 0 ? "design" : clean) + ".png";
    }

    private bool EnsurePhotos()
    {
        if (_photos.Count > 0) return true;

        MessageBox.Show(this, "Add some photos first.", "Photo Grid Design",
                        MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }
}
