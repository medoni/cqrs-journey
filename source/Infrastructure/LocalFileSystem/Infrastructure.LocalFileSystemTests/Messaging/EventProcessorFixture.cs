using System;
using System.IO;
using Infrastructure.LocalFileSystem.Messaging;
using Infrastructure.LocalFileSystem.Messaging.Handling;
using Infrastructure.Messaging;
using Infrastructure.Messaging.Handling;
using Infrastructure.Serialization;
using Moq;
using Xunit;

namespace Infrastructure.LocalFileSystemTests.Messaging {

    public class given_event_processor {
        private Mock<IMessageReceiver> receiverMock;
        private EventProcessor processor;

        public given_event_processor() {
            System.Diagnostics.Trace.Listeners.Clear();
            this.receiverMock = new Mock<IMessageReceiver>();
            this.processor = new EventProcessor(this.receiverMock.Object, CreateSerializer());
        }

        [Fact]
        public void when_starting_then_starts_receiver() {
            this.processor.Start();

            this.receiverMock.Verify(r => r.Start());
        }

        [Fact]
        public void when_stopping_after_starting_then_stops_receiver() {
            this.processor.Start();
            this.processor.Stop();

            this.receiverMock.Verify(r => r.Stop());
        }

        [Fact]
        public void when_receives_message_then_notifies_registered_handler() {
            var handlerAMock = new Mock<IEventHandler>();
            handlerAMock.As<IEventHandler<Event1>>();
            handlerAMock.As<IEventHandler<Event2>>();

            var handlerBMock = new Mock<IEventHandler>();
            handlerBMock.As<IEventHandler<Event2>>();

            this.processor.Register(handlerAMock.Object);
            this.processor.Register(handlerBMock.Object);

            this.processor.Start();

            var event1 = new Event1 {SourceId = Guid.NewGuid()};
            var event2 = new Event2 {SourceId = Guid.NewGuid()};

            this.receiverMock.Raise(r => r.MessageReceived += null,
                new MessageReceivedEventArgs(new Message(Serialize(event1))));
            this.receiverMock.Raise(r => r.MessageReceived += null,
                new MessageReceivedEventArgs(new Message(Serialize(event2))));

            handlerAMock.As<IEventHandler<Event1>>()
                .Verify(h => h.Handle(It.Is<Event1>(e => e.SourceId == event1.SourceId)));
            handlerAMock.As<IEventHandler<Event2>>()
                .Verify(h => h.Handle(It.Is<Event2>(e => e.SourceId == event2.SourceId)));
            handlerBMock.As<IEventHandler<Event2>>()
                .Verify(h => h.Handle(It.Is<Event2>(e => e.SourceId == event2.SourceId)));
        }

        [Fact]
        public void when_receives_message_then_notifies_generic_handler() {
            var handler = new Mock<IEventHandler>();
            handler.As<IEventHandler<IEvent>>();

            this.processor.Register(handler.Object);

            this.processor.Start();

            var event1 = new Event1 {SourceId = Guid.NewGuid()};
            var event2 = new Event2 {SourceId = Guid.NewGuid()};

            this.receiverMock.Raise(r => r.MessageReceived += null,
                new MessageReceivedEventArgs(new Message(Serialize(event1))));
            this.receiverMock.Raise(r => r.MessageReceived += null,
                new MessageReceivedEventArgs(new Message(Serialize(event2))));

            handler.As<IEventHandler<IEvent>>()
                .Verify(h => h.Handle(It.Is<Event1>(e => e.SourceId == event1.SourceId)));
            handler.As<IEventHandler<IEvent>>()
                .Verify(h => h.Handle(It.Is<Event2>(e => e.SourceId == event2.SourceId)));
        }

        private static string Serialize(object payload) {
            var serializer = CreateSerializer();

            using (var writer = new StringWriter()) {
                serializer.Serialize(writer, payload);
                return writer.ToString();
            }
        }

        private static ITextSerializer CreateSerializer() {
            return new JsonTextSerializer();
        }

        public class Event1 : IEvent {
            public Guid SourceId { get; set; }
        }

        public class Event2 : IEvent {
            public Guid SourceId { get; set; }
        }
    }
}