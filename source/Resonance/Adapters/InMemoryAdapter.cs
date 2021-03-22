using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters
{
    public class InMemoryAdapter : ResonanceAdapter
    {
        #region Memory Transport Manager

        internal static class MemoryTransportManager
        {
            private static List<InMemoryAdapter> _adapters;

            static MemoryTransportManager()
            {
                _adapters = new List<InMemoryAdapter>();
            }

            internal static void Connect(InMemoryAdapter adapter)
            {
                if (adapter == null)
                {
                    throw new NullReferenceException("Cannot connect null adapter.");
                }

                if (String.IsNullOrWhiteSpace(adapter.Address))
                {
                    throw new InvalidOperationException("Cannot register a memory adapter with null address.");
                }

                if (_adapters.Where(x => x.Address == adapter.Address).Count() > 1)
                {
                    throw new InvalidOperationException("Cannot register more than two memory adapters with the same address.");
                }

                if (_adapters.Contains(adapter))
                {
                    throw new InvalidOperationException("The specified memory adapter is already registered.");
                }

                _adapters.Add(adapter);
            }

            internal static void Disconnect(InMemoryAdapter adapter)
            {
                if (adapter == null)
                {
                    throw new NullReferenceException("Cannot disconnect null adapter.");
                }

                _adapters.Remove(adapter);
            }

            internal static void Write(InMemoryAdapter adapter, byte[] data)
            {
                Task.Factory.StartNew(() =>
                {
                    var other_adapter = _adapters.ToList().SingleOrDefault(x => x.Address == adapter.Address && x != adapter);

                    if (other_adapter != null)
                    {
                        other_adapter.EmulateDataAvailable(data);
                    }
                });
            }
        }

        #endregion

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
                MemoryTransportManager.Write(this, data);
            }
            catch (Exception ex)
            {
                OnFailed(LogManager.Log(ex));
            }
        }

        /// <summary>
        /// Connects the transport component.
        /// </summary>
        /// <returns></returns>
        public override Task Connect()
        {
            MemoryTransportManager.Connect(this);
            State = ResonanceComponentState.Connected;
            return Task.FromResult(new object());
        }

        /// <summary>
        /// Disconnects the transport component.
        /// </summary>
        /// <returns></returns>
        public override Task Disconnect()
        {
            MemoryTransportManager.Disconnect(this);
            State = ResonanceComponentState.Disconnected;
            return Task.FromResult(new object());
        }

        public override string ToString()
        {
            return $"In-Memory Adapter {_counter} ({Address})";
        }
    }
}
