using Resonance.Adapters.Bluetooth;
using Resonance.Discovery;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Bluetooth.Discovery
{
    /// <summary>
    /// Represents a discovered nearby Bluetooth device.
    /// </summary>
    /// <seealso cref="Resonance.Discovery.IResonanceDiscoveredService{Resonance.Adapters.Bluetooth.BluetoothDevice}" />
    public class ResonanceBluetoothDiscoveredDevice : IResonanceDiscoveredService<BluetoothDevice>
    {
        /// <summary>
        /// Gets the discovered device information.
        /// </summary>
        public BluetoothDevice DiscoveryInfo { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceBluetoothDiscoveredDevice"/> class.
        /// </summary>
        /// <param name="device">The Bluetooth device.</param>
        public ResonanceBluetoothDiscoveredDevice(BluetoothDevice device)
        {
            DiscoveryInfo = device;
        }
    }
}
