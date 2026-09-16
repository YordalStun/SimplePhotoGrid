using System.IO;
using System.Windows;

namespace PhotoGridDesign;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
        window.AddFiles(ResolvePhotos(e.Args));
    }

    /// <summary>Photos arrive either as "--photos handoff.txt" from the main app, or as plain
    /// paths when the Design program is started on its own.</summary>
    private static IEnumerable<string> ResolvePhotos(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], "--photos", StringComparison.OrdinalIgnoreCase))
            {
                yield return args[i];
                continue;
            }

            if (i + 1 >= args.Length) yield break;

            var handoff = args[++i];
            string[] lines;
            try
            {
                lines = File.ReadAllLines(handoff);
            }
            catch
            {
                continue;
            }

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line)) yield return line;
            }

            try
            {
                File.Delete(handoff);
            }
            catch
            {
                // Harmless; the main app's temp sweep clears it later.
            }
        }
    }
}
