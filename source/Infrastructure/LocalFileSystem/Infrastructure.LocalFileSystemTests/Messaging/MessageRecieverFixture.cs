using System;
using System.IO;
using System.Threading;
using Infrastructure.LocalFileSystem.Messaging;
using Infrastructure.LocalFileSystem.Messaging.Implementation;
using Xunit;

namespace Infrastructure.LocalFileSystemTests.Messaging {
    public class given_sender_and_receiver : IDisposable {
        
        private readonly string _messageDirectory;
        private readonly MessageSender sender;
        private readonly TestableMessageReceiver receiver;

        public given_sender_and_receiver() {
            _messageDirectory = MessagingDbInitializer.GetOrCreateDB(null, "test.Commands");
            this.sender = new MessageSender(_messageDirectory);
            this.receiver = new TestableMessageReceiver(_messageDirectory);
        }

        void IDisposable.Dispose()
        {
            this.receiver.Stop();
            Directory.Delete(_messageDirectory, true);
        }
        
        [Fact]
        public void when_sending_message_then_receives_message()
        {
            Message message = null;

            this.receiver.MessageReceived += (s, e) => { message = e.Message; };

            this.sender.Send(new Message("test message"));

            Assert.True(this.receiver.ReceiveMessage());
            Assert.Equal("test message", message.Body);
            Assert.Null(message.CorrelationId);
            Assert.Null(message.DeliveryDate);
        }

        [Fact]
        public void when_sending_message_with_correlation_id_then_receives_message()
        {
            Message message = null;

            this.receiver.MessageReceived += (s, e) => { message = e.Message; };

            this.sender.Send(new Message("test message", correlationId: "correlation"));

            Assert.True(this.receiver.ReceiveMessage());
            Assert.Equal("test message", message.Body);
            Assert.Equal("correlation", message.CorrelationId);
            Assert.Null(message.DeliveryDate);
        }

        [Fact]
        public void when_successfully_handles_message_then_removes_message()
        {
            this.receiver.MessageReceived += (s, e) => { };

            this.sender.Send(new Message("test message"));

            Assert.True(this.receiver.ReceiveMessage());
            Assert.False(this.receiver.ReceiveMessage());
        }

        [Fact]
        public void when_unsuccessfully_handles_message_then_does_not_remove_message()
        {
            EventHandler<MessageReceivedEventArgs> failureHandler = null;
            failureHandler = (s, e) => { this.receiver.MessageReceived -= failureHandler; throw new ArgumentException(); };

            this.receiver.MessageReceived += failureHandler;

            this.sender.Send(new Message("test message"));

            try
            {
                Assert.True(this.receiver.ReceiveMessage());
                Assert.False(true, "should have thrown");
            }
            catch (ArgumentException)
            { }

            Assert.True(this.receiver.ReceiveMessage());
        }

        [Fact]
        public void when_sending_message_with_delay_then_receives_message_after_delay()
        {
            Message message = null;

            this.receiver.MessageReceived += (s, e) => { message = e.Message; };

            var deliveryDate = DateTime.UtcNow.Add(TimeSpan.FromSeconds(5));
            this.sender.Send(new Message("test message", deliveryDate));

            Assert.False(this.receiver.ReceiveMessage());

            Thread.Sleep(TimeSpan.FromSeconds(6));

            Assert.True(this.receiver.ReceiveMessage());
            Assert.Equal("test message", message.Body);
        }

        [Fact]
        public void when_receiving_message_then_other_receivers_cannot_see_message_but_see_other_messages()
        {
            var secondReceiver = new TestableMessageReceiver(this._messageDirectory);

            this.sender.Send(new Message("message1"));
            this.sender.Send(new Message("message2"));

            var waitEvent = new AutoResetEvent(false);
            string receiver1Message = null;
            string receiver2Message = null;

            this.receiver.MessageReceived += (s, e) =>
            {
                waitEvent.Set();
                receiver1Message = e.Message.Body;
                waitEvent.WaitOne();
            };
            secondReceiver.MessageReceived += (s, e) =>
            {
                receiver2Message = e.Message.Body;
            };

            ThreadPool.QueueUserWorkItem(_ => { this.receiver.ReceiveMessage(); });

            Assert.True(waitEvent.WaitOne(TimeSpan.FromSeconds(10)));
            secondReceiver.ReceiveMessage();
            waitEvent.Set();

            Assert.Equal("message1", receiver1Message);
            Assert.Equal("message2", receiver2Message);
        }

        [Fact]
        public void when_receiving_message_then_can_send_new_message()
        {
            var secondReceiver = new TestableMessageReceiver(_messageDirectory);

            this.sender.Send(new Message("message1"));

            var waitEvent = new AutoResetEvent(false);
            string receiver1Message = null;
            string receiver2Message = null;

            this.receiver.MessageReceived += (s, e) =>
            {
                waitEvent.Set();
                receiver1Message = e.Message.Body;
                waitEvent.WaitOne();
            };
            secondReceiver.MessageReceived += (s, e) =>
            {
                receiver2Message = e.Message.Body;
            };

            ThreadPool.QueueUserWorkItem(_ => { this.receiver.ReceiveMessage(); });

            Assert.True(waitEvent.WaitOne(TimeSpan.FromSeconds(10)));
            this.sender.Send(new Message("message2"));
            secondReceiver.ReceiveMessage();
            waitEvent.Set();

            Assert.Equal("message1", receiver1Message);
            Assert.Equal("message2", receiver2Message);
        }

        public class TestableMessageReceiver : MessageReceiver
        {
            public TestableMessageReceiver(string storageDirectory)
                : base(storageDirectory)
            {
            }

            public new bool ReceiveMessage()
            {
                return base.ReceiveMessage();
            }
        }
        
    }
}