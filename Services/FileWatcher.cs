using System;
using System.IO;

namespace HGEngineGUI.Services
{
    public sealed class FileWatcher : IDisposable
    {
        private FileSystemWatcher? _watcher;
        public event EventHandler<string>? FileChanged;

        public void Start(string directory, string filter)
        {
            Stop();
            if (!Directory.Exists(directory)) return;
            _watcher = new FileSystemWatcher(directory, filter)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Deleted += OnChanged;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            FileChanged?.Invoke(this, e.FullPath);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            FileChanged?.Invoke(this, e.FullPath);
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}


