using System.ComponentModel;
using System.Windows;
using SimplePhotoGrid.Rendering;

namespace SimplePhotoGrid;

/// <summary>Progress for the resample-and-compress pass that runs when Print is pressed.</summary>
public partial class PrintProgressWindow : Window
{
    private readonly CancellationTokenSource _cancel = new();
    private bool _allowClose;

    public PrintProgressWindow()
    {
        InitializeComponent();
    }

    public CancellationToken Token => _cancel.Token;

    public void Report(PreparationProgress progress)
    {
        if (progress.Total <= 0) return;

        Bar.Value = Math.Clamp(progress.Done * 100.0 / progress.Total, 0, 100);
        HeadingText.Text = $"Compressing photos for the printer... {progress.Done} of {progress.Total}";
        DetailText.Text = string.IsNullOrEmpty(progress.FileName) ? " " : progress.FileName;
    }

    /// <summary>The compression pass is done; the spool write happens on the UI thread and cannot
    /// report progress, so the bar goes indeterminate for that stretch.</summary>
    public void ShowSending()
    {
        HeadingText.Text = "Sending to the printer...";
        DetailText.Text = " ";
        Bar.IsIndeterminate = true;
        CancelButton.IsEnabled = false;
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        HeadingText.Text = "Cancelling...";
        _cancel.Cancel();
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
        // Closing the window is a cancel request; the print flow closes it for real when done.
        if (_allowClose) return;

        e.Cancel = true;
        _cancel.Cancel();
    }
}
