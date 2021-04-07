using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Adapters.NamedPipes
{
    /// <summary>
    /// Represents a Resonance named pipes adapter.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class NamedPipesAdapter : ResonanceAdapter
    {
        private NamedPipeClientStream _client;
        private PipeStream _pipeStream;
        private byte[] _size_buffer;

        #region Properties

        /// <summary>
        /// Gets the name of the server.
        /// </summary>
        public String ServerName { get; private set; }

        /// <summary>
        /// Gets the name of the pipe.
        /// </summary>
        public String PipeName { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipesAdapter"/> class.
        /// </summary>
        /// <param name="serverName">Name of the server.</param>
        /// <param name="pipeName">Name of the pipe.</param>
        public NamedPipesAdapter(String serverName, String pipeName)
        {
            ServerName = serverName;
            PipeName = pipeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipesAdapter"/> class.
        /// </summary>
        /// <param name="pipeName">Name of the pipe.</param>
        public NamedPipesAdapter(String pipeName) : this(".", pipeName)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedPipesAdapter"/> class.
        /// </summary>
        /// <param name="pipeStream">The pipe stream.</param>
        public NamedPipesAdapter(PipeStream pipeStream)
        {
            _pipeStream = pipeStream;
        }

        #endregion

        #region Connect / Disconnect / Write

        protected override Task OnConnect()
        {
            return Task.Factory.StartNew(() =>
            {
                if (State != ResonanceComponentState.Connected)
                {
                    if (_pipeStream == null)
                    {
                        _client = new NamedPipeClientStream(ServerName, PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                        _pipeStream = _client;
                        _client.Connect(5000);
                    }

                    State = ResonanceComponentState.Connected;

                    Task.Factory.StartNew(() =>
                    {
                        WaitForData();
                    }, TaskCreationOptions.LongRunning);
                }
            });
        }

        protected override Task OnDisconnect()
        {
            return Task.Factory.StartNew((Action)(() =>
            {
                if (State == ResonanceComponentState.Connected)
                {
                    State = ResonanceComponentState.Disconnected;
                    _pipeStream.WaitForPipeDrain();
                    _pipeStream.Close();
                }
            }));
        }

        protected override void OnWrite(byte[] data)
        {
            data = PrependDataSize(data);
            _pipeStream?.Write(data, 0, data.Length);
        }

        #endregion

        #region Data Reading

        private void WaitForData()
        {
            try
            {
                if (State == ResonanceComponentState.Connected)
                {
                    _size_buffer = new byte[4];
                    _pipeStream.BeginRead(_size_buffer, 0, _size_buffer.Length, EndReading, null);

                }
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error occurred while trying to read from the pipe stream.");
            }
        }

        private void EndReading(IAsyncResult ar)
        {
            try
            {
                if (State == ResonanceComponentState.Connected)
                {
                    _pipeStream.EndRead(ar);

                    int expectedSize = BitConverter.ToInt32(_size_buffer, 0);

                    if (expectedSize > 0)
                    {
                        byte[] data = new byte[expectedSize];
                        int read = 0;

                        while (read < expectedSize)
                        {
                            read += _pipeStream.Read(data, read, expectedSize - read);

                            if (State != ResonanceComponentState.Connected)
                            {
                                break;
                            }
                        }

                        OnDataAvailable(data);
                    }
                    else
                    {
                        Thread.Sleep(2000);
                    }

                    WaitForData();
                }
            }
            catch (Exception ex)
            {
                OnFailed(ex, "Error occurred while trying to read from the pipe stream.");
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return $"{base.ToString()} ({ServerName}/{PipeName})";
        }

        #endregion
    }
}
