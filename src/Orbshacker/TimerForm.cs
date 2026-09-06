using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Orbshacker;

public sealed class TimerForm : Form
{
    private int _remaining;
    private readonly Label _time = new();
    private readonly Label _status = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    public TimerForm(int minutes)
    {
        Text = "Timer"; ClientSize = new(400, 250); FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        BackColor = Color.FromArgb(26, 26, 26); StartPosition = FormStartPosition.CenterScreen; _remaining = minutes * 60;
        _time.Dock = DockStyle.Fill; _time.TextAlign = ContentAlignment.MiddleCenter; _time.Font = new("Consolas", 56, FontStyle.Bold); _time.ForeColor = Color.Gainsboro;
        _status.Dock = DockStyle.Bottom; _status.Height = 48; _status.TextAlign = ContentAlignment.MiddleCenter; _status.Font = new("Segoe UI", 10); _status.ForeColor = Color.DimGray;
        Controls.Add(_time); Controls.Add(_status); _timer.Tick += Tick; Tick(null, EventArgs.Empty); _timer.Start();
    }
    private void Tick(object? sender, EventArgs args)
    {
        _time.Text = $"{_remaining / 60:00}:{_remaining % 60:00}"; _status.Text = "Running";
        if (_remaining-- > 0) return;
        _timer.Stop(); _time.Text = "00:00"; _time.ForeColor = Color.FromArgb(255, 107, 107); _status.Text = "Complete"; _status.ForeColor = _time.ForeColor;
        if (AppConfig.Settings.AutoDelete) ScheduleSelfDestruction();
    }
    private void ScheduleSelfDestruction()
    {
        var executable = Environment.ProcessPath!; var directory = Path.GetDirectoryName(executable)!;
        var files = new List<string> { executable };
        if (AppConfig.Settings.SteamManifestPath is { Length: > 0 } manifest) files.Add(manifest);
        var script = Path.Combine(Path.GetTempPath(), $"orbshacker-cleanup-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(script, "@echo off\r\ntimeout /t 2 /nobreak >nul\r\n" + string.Join("\r\n", files.Select(f => $"del /f /q \"{f}\" >nul 2>&1")) + $"\r\nrmdir \"{directory}\" >nul 2>&1\r\ndel /f /q \"%~f0\"\r\n");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{script}\"") { CreateNoWindow = true, UseShellExecute = false, WindowStyle = ProcessWindowStyle.Hidden }); Close();
    }
    public static void HideConsole()
    {
        if (OperatingSystem.IsWindows()) ShowWindow(GetConsoleWindow(), 0);
    }
    [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr window, int command);
}
