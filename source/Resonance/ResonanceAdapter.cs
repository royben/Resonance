using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceAdapter"/> base class
    /// </summary>
    public abstract class ResonanceAdapter : ResonanceObject, IResonanceAdapter
    {
        protected long _totalBytes;
        protected static long _component_counter = 1;
        private long _transferRateTotalBytes;
        private Timer _transferRateTimer;
        private object _disposeLock = new object();

        #region Events

        /// <summary>
        /// Occurs when the current state of the component has changed.
        /// </summary>
        public event EventHandler<ResonanceComponentStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Occurs when a new encoded data is available.
        /// </summary>
        public event EventHandler<ResonanceAdapterDataAvailableEventArgs> DataAvailable;

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
        /// Gets the current transfer rate.
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
        /// Gets the last failed state exception of this component.
        /// </summary>
        public Exception FailedStateException { get; private set; }

        private ResonanceComponentState _state;
        /// <summary>
        /// Gets the current state of the component.
        /// </summary>
        public ResonanceComponentState State
        {
            get { return _state; }
            protected set
            {
                if (_state != value)
                {
                    var prev = _state;
                    _state = value;
                    OnStateChanged(prev, _state);
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
        /// <param name="ex">The exception.</param>
        protected virtual void OnFailed(Exception ex)
        {
            FailedStateException = ex;
            LogManager.Log(ex, $"{this}: Adapter failed.");
            Disconnect().Wait();
            State = ResonanceComponentState.Failed;
        }

        /// <summary>
        /// Called when there is new encoded data available.
        /// </summary>
        /// <param name="data">The encoded data.</param>
        protected virtual void OnDataAvailable(byte[] data)
        {
            TotalBytesReceived += data.Length;
            _totalBytes += data.Length;
            AppendTransferRateBytes(data.Length);
            DataAvailable?.Invoke(this, new ResonanceAdapterDataAvailableEventArgs(data));
        }

        /// <summary>
        ///  Called when the adapter state has changed.
        /// </summary>
        /// <param name="previousState">The previous component state.</param>
        /// <param name="newState">The new component state.</param>
        protected virtual void OnStateChanged(ResonanceComponentState previousState, ResonanceComponentState newState)
        {
            StateChanged?.Invoke(this, new ResonanceComponentStateChangedEventArgs(previousState, newState));

            if (newState == ResonanceComponentState.Connected)
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
        /// Throws an exception if this adapter is in a disposed state.
        /// </summary>
        protected virtual void ThrowIfDisposed()
        {
            if (State == ResonanceComponentState.Disposed)
            {
                throw LogManager.Log(new ObjectDisposedException($"{this}: The adapter is in a " + State + " state."));
            }
        }

        /// <summary>
        /// Prepends the size of the byte array and returns a new byte array.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] PrependDataHeaderSize(byte[] data)
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

        /// <summary>
        /// Appends the specified data length to the transfer rate bytes.
        /// </summary>
        /// <param name="dataLength">Length of the data.</param>
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
            lock (_disposeLock)
            {
                if (State != ResonanceComponentState.Disposed)
                {
                    Disconnect().Wait();
                    State = ResonanceComponentState.Disposed;
                }
            }
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Writes the specified encoded data.
        /// </summary>
        /// <param name="data">The data.</param>
        public abstract void Write(byte[] data);

        /// <summary>
        /// Connects this component.
        /// </summary>
        /// <returns></returns>
        public abstract Task Connect();

        /// <summary>
        /// Disconnects this component.
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
