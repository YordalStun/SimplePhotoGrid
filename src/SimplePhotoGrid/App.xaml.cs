using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace SimplePhotoGrid;

public partial class App : Application
{
    // Explorer launches one process per selected file, so the first instance owns the window and
    // later instances hand it their file and exit. That is what makes "select 20 photos, right
    // click, print" land in a single sheet.
    private const string MutexName = @"Local\SimplePhotoGrid.SingleInstance.v1";
    private const string PipeName = "SimplePhotoGrid.Files.v1";

    private Mutex? _mutex;
    private MainWindow? _window;
    private CancellationTokenSource? _serverCancel;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out var isFirstInstance);

        if (!isFirstInstance)
        {
            ForwardToRunningInstance(e.Args);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        _window = new MainWindow();
        MainWindow = _window;
        _window.Show();
        _window.AddFiles(e.Args);

        _serverCancel = new CancellationTokenSource();
        _ = Task.Run(() => RunPipeServerAsync(_serverCancel.Token));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serverCancel?.Cancel();
        _mutex?.Dispose();
        base.OnExit(e);
    }

    private static void ForwardToRunningInstance(IEnumerable<string> paths)
    {
        var payload = paths.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        if (payload.Length == 0) return;

        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(5000);
            using var writer = new StreamWriter(client, new UTF8Encoding(false)) { AutoFlush = true };
            foreach (var path in payload) writer.WriteLine(path);
        }
        catch
        {
            // The first instance may be closing. Nothing useful to do but exit quietly.
        }
    }

    private async Task RunPipeServerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                using var reader = new StreamReader(server, Encoding.UTF8);
                var received = new List<string>();
                while (await reader.ReadLineAsync(token).ConfigureAwait(false) is { } line)
                {
                    if (!string.IsNullOrWhiteSpace(line)) received.Add(line);
                }

                if (received.Count > 0)
                {
                    await Dispatcher.InvokeAsync(() => _window?.AddFilesFromAnotherInstance(received));
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                // Keep listening; a broken client should not kill the server loop.
                await Task.Delay(250, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
