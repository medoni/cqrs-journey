using System;
using System.IO;

namespace Infrastructure.LocalFileSystem.Messaging.Implementation {
    public static class MessagingDbInitializer {

        public static void CreateDB(string storageDirectory, string messageName) {
            GetOrCreateDB(storageDirectory, messageName);
        }

        public static string GetOrCreateDB(string storageDirectory, string messageName) {
            if (string.IsNullOrEmpty(messageName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(messageName));


            if (string.IsNullOrEmpty(storageDirectory)) {
                storageDirectory = DefaultDBInitalizer.GetOrCreateDB(storageDirectory);
            }

            var dbDirectory = Path.Combine(storageDirectory, messageName);

            if (!Directory.Exists(dbDirectory)) {
                Directory.CreateDirectory(dbDirectory);
            }

            return dbDirectory;
        }
        
    }
}