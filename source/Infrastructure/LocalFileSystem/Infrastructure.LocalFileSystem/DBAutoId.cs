using System;
using System.Collections.Generic;
using System.IO;

namespace Infrastructure.LocalFileSystem {
    internal static class DBAutoId {
        
        private static readonly Dictionary<string, int> _startIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        internal static string CreateNextId(string directory) {
            lock (_startIds) {
                _startIds.TryGetValue(directory, out var startId);

                var nextIdTpl = CreateNextId(directory, startId);
                var nextId = nextIdTpl.Item1;
                var nextIdPath = nextIdTpl.Item2;
                _startIds[directory] = nextId + 1;
                return nextIdPath;
            }
        }

        private static Tuple<int, string> CreateNextId(string directory, int startId) {
            for (;;) {
                try {
                    string path = Path.Combine(directory, $"{startId}.dat");
                    using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) {
                        return Tuple.Create<int, string>(startId, path);
                    }
                }
                catch (IOException ex) {
                    ++startId;
                }
            }
        }
    }
}