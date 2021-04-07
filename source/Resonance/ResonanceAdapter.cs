using Resonance.Threading;
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
        private readonly int _componentCounter;
        private long _transferRateTotalBytes;
        private Timer _transferRateTimer;
        private System.Threading.Thread _pushThread;
        private ProducerConsumerQueue<byte[]> _pushQueue;
        private bool _isDisposing;

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
            private set { _totalBytesReceived = value; RaisePropertyChanged(nameof(TotalBytesReceived)); }
        }

        private long _totalBytesSent;
        /// <summary>
        /// Gets the total bytes sent.
        /// </summary>
        public long TotalBytesSent
        {
            get { return _totalBytesSent; }
            private set { _totalBytesSent = value; RaisePropertyChanged(nameof(TotalBytesSent)); }
        }

        private long _transferRate;
        /// <summary>
        /// Gets the current transfer rate.
        /// </summary>
        public long TransferRate
        {
            get { return _transferRate; }
            private set { _transferRate = value; RaisePropertyChanged(nameof(TransferRate)); }
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

        /// <summary>
        /// Gets or sets the adapter data writing mode.
        /// </summary>
        public ResonanceAdapterWriteMode WriteMode { get; set; }

        /// <summary>
        /// Gets the queue write mode interval when <see cref="WriteMode" /> is set to <see cref="ResonanceAdapterWriteMode.Queue" />.
        /// </summary>
        public TimeSpan QueueWriteModeInterval { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceAdapter"/> class.
        /// </summary>
        public ResonanceAdapter()
        {
            _componentCounter = ResonanceComponentCounterManager.Default.GetIncrement(this);
            WriteMode = ResonanceAdapterWriteMode.Direct;
            QueueWriteModeInterval = TimeSpan.FromMilliseconds(10);
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Called when the adapter has failed.
        /// </summary>
        /// <param name="ex">The exception.</param>
        /// <param name="message">Logging message.</param>
        protected virtual void OnFailed(Exception ex, String message)
        {
            FailedStateException = ex;
            Log.Error(ex, $"{this}: {message}");
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
            Log.Debug($"{this}: State changed '{previousState}' => '{newState}'.");

            StateChanged?.Invoke(this, new ResonanceComponentStateChangedEventArgs(previousState, newState));

            if (newState == ResonanceComponentState.Connected)
            {
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
                throw Log.Error(new ObjectDisposedException($"{this}: The adapter is in a '{State}' state."));
            }
        }

        /// <summary>
        /// Prepends the size of the byte array and returns a new byte array.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        protected virtual byte[] PrependDataSize(byte[] data)
        {
            return BitConverter.GetBytes(data.Length).Concat(data).ToArray();
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
            DisposeAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Disposes the adapter resources.
        /// </summary>
        public virtual async Task DisposeAsync()
        {
            if (State != ResonanceComponentState.Disposed && !_isDisposing)
            {
                try
                {
                    Log.Info($"{this}: Disposing...");
                    _isDisposing = true;
                    await Disconnect();
                    Log.Info($"{this}: Disposed.");
                    State = ResonanceComponentState.Disposed;
                }
                catch (Exception ex)
                {
                    throw Log.Error(ex, $"{this}: Error occurred while trying to dispose the adapter.");
                }
                finally
                {
                    _isDisposing = false;
                }
            }
        }

        #endregion

        #region Connect / Disconnect /Write

        /// <summary>
        /// Connects the adapter.
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            ThrowIfDisposed();

            if (State != ResonanceComponentState.Connected)
            {
                Log.Info($"{this}: Connecting...");

                try
                {
                    await OnConnect();

                    Log.Info($"{this}: Connected.");

                    if (WriteMode == ResonanceAdapterWriteMode.Queue)
                    {
                        _pushQueue = new ProducerConsumerQueue<byte[]>();
                        _pushThread = new System.Threading.Thread(PushThreadMethod);
                        _pushThread.IsBackground = true;
                        _pushThread.Name = $"{this} Push Thread";
                        _pushThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"{this}: Adapter connection error occurred.");
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Disconnects the adapter.
        /// </summary>
        /// <returns></returns>
        public async Task Disconnect()
        {
            if (State == ResonanceComponentState.Connected)
            {
                try
                {
                    Log.Info($"{this}: Disconnecting...");

                    await OnDisconnect();

                    Log.Info($"{this}: Disconnected...");

                    if (WriteMode == ResonanceAdapterWriteMode.Queue)
                    {
                        _pushQueue.BlockEnqueue(null); //Will terminate the push thread.
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Adapter disconnection error occurred.");
                    throw ex;
                }
            }
        }

        /// <summary>
        /// Writes the specified encoded data.
        /// </summary>
        /// <param name="data">The data.</param>
        public void Write(byte[] data)
        {
            ThrowIfDisposed();

            try
            {
                if (WriteMode == ResonanceAdapterWriteMode.Direct)
                {
                    OnWrite(data);
                }
                else
                {
                    _pushQueue.BlockEnqueue(data);
                }

                TotalBytesSent += data.Length;
                AppendTransferRateBytes(data.Length);
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error writing to adapter stream.");
                throw;
            }
        }

        #endregion

        #region Abstract Methods

        /// <summary>
        /// Called when the adapter is connecting.
        /// </summary>
        /// <returns></returns>
        protected abstract Task OnConnect();

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
        protected abstract Task OnDisconnect();

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected abstract void OnWrite(byte[] data);

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
            return this != null ? $"{this.GetType().Name} {_componentCounter}" : "No Adapter";
        }

        #endregion

        #region Push Queue Thread

        private void PushThreadMethod()
        {
            try
            {
                while (State == ResonanceComponentState.Connected)
                {
                    List<byte[]> dataCollection = new List<byte[]>();

                    var data = _pushQueue.BlockDequeue();
                    if (data == null) return;

                    var first = true;

                    while (_pushQueue.Count > 0 || first)
                    {
                        if (!first)
                        {
                            data = _pushQueue.BlockDequeue();
                        }
                        else
                        {
                            first = false;
                        }

                        dataCollection.Add(data);
                    }

                    if (dataCollection.Count > 0)
                    {
                        byte[] allData = dataCollection.SelectMany(a => a).ToArray();

                        try
                        {
                            OnWrite(allData);
                        }
                        catch
                        {
                            return;
                        }
                    }

                    System.Threading.Thread.Sleep(QueueWriteModeInterval);
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                //Ignore
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Unexpected occurred on adapter write queue.");
            }
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
