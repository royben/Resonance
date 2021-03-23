using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.InMemory
{
    public class InMemoryAdapter : ResonanceAdapter
    {
        private static int _counter;

        public String Address { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryTransportAdapter"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        public InMemoryAdapter(String address)
        {
            Address = address;
            _counter++;
        }

        /// <summary>
        /// Emulates in coming data.
        /// </summary>
        /// <param name="data">The data.</param>
        internal void EmulateDataAvailable(byte[] data)
        {
            OnDataAvailable(data);
        }

        /// <summary>
        /// Writes the specified data to the stream.
        /// </summary>
        /// <param name="data">The data.</param>
        public override void Write(byte[] data)
        {
            ThrowIfDisposed();

            try
            {
                TotalBytesSent += data.Length;
                _totalBytes += data.Length;
                InMemoryAdaptersManager.Write(this, data);
            }
            catch (Exception ex)
            {
                OnFailed(LogManager.Log(ex));
                throw ex;
            }
        }

        /// <summary>
        /// Connects the transport component.
        /// </summary>
        /// <returns></returns>
        public override Task Connect()
        {
            InMemoryAdaptersManager.RegisterAdapter(this);
            State = ResonanceComponentState.Connected;
            return Task.FromResult(new object());
        }

        /// <summary>
        /// Disconnects the transport component.
        /// </summary>
        /// <returns></returns>
        public override Task Disconnect()
        {
            InMemoryAdaptersManager.UnregisterAdapter(this);
            State = ResonanceComponentState.Disconnected;
            return Task.FromResult(true);
        }

        public override string ToString()
        {
            return $"In-Memory Adapter {_counter} ({Address})";
        }
    }
}
