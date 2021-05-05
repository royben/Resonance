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
        /// <summary>
        /// Gets the adapter address.
        /// </summary>
        public String Address { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether to perform the Write method asynchronously.
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryAdapter"/> class.
        /// </summary>
        /// <param name="address">The address.</param>
        public InMemoryAdapter(String address) : base()
        {
            Address = address;
            InMemoryAdaptersManager.RegisterAdapter(this);
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
            return Task.FromResult(new object());
        }

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnDisconnect()
        {
            InMemoryAdaptersManager.UnregisterAdapter(this);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnWrite(byte[] data)
        {
            if (IsAsync)
            {
                Task.Factory.StartNew(() => { InMemoryAdaptersManager.Write(this, data); });
            }
            else
            {
                InMemoryAdaptersManager.Write(this, data);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{base.ToString()} ({Address})";
        }

        /// <summary>
        /// Detach all registered In-Memory adapters.
        /// </summary>
        public static void DisposeAll()
        {
            InMemoryAdaptersManager.Reset();
        }
    }
}
