using System;

namespace Infrastructure.LocalFileSystem.Messaging {
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(Message message)
        {
            this.Message = message;
        }

        public Message Message { get; private set; }
    }
}