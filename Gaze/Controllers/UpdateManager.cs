using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;

namespace Gaze.Controllers;

/// <summary>
/// GitHub-based auto-updater. Checks the latest release on your GitHub repository,
/// compares with the current version, and downloads + installs if newer.
/// 
/// Setup: Create a release on GitHub with a .zip asset containing the published app.
/// Tag the release with a semver like "v1.0.1".
/// </summary>
public class UpdateManager
{
    // ── CONFIGURE THIS ──────────────────────────────────────────────
    // Replace with your actual GitHub repo (e.g., "atharvabari/Gaze")
    private const string GitHubOwner = "AtharvaBari";
    private const string GitHubRepo = "Gaze-Windows";
    // ────────────────────────────────────────────────────────────────

    private static readonly HttpClient Http = new()
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "Gaze-Updater" },
            { "Accept", "application/vnd.github.v3+json" }
        }
    };

    public string CurrentVersion { get; } = "1.0.1";

    // State properties
    public bool IsChecking { get; private set; }
    public bool IsDownloading { get; private set; }
    public double DownloadProgress { get; private set; }
    public string? LatestVersion { get; private set; }
    public string? ReleaseNotes { get; private set; }
    public string? DownloadUrl { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool UpdateAvailable => LatestVersion != null && IsNewerVersion(LatestVersion, CurrentVersion);

    public event Action? StateChanged;

    /// <summary>
    /// Checks the GitHub releases API for a newer version.
    /// </summary>
    public async Task CheckForUpdatesAsync()
    {
        IsChecking = true;
        ErrorMessage = null;
        StateChanged?.Invoke();

        try
        {
            string url = $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";
            var response = await Http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = $"GitHub returned {response.StatusCode}. Check the repo name.";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Parse tag name (e.g., "v1.0.1" → "1.0.1")
            string tagName = root.GetProperty("tag_name").GetString() ?? "v0.0.0";
            LatestVersion = tagName.TrimStart('v', 'V');
            ReleaseNotes = root.GetProperty("body").GetString() ?? "";

            // Find the .zip asset for Windows
            DownloadUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString() ?? "";
                    if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                        name.Contains("windows", StringComparison.OrdinalIgnoreCase))
                    {
                        DownloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                // Fallback: grab the first .zip if no "windows" match
                if (DownloadUrl == null)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            DownloadUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to check: {ex.Message}";
        }
        finally
        {
            IsChecking = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Downloads the latest release .zip and extracts it next to the current app,
    /// then launches the new version and exits.
    /// </summary>
    public async Task DownloadAndInstallAsync()
    {
        if (DownloadUrl == null) return;

        IsDownloading = true;
        DownloadProgress = 0;
        ErrorMessage = null;
        StateChanged?.Invoke();

        try
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "GazeUpdate");
            string zipPath = Path.Combine(tempDir, "update.zip");
            string extractPath = Path.Combine(tempDir, "extracted");

            // Clean previous download
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            // Download with progress
            using var response = await Http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            long downloadedBytes = 0;

            using (var contentStream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
            {
                var buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        DownloadProgress = (double)downloadedBytes / totalBytes;
                        StateChanged?.Invoke();
                    }
                }
            }

            DownloadProgress = 1.0;
            StateChanged?.Invoke();

            // Extract
            if (Directory.Exists(extractPath))
                Directory.Delete(extractPath, true);
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            // Find the executable in extracted folder
            string? newExe = Directory.GetFiles(extractPath, "Gaze.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (newExe == null)
            {
                ErrorMessage = "Update downloaded but Gaze.exe not found in the archive.";
                StateChanged?.Invoke();
                return;
            }

            // Create a batch script that waits for us to close, copies files, and relaunches
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string batchPath = Path.Combine(tempDir, "update.bat");
            string sourceDir = Path.GetDirectoryName(newExe) ?? extractPath;

            string batchContent = $"""
                @echo off
                echo Updating Gaze...
                timeout /t 2 /nobreak >nul
                xcopy /s /y /q "{sourceDir}\*" "{appDir}"
                start "" "{Path.Combine(appDir, "Gaze.exe")}"
                del "%~f0"
                """;

            File.WriteAllText(batchPath, batchContent);

            // Launch the updater batch and exit
            Process.Start(new ProcessStartInfo
            {
                FileName = batchPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            // Shut down the app
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.Application.Current.Shutdown();
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Update failed: {ex.Message}";
            IsDownloading = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Compares two semver strings. Returns true if 'latest' > 'current'.
    /// </summary>
    private static bool IsNewerVersion(string latest, string current)
    {
        try
        {
            var latestParts = latest.Split('.').Select(int.Parse).ToArray();
            var currentParts = current.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(latestParts.Length, currentParts.Length); i++)
            {
                int l = i < latestParts.Length ? latestParts[i] : 0;
                int c = i < currentParts.Length ? currentParts[i] : 0;
                if (l > c) return true;
                if (l < c) return false;
            }
        }
        catch { }

        return false;
    }
}
