using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Orbshacker;

public sealed class GameFaker : IDisposable
{
    private readonly List<string> _createdFiles = [];
    private readonly List<string> _createdDirectories = [];
    private readonly List<Process> _processes = [];
    public string ChosenPath => AppConfig.Settings.ChosenFolder;

    public void RegisterCreatedFile(string path) => _createdFiles.Add(path);
    public void RegisterParentDirectories(string path, string limitDirectory)
    {
        for (var parent = Directory.GetParent(path); parent is not null && !Path.GetFullPath(parent.FullName).Equals(Path.GetFullPath(limitDirectory), StringComparison.OrdinalIgnoreCase); parent = parent.Parent)
            if (!_createdDirectories.Contains(parent.FullName, StringComparer.OrdinalIgnoreCase)) _createdDirectories.Add(parent.FullName);
    }

    public string CopyExecutableTo(string targetPath, string? manifestPath = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(Environment.ProcessPath!, targetPath, true);
        CopyFrameworkDependentFiles(targetPath);
        var settings = AppConfig.Settings with { SteamManifestPath = manifestPath };
        var marker = Encoding.UTF8.GetBytes(AppConfig.BakedMarker);
        var json = JsonSerializer.SerializeToUtf8Bytes(settings, AppConfig.JsonOptions);
        using (var output = new FileStream(targetPath, FileMode.Append, FileAccess.Write, FileShare.Read))
        { output.Write(marker); output.Write(json); output.Write(marker); }
        RegisterCreatedFile(targetPath);
        var normalized = targetPath.Replace('\\', '/');
        var markerIndex = normalized.IndexOf("steamapps/common", StringComparison.OrdinalIgnoreCase);
        RegisterParentDirectories(targetPath, markerIndex >= 0 ? normalized[..(markerIndex + "steamapps/common".Length)] : ChosenPath);
        return targetPath;
    }

    private void CopyFrameworkDependentFiles(string targetPath)
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        if (string.IsNullOrEmpty(assemblyPath)) return; // Published single-file executable.
        var sourceDirectory = Path.GetDirectoryName(assemblyPath)!; var destinationDirectory = Path.GetDirectoryName(targetPath)!;
        var baseName = Path.GetFileNameWithoutExtension(assemblyPath);
        foreach (var source in new[] { assemblyPath, Path.Combine(sourceDirectory, baseName + ".deps.json"), Path.Combine(sourceDirectory, baseName + ".runtimeconfig.json") })
        {
            if (!File.Exists(source)) continue;
            var destination = Path.Combine(destinationDirectory, Path.GetFileName(source)); File.Copy(source, destination, true); RegisterCreatedFile(destination);
        }
    }

    public string CreateFakeGame(string executableName)
    {
        var name = PathUtilities.SanitizeRelativePath(executableName);
        if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
        return CopyExecutableTo(Path.Combine(ChosenPath, AppConfig.Settings.FakeExeDir, name.Replace('/', Path.DirectorySeparatorChar)));
    }

    public bool LaunchExecutable(string path)
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(path)! });
            if (process is not null) _processes.Add(process);
            Ui.Success("Process launched in background. Discord should detect it in a few seconds."); return true;
        }
        catch (Exception error) { Ui.Warning($"Failed to auto-launch: {error.Message}. Run manually: {path}"); return false; }
    }

    public void Cleanup()
    {
        if (!AppConfig.Settings.AutoDelete) return;
        Ui.Info("AUTO_DELETE enabled. Cleaning up processes and files...");
        foreach (var process in _processes) try { if (!process.HasExited) process.Kill(true); } catch { }
        foreach (var file in _createdFiles.AsEnumerable().Reverse())
            for (var attempt = 0; attempt < 5; attempt++) try { if (File.Exists(file)) File.Delete(file); break; } catch { Thread.Sleep(200); }
        foreach (var directory in _createdDirectories.OrderByDescending(x => x.Length))
            try { if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory); } catch { }
        Ui.Success("Cleanup complete.");
    }
    public void Dispose() { Cleanup(); GC.SuppressFinalize(this); }
}
