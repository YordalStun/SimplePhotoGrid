using System.Collections.ObjectModel;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SimplePhotoGrid.Model;
using SimplePhotoGrid.Rendering;

namespace SimplePhotoGrid;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PhotoItem> _photos = new();
    private readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);
    private readonly SheetSettings _settings = new();
    private readonly DispatcherTimer _refreshTimer;

    private SheetRenderer? _renderer;
    private int _pageIndex;
    private bool _loaded;

    public MainWindow()
    {
        InitializeComponent();

        PhotoList.ItemsSource = _photos;
        PaperCombo.ItemsSource = PaperSize.All;
        PaperCombo.SelectedItem = PaperSize.A4;
        GridCombo.ItemsSource = GridPreset.All;
        GridCombo.SelectedItem = GridPreset.Auto;
        QualityCombo.ItemsSource = PrintQuality.All;
        QualityCombo.SelectedItem = PrintQuality.Normal;

        // Explorer fires one process per selected file; the pipe delivers them in a burst, so
        // coalesce redraws rather than re-rendering the sheet twenty times.
        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _refreshTimer.Tick += (_, _) => { _refreshTimer.Stop(); Refresh(); };

        Loaded += (_, _) =>
        {
            _loaded = true;
            UpdateShellButton();
            Refresh();
        };
    }

    // ---------------------------------------------------------------- adding photos

    public void AddFiles(IEnumerable<string> paths)
    {
        var added = 0;
        foreach (var path in Expand(paths))
        {
            if (_paths.Add(path))
            {
                _photos.Add(new PhotoItem(path));
                added++;
            }
        }

        if (added > 0) QueueRefresh();
        else UpdateCounts();
    }

    /// <summary>Called when a second copy of the app was launched (right-click on a multi-selection)
    /// and handed us its file.</summary>
    public void AddFilesFromAnotherInstance(IEnumerable<string> paths)
    {
        AddFiles(paths);

        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
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

    // ---------------------------------------------------------------- drag and drop

    private void OnDragOver(object sender, DragEventArgs e)
    {
        var hasFiles = e.Data.GetDataPresent(DataFormats.FileDrop);
        e.Effects = hasFiles ? DragDropEffects.Copy : DragDropEffects.None;
        DropOverlay.Visibility = hasFiles ? Visibility.Visible : Visibility.Collapsed;
        e.Handled = true;
    }

    private void OnDragLeave(object sender, DragEventArgs e) =>
        DropOverlay.Visibility = Visibility.Collapsed;

    private void OnDrop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files);
        e.Handled = true;
    }

    // ---------------------------------------------------------------- list commands

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var filter = "Images|" + string.Join(";", ImageLoader.SupportedExtensions.Select(x => "*" + x))
                     + "|All files|*.*";
        var dialog = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Filter = filter };
        if (dialog.ShowDialog(this) == true) AddFiles(dialog.FileNames);
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        var selected = PhotoList.SelectedItems.Cast<PhotoItem>().ToList();
        if (selected.Count == 0) return;

        foreach (var item in selected)
        {
            _photos.Remove(item);
            _paths.Remove(item.FilePath);
        }
        QueueRefresh();
    }

    private void OnClearClick(object sender, RoutedEventArgs e)
    {
        if (_photos.Count == 0) return;
        _photos.Clear();
        _paths.Clear();
        _pageIndex = 0;
        QueueRefresh();
    }

    private void OnSortClick(object sender, RoutedEventArgs e)
    {
        var sorted = _photos.OrderBy(p => p.FileName, NaturalComparer.Instance).ToList();
        _photos.Clear();
        foreach (var item in sorted) _photos.Add(item);
        QueueRefresh();
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e) => Move(-1);

    private void OnMoveDownClick(object sender, RoutedEventArgs e) => Move(+1);

    private void Move(int delta)
    {
        var selected = PhotoList.SelectedItems.Cast<PhotoItem>()
            .Select(item => _photos.IndexOf(item))
            .OrderBy(index => delta < 0 ? index : -index)
            .ToList();
        if (selected.Count == 0) return;

        foreach (var index in selected)
        {
            var target = index + delta;
            if (target < 0 || target >= _photos.Count) return;
            _photos.Move(index, target);
        }
        QueueRefresh();
    }

    // ---------------------------------------------------------------- settings & preview

    private void OnSettingChanged(object sender, RoutedEventArgs e)
    {
        if (!_loaded) return;
        QueueRefresh();
    }

    private void QueueRefresh()
    {
        UpdateCounts();
        _refreshTimer.Stop();
        _refreshTimer.Start();
    }

    private void ReadSettings()
    {
        _settings.Paper = PaperCombo.SelectedItem as PaperSize ?? PaperSize.A4;
        _settings.Landscape = OrientationCombo.SelectedIndex == 1;
        _settings.Grid = GridCombo.SelectedItem as GridPreset ?? GridPreset.Auto;
        _settings.MarginMm = ParseNumber(MarginBox.Text, 10, 0, 50);
        _settings.GapMm = ParseNumber(GapBox.Text, 4, 0, 40);
        _settings.Title = TitleBox.Text;
        _settings.TitleOnEveryPage = TitleEveryPageCheck.IsChecked == true;
        _settings.TitleFontSize = TitleSizeSlider.Value;
        _settings.ShowCaptions = CaptionsCheck.IsChecked == true;
        _settings.CaptionsIncludeExtension = CaptionExtensionCheck.IsChecked == true;
        _settings.CaptionFontSize = CaptionSizeSlider.Value;
        _settings.ShowBorders = BordersCheck.IsChecked == true;
        _settings.ShowPageNumbers = PageNumbersCheck.IsChecked == true;
        _settings.Quality = QualityCombo.SelectedItem as PrintQuality ?? PrintQuality.Normal;
    }

    private static double ParseNumber(string text, double fallback, double min, double max) =>
        double.TryParse(text, out var value) ? Math.Clamp(value, min, max) : fallback;

    private void Refresh()
    {
        ReadSettings();
        _renderer = new SheetRenderer(_photos.ToList(), _settings);
        _pageIndex = Math.Clamp(_pageIndex, 0, _renderer.PageCount - 1);

        var size = _renderer.PageSize;
        PreviewHost.Width = size.Width;
        PreviewHost.Height = size.Height;
        PreviewHost.Child = _renderer.RenderPage(_pageIndex);

        EmptyHint.Visibility = _photos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        PageText.Text = $"Page {_pageIndex + 1} of {_renderer.PageCount}";
        UpdateCounts();
    }

    private void UpdateCounts()
    {
        CountText.Text = _photos.Count switch
        {
            0 => "No photos yet",
            1 => "1 photo",
            _ => $"{_photos.Count} photos"
        };

        if (_renderer is null) return;
        StatusText.Text = $"{_renderer.Columns} x {_renderer.Rows} per page " +
                          $"({_renderer.PerPage} photos, max {GridPreset.MaxPerPage}) " +
                          $"· {_settings.Paper.Name} {(_settings.Landscape ? "landscape" : "portrait")}";
    }

    private void OnPreviousPageClick(object sender, RoutedEventArgs e) => GoToPage(_pageIndex - 1);

    private void OnNextPageClick(object sender, RoutedEventArgs e) => GoToPage(_pageIndex + 1);

    private void GoToPage(int index)
    {
        if (_renderer is null) return;
        var target = Math.Clamp(index, 0, _renderer.PageCount - 1);
        if (target == _pageIndex) return;
        _pageIndex = target;
        PreviewHost.Child = _renderer.RenderPage(_pageIndex);
        PageText.Text = $"Page {_pageIndex + 1} of {_renderer.PageCount}";
    }

    // ---------------------------------------------------------------- printing

    private bool _printing;

    private async void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (_printing) return;

        if (_photos.Count == 0)
        {
            MessageBox.Show(this, "Add some photos first.", "Simple Photo Grid",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ReadSettings();

        // Page ranges are left off: the sheet is generated as a whole and the range UI would
        // imply a selection we do not honour.
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
            // Some drivers reject a ticket we build up front; the dialog's own defaults will do.
        }

        if (dialog.ShowDialog() != true) return;

        _printing = true;
        var photos = _photos.ToList();

        // Photos are drawn at cell size, so sending camera-resolution bitmaps to the spooler
        // wastes tens of megabytes per page. Resample to what the paper can actually resolve.
        var photoArea = new SheetRenderer(photos, _settings).PhotoAreaDip();
        var dpi = _settings.Quality.Dpi;
        var targetPixels = new Size(
            Math.Ceiling(photoArea.Width / 96.0 * dpi),
            Math.Ceiling(photoArea.Height / 96.0 * dpi));

        var progressWindow = new PrintProgressWindow { Owner = this };
        IsEnabled = false;
        progressWindow.Show();

        try
        {
            var reporter = new Progress<PreparationProgress>(progressWindow.Report);
            var token = progressWindow.Token;

            var prepared = await Task.Run(
                () => PrintImagePreparer.Prepare(photos, targetPixels, _settings.Quality.JpegQuality,
                                                 reporter, token),
                token);

            token.ThrowIfCancellationRequested();
            progressWindow.ShowSending();

            var renderer = new SheetRenderer(photos, _settings, prepared);
            dialog.PrintDocument(new SheetPaginator(renderer), "Simple Photo Grid");

            StatusText.Text =
                $"Sent {renderer.PageCount} page(s) to {dialog.PrintQueue?.Name} " +
                $"\u00b7 {PrintImagePreparer.DescribeSize(prepared.TotalBytes)} of image data";
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Printing cancelled.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Printing failed:\n\n" + ex.Message, "Simple Photo Grid",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            progressWindow.AllowClose();
            progressWindow.Close();
            IsEnabled = true;
            _printing = false;
        }
    }

    // ---------------------------------------------------------------- shell integration

    private void OnShellIntegrationClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ExplorerIntegration.IsRegistered)
            {
                ExplorerIntegration.Unregister();
                MessageBox.Show(this, "Removed from the Explorer right-click menu.",
                                "Simple Photo Grid", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                ExplorerIntegration.Register();
                MessageBox.Show(this,
                    "Added. Select photos in Explorer, right-click one and choose " +
                    "\"Print with Simple Photo Grid\".\n\n" +
                    "On Windows 11 it appears under \"Show more options\".",
                    "Simple Photo Grid", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Simple Photo Grid",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }

        UpdateShellButton();
    }

    private void UpdateShellButton()
    {
        try
        {
            ShellButton.Content = ExplorerIntegration.IsRegistered
                ? "Remove from Explorer right-click menu"
                : "Add to Explorer right-click menu";
        }
        catch
        {
            ShellButton.IsEnabled = false;
        }
    }
}

/// <summary>Orders IMG_2 before IMG_10, the way Explorer does.</summary>
internal sealed class NaturalComparer : IComparer<string>
{
    public static readonly NaturalComparer Instance = new();

    public int Compare(string? x, string? y)
    {
        if (x is null || y is null) return string.CompareOrdinal(x, y);

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var startX = i;
                var startY = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;

                var numX = x.AsSpan(startX, i - startX).TrimStart('0');
                var numY = y.AsSpan(startY, j - startY).TrimStart('0');
                if (numX.Length != numY.Length) return numX.Length - numY.Length;

                var digits = numX.SequenceCompareTo(numY);
                if (digits != 0) return digits;
            }
            else
            {
                var compared = char.ToUpperInvariant(x[i]).CompareTo(char.ToUpperInvariant(y[j]));
                if (compared != 0) return compared;
                i++;
                j++;
            }
        }
        return (x.Length - i) - (y.Length - j);
    }
}
