using Microsoft.Win32;

namespace SimplePhotoGrid;

/// <summary>Adds/removes a per-user "Print with Simple Photo Grid" entry on the right-click menu
/// of image files. Per-user (HKCU) so no administrator rights are needed.</summary>
public static class ExplorerIntegration
{
    private const string KeyPath = @"Software\Classes\SystemFileAssociations\image\shell\SimplePhotoGrid";
    private const string VerbLabel = "Print with Simple Photo Grid";

    public static bool IsRegistered
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath + @"\command");
            return key?.GetValue(null) is string command && command.Contains("SimplePhotoGrid", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Register()
    {
        var exe = Environment.ProcessPath
                  ?? throw new InvalidOperationException("Could not determine the application path.");

        using var key = Registry.CurrentUser.CreateSubKey(KeyPath)
                        ?? throw new InvalidOperationException("Could not write to the registry.");
        key.SetValue(null, VerbLabel);
        key.SetValue("Icon", exe);
        // Tells Explorer this verb copes with a large multi-selection rather than greying out
        // past the default 15-item limit. Explorer still invokes us once per file; the
        // single-instance pipe in App.xaml.cs gathers them back into one window.
        key.SetValue("MultiSelectModel", "Player");

        using var command = key.CreateSubKey("command")
                            ?? throw new InvalidOperationException("Could not write to the registry.");
        command.SetValue(null, $"\"{exe}\" \"%1\"");
    }

    public static void Unregister()
    {
        Registry.CurrentUser.DeleteSubKeyTree(KeyPath, throwOnMissingSubKey: false);
    }
}
