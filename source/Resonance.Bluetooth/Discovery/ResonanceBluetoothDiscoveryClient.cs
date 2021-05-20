using Resonance.Adapters.Bluetooth;
using Resonance.Discovery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Bluetooth.Discovery
{
    /// <summary>
    /// Represents a nearby Bluetooth device discovery client capable of notifying about new device detection.
    /// </summary>
    /// <seealso cref="Resonance.Discovery.IResonanceDiscoveryClient{Resonance.Adapters.Bluetooth.BluetoothDevice, Resonance.Bluetooth.Discovery.ResonanceBluetoothDiscoveredDevice}" />
    public class ResonanceBluetoothDiscoveryClient : IResonanceDiscoveryClient<BluetoothDevice, ResonanceBluetoothDiscoveredDevice>
    {
        private Thread _thread;
        private List<ResonanceBluetoothDiscoveredDevice> _discoveredServices;

        /// <summary>
        /// Gets a value indicating whether this client has started.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Occurs when a device has been discovered.
        /// </summary>
        public event EventHandler<ResonanceDiscoveredServiceEventArgs<ResonanceBluetoothDiscoveredDevice, BluetoothDevice>> ServiceDiscovered;

        /// <summary>
        /// Occurs when a discovered device has been lost.
        /// </summary>
        public event EventHandler<ResonanceDiscoveredServiceEventArgs<ResonanceBluetoothDiscoveredDevice, BluetoothDevice>> ServiceLost;

        /// <summary>
        /// Asynchronous method for collecting discovered devices within the given duration.
        /// </summary>
        /// <param name="maxDuration">The maximum duration to perform the scan.</param>
        /// <param name="maxServices">Drop the scanning after the maximum services discovered.</param>
        /// <returns></returns>
        public List<ResonanceBluetoothDiscoveredDevice> Discover(TimeSpan maxDuration, int? maxServices = null)
        {
            return DiscoverAsync(maxDuration, maxServices).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronous method for collecting discovered devices within the given duration.
        /// </summary>
        /// <param name="maxDuration">The maximum duration to perform the scan.</param>
        /// <param name="maxServices">Drop the scanning after the maximum services discovered.</param>
        /// <returns></returns>
        public async Task<List<ResonanceBluetoothDiscoveredDevice>> DiscoverAsync(TimeSpan maxDuration, int? maxServices = null)
        {
            await StartAsync();

            return await Task.Factory.StartNew<List<ResonanceBluetoothDiscoveredDevice>>(() =>
            {
                DateTime startTime = DateTime.Now;

                while (DateTime.Now < startTime + maxDuration)
                {
                    Thread.Sleep(10);

                    if (maxServices != null && _discoveredServices.Count >= maxServices.Value)
                    {
                        break;
                    }
                }

                if (maxServices != null)
                {
                    return _discoveredServices.Take(maxServices.Value).ToList();
                }
                else
                {
                    return _discoveredServices.ToList();
                }
            });
        }

        /// <summary>
        /// Start discovering.
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                _discoveredServices = new List<ResonanceBluetoothDiscoveredDevice>();
                _thread = new Thread(DiscoverThread);
                _thread.IsBackground = true;
                _thread.Start();
            }
        }

        /// <summary>
        /// Start discovering.
        /// </summary>
        /// <returns></returns>
        public Task StartAsync()
        {
            return Task.Factory.StartNew(Start);
        }

        /// <summary>
        /// Start discovering.
        /// </summary>
        public void Stop()
        {
            IsStarted = false;
        }

        /// <summary>
        /// Stop discovering.
        /// </summary>
        /// <returns></returns>
        public Task StopAsync()
        {
            return Task.Factory.StartNew(Stop);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Disposes component resources asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            return Task.Factory.StartNew(Dispose);
        }

        private void DiscoverThread()
        {
            while (IsStarted)
            {
                var devices = BluetoothAdapter.DiscoverDevices();

                if (!IsStarted) return;

                //Remove lost devices.
                foreach (var device in _discoveredServices.ToList())
                {
                    if (!devices.Exists(x => x.Address == device.DiscoveryInfo.Address && x.Name == device.DiscoveryInfo.Name))
                    {
                        _discoveredServices.Remove(device);
                        ServiceLost?.Invoke(this, new ResonanceDiscoveredServiceEventArgs<ResonanceBluetoothDiscoveredDevice, BluetoothDevice>(device));
                    }
                }

                //Add new devices.
                foreach (var device in devices)
                {
                    if (!_discoveredServices.Exists(x => x.DiscoveryInfo.Address == device.Address && x.DiscoveryInfo.Name == device.Name))
                    {
                        var newDevice = new ResonanceBluetoothDiscoveredDevice(device);
                        _discoveredServices.Add(newDevice);
                        ServiceDiscovered?.Invoke(this, new ResonanceDiscoveredServiceEventArgs<ResonanceBluetoothDiscoveredDevice, BluetoothDevice>(newDevice));
                    }
                }
            }
        }
    }
}
