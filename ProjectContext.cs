using System;
using System.IO;
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
    }
}


