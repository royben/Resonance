using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Resonance
{
    public abstract class ResonanceAdapter : ResonanceObject, IResonanceAdapter
    {
        protected long _totalBytes;
        protected static long _component_counter = 1;
        private long _transferRateTotalBytes;
        private Timer _transferRateTimer;

        #region Events

        /// <summary>
        /// Occurs when component state changes.
        /// </summary>
        public event EventHandler<ResonanceComponentState> StateChanged;

        /// <summary>
        /// Occurs when new data is available.
        /// </summary>
        public event EventHandler<byte[]> DataAvailable;

        #endregion

        #region Properties

        private long _totalBytesReceived;
        /// <summary>
        /// Gets the total bytes received.
        /// </summary>
        public long TotalBytesReceived
        {
            get { return _totalBytesReceived; }
            protected set { _totalBytesReceived = value; RaisePropertyChanged(nameof(TotalBytesReceived)); }
        }

        private long _totalBytesSent;
        /// <summary>
        /// Gets the total bytes sent.
        /// </summary>
        public long TotalBytesSent
        {
            get { return _totalBytesSent; }
            protected set { _totalBytesSent = value; RaisePropertyChanged(nameof(TotalBytesSent)); }
        }

        private long _transferRate;
        /// <summary>
        /// Gets the adapter current transfer rate.
        /// </summary>
        public long TransferRate
        {
            get
            {
                return _transferRate;
            }
            protected set { _transferRate = value; RaisePropertyChanged(nameof(TransferRate)); }
        }

        /// <summary>
        /// Gets the last failed state exception/reason.
        /// </summary>
        public Exception FailedStateException { get; private set; }

        private ResonanceComponentState _state;
        /// <summary>
        /// Gets the component state.
        /// </summary>
        public ResonanceComponentState State
        {
            get { return _state; }
            protected set
            {
                if (_state != value)
                {
                    _state = value;
                    OnStateChanged(_state);
                }
            }
        }

        private bool _enableCompression;
        /// <summary>
        /// Gets or sets a value indicating whether to enable compression/decompression of data.
        /// </summary>
        public bool EnableCompression
        {
            get { return _enableCompression; }
            set { _enableCompression = value; RaisePropertyChangedAuto(); }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called when the adapter has failed.
        /// </summary>
        /// <param name="ex">The ex.</param>
        protected virtual void OnFailed(Exception ex)
        {
            FailedStateException = ex;
            LogManager.Log(ex, $"{this}: Adapter failed.");
            Disconnect().Wait();
            State = ResonanceComponentState.Failed;
        }

        /// <summary>
        /// Called when there is new data available.
        /// </summary>
        /// <param name="data">The data.</param>
        protected virtual void OnDataAvailable(byte[] data)
        {
            TotalBytesReceived += data.Length;
            _totalBytes += data.Length;
            AppendTransferRateBytes(data.Length);
            DataAvailable?.Invoke(this, data);
        }

        /// <summary>
        /// Called when the adapter state has changed.
        /// </summary>
        /// <param name="state">The state.</param>
        protected virtual void OnStateChanged(ResonanceComponentState state)
        {
            StateChanged?.Invoke(this, state);

            if (state == ResonanceComponentState.Connected)
            {
                _totalBytes = 0;
                TransferRate = 0;

                if (_transferRateTimer != null)
                {
                    _transferRateTimer.Stop();
                    _transferRateTimer.Dispose();
                }

                _transferRateTimer = new Timer(1000);
                _transferRateTimer.Elapsed += _transferRateTimer_Elapsed;
                _transferRateTimer.Start();
            }
            else
            {
                if (_transferRateTimer != null)
                {
                    _transferRateTimer.Stop();
                    _transferRateTimer.Dispose();
                }
            }
        }

        /// <summary>
        /// Throws an exception if adapter is in a failed or disposed state.
        /// </summary>
        protected virtual void ThrowIfDisposed()
        {
            if (State == ResonanceComponentState.Disposed)
            {
                throw LogManager.Log(new ObjectDisposedException($"{this}: The adapter is in a " + State + " state."));
            }
        }

        /// <summary>
        /// Applies any additional headers if required.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] PrepandDataHeaderSize(byte[] data)
        {
            byte[] postData = data;

            postData = BitConverter.GetBytes(data.Length).Concat(data).ToArray();

            TotalBytesSent += postData.Length;
            _totalBytes += postData.Length;

            AppendTransferRateBytes(postData.Length);

            return postData;
        }

        #endregion

        #region Private Methods

        protected void AppendTransferRateBytes(long dataLength)
        {
            _transferRateTotalBytes += dataLength;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Disconnect().Wait();
            State = ResonanceComponentState.Disposed; 
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Writes the specified data to the stream.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="immidiate">Writes the data as soon as possible while ignoring any message queuing and batching.</param>
        public abstract void Write(byte[] data);

        /// <summary>
        /// Connects the transport component.
        /// </summary>
        /// <returns></returns>
        public abstract Task Connect();

        /// <summary>
        /// Disconnects the transport component.
        /// </summary>
        /// <returns></returns>
        public abstract Task Disconnect();

        #endregion

        #region Override Methods

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{this.GetType().Name}";
        }

        #endregion

        #region Calculate Transfer Rate

        private void _transferRateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TransferRate = _transferRateTotalBytes;
            _transferRateTotalBytes = 0;
        }

        #endregion
    }
}
