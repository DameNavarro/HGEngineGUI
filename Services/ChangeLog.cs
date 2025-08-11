using System;
using System.Collections.Generic;

namespace HGEngineGUI.Services
{
    public static class ChangeLog
    {
        public record Change(string FilePath, DateTime When, int SizeBytes);

        private static readonly List<Change> _changes = new();
        public static IReadOnlyList<Change> Changes => _changes;

        public static void Record(string filePath, int sizeBytes)
        {
            _changes.Insert(0, new Change(filePath, DateTime.Now, sizeBytes));
            if (_changes.Count > 100) _changes.RemoveAt(_changes.Count - 1);
        }
    }
}


