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
        /// Called when the adapter is connecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnConnect()
        {
            InMemoryAdaptersManager.RegisterAdapter(this);
            State = ResonanceComponentState.Connected;
            return Task.FromResult(new object());
        }

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnDisconnect()
        {
            InMemoryAdaptersManager.UnregisterAdapter(this);
            State = ResonanceComponentState.Disconnected;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnWrite(byte[] data)
        {
            InMemoryAdaptersManager.Write(this, data);
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
