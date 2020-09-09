using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Infrastructure.LocalFileSystem.Messaging.Implementation {
    public class MessageSender : IMessageSender {

        private const Formatting FormattedJson =
#if DEBUG
        Formatting.Indented;
#else
        Formatting.None;
#endif
        
        private readonly string _storageDirectory;

        public MessageSender(string storageDirectory) {
            this._storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
        }
        
        public void Send(Message message) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            InsertMessage(message);
        }

        public void Send(IEnumerable<Message> messages) {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            foreach (var message in messages) {
                InsertMessage(message);
            }
        }

        private void InsertMessage(Message message) {
            var json = JsonConvert.SerializeObject(message, FormattedJson);
            File.WriteAllText(DBAutoId.CreateNextId(_storageDirectory), json);
        }
    }
}