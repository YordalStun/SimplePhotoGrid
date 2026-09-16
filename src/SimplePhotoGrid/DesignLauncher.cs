using System.Diagnostics;
using System.IO;
using System.Text;

namespace SimplePhotoGrid;

/// <summary>Hands the chosen photos to the companion Design program.</summary>
public static class DesignLauncher
{
    public const string ExecutableName = "PhotoGridDesign.exe";

    /// <summary>Folder the running executable sits in. Not AppContext.BaseDirectory: a
    /// single-file build reports its extraction folder there, not where the .exe lives.</summary>
    private static string? AppFolder =>
        Environment.ProcessPath is { } path ? Path.GetDirectoryName(path) : null;

    public static string? FindExecutable()
    {
        if (AppFolder is not { } folder) return null;
        var candidate = Path.Combine(folder, ExecutableName);
        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>Starts the Design program with these photos. The paths travel in a handoff file
    /// rather than on the command line, which has a length limit a long list would blow past.</summary>
    public static void Launch(IEnumerable<string> photoPaths)
    {
        var executable = FindExecutable()
            ?? throw new FileNotFoundException($"{ExecutableName} was not found.");

        var folder = Path.Combine(Path.GetTempPath(), "SimplePhotoGrid", "handoff");
        Directory.CreateDirectory(folder);

        var handoff = Path.Combine(folder, Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllLines(handoff, photoPaths, new UTF8Encoding(false));

        Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"--photos \"{handoff}\"",
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        });
    }
}
