using System;
using System.IO;

namespace Infrastructure.LocalFileSystem {
    public static class DefaultDBInitalizer {
        
        public static string GetOrCreateDB(string storageDirectory) {
            if (string.IsNullOrEmpty(storageDirectory)) {
                storageDirectory = Path.Combine(Environment.CurrentDirectory, "LocalDB");
            }
            if (!Directory.Exists(storageDirectory)) {
                Directory.CreateDirectory(storageDirectory);
            }

            return storageDirectory;
        }
    }
}