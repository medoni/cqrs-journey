using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Infrastructure.LocalFileSystem.Messaging.Implementation {
    public class MessageReceiver : IMessageReceiver, IDisposable {

        private const int DefaultPollTimeInMS = 100;

        private readonly string _storageDirectory;
        private readonly TimeSpan _pollTime;
        
        private readonly object lockObject = new object();
        private CancellationTokenSource cancellationSource;
        
        public MessageReceiver(string storageDirectory) : this(storageDirectory, TimeSpan.FromMilliseconds(DefaultPollTimeInMS)) {
        }
        
        public MessageReceiver(string storageDirectory, TimeSpan pollTime) {
            this._storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            _pollTime = pollTime;
        }
        
        public event EventHandler<MessageReceivedEventArgs> MessageReceived = (sender, args) => { };
        
        public void Start()
        {
            lock (this.lockObject)
            {
                if (this.cancellationSource == null)
                {
                    this.cancellationSource = new CancellationTokenSource();
                    Task.Factory.StartNew(
                        () => this.ReceiveMessages(this.cancellationSource.Token),
                        this.cancellationSource.Token,
                        TaskCreationOptions.LongRunning,
                        TaskScheduler.Current);
                }
            }
        }

        public void Stop()
        {
            lock (this.lockObject)
            {
                using (this.cancellationSource)
                {
                    if (this.cancellationSource != null)
                    {
                        this.cancellationSource.Cancel();
                        this.cancellationSource = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Stop();
        }

        ~MessageReceiver()
        {
            Dispose(false);
        }
        
        /// <summary>
        /// Receives the messages in an endless loop.
        /// </summary>
        private void ReceiveMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!this.ReceiveMessage())
                {
                    Thread.Sleep(this._pollTime);
                }
            }
        }

        protected bool ReceiveMessage() {
            var filesIter = Directory.EnumerateFiles(_storageDirectory)
                // convert to <path, Message>
                .Select(x => Tuple.Create(x, ReadMessage(x)))
                .Where(x => x.Item2.DeliveryDate is null || x.Item2.DeliveryDate < GetCurrentDate())
                .OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x.Item1)))
            ;

            var topFile = filesIter.FirstOrDefault();
            if (topFile is null) {
                return false;
            }
            
            File.Delete(topFile.Item1);

            try {
                this.MessageReceived(this, new MessageReceivedEventArgs(topFile.Item2));
            }
            catch (Exception) {
                WriteMessage(topFile.Item1, topFile.Item2);
                throw;
            }

            return true;
        }

        private Message ReadMessage(string filePath) {
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Message>(json);
        }

        private void WriteMessage(string filePath, Message message) {
            var json = MessageSender.GetMessageJson(message);
            File.WriteAllText(filePath, json);
        }
        
        protected virtual DateTime GetCurrentDate()
        {
            return DateTime.UtcNow;
        }
    }
}