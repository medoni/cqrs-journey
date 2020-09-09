using System;
using System.IO;
using Infrastructure.LocalFileSystem.Messaging;
using Infrastructure.LocalFileSystem.Messaging.Implementation;
using Xunit;

namespace Infrastructure.LocalFileSystemTests.Messaging {
    
    public class given_sender : IDisposable {
    
        private readonly string _messageDirectory;
        private readonly MessageSender sender;

        public given_sender() {
            _messageDirectory = MessagingDbInitializer.GetOrCreateDB(null, "Messages");
            this.sender = new MessageSender(_messageDirectory);
        }

        void IDisposable.Dispose()
        {
            Directory.Delete(_messageDirectory, true);
        }
        
        [Fact]
        public void when_sending_string_message_then_saves_message()
        {
            var messageBody = "Message-" + Guid.NewGuid().ToString();
            var message = new Message(messageBody);

            this.sender.Send(message);
        }
    }
}