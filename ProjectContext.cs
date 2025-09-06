using System;
using System.IO;
using System.Threading.Tasks;
using HGEngineGUI.Services;

namespace HGEngineGUI
{
    public static class ProjectContext
    {
        public static string? RootPath { get; private set; }
        public static int SpeciesCount { get; internal set; } = -1;
        private static FileWatcher? _watcher;
        public static event EventHandler<string>? ExternalFileChanged;

        public static void SetRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path");
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(path);
            RootPath = path;

            // Start global watcher on armips/data
            try
            {
                _watcher?.Dispose();
                var dataDir = Path.Combine(path, "armips", "data");
                _watcher = new FileWatcher();
                _watcher.FileChanged += (s, fullPath) => ExternalFileChanged?.Invoke(null, fullPath);
                _watcher.Start(dataDir, "*");
            }
            catch { }
        }

        // Utility: open a folder in Explorer relative to project root
        public static async Task OpenFolderAsync(string relativePath)
        {
            try
            {
                if (RootPath == null) return;
                var path = Path.Combine(RootPath, (relativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar));
                if (Directory.Exists(path))
                {
                    var uri = new Uri($"file:///{path.Replace("\\", "/")}");
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
            catch { }
        }
    }
}


