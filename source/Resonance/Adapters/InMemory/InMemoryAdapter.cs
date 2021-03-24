using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.InMemory
{
    /// <summary>
    /// Represents a Resonance In-Memory adapter for reading/writing data to and from another In-Memory adapter with the same address.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class InMemoryAdapter : ResonanceAdapter
    {
        private static int _counter;

        /// <summary>
        /// Gets the adapter address.
        /// </summary>
        public String Address { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryAdapter"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        public InMemoryAdapter(String address)
        {
            Address = address;
            _counter++;
        }

        /// <summary>
        /// Emulates data available event on this adapter.
        /// </summary>
        /// <param name="data">The data.</param>
        internal void EmulateDataAvailable(byte[] data)
        {
            OnDataAvailable(data);
        }

        /// <summary>
        /// Writes the specified encoded data.
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
        /// Connects this component.
        /// </summary>
        /// <returns></returns>
        public override Task Connect()
        {
            InMemoryAdaptersManager.RegisterAdapter(this);
            State = ResonanceComponentState.Connected;
            return Task.FromResult(new object());
        }

        /// <summary>
        /// Disconnects this component.
        /// </summary>
        /// <returns></returns>
        public override Task Disconnect()
        {
            InMemoryAdaptersManager.UnregisterAdapter(this);
            State = ResonanceComponentState.Disconnected;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"In-Memory Adapter {_counter} ({Address})";
        }
    }
}
