using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.Bluetooth
{
    /// <summary>
    /// Represents a remote Bluetooth device.
    /// </summary>
    public class BluetoothDevice
    {
        private BluetoothDeviceInfo _deviceInfo;
        private String _name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothDevice"/> class.
        /// </summary>
        /// <param name="deviceInfo">The device information.</param>
        internal BluetoothDevice(BluetoothDeviceInfo deviceInfo)
        {
            _deviceInfo = deviceInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BluetoothDevice"/> class.
        /// </summary>
        /// <param name="name">The remote device name.</param>
        internal BluetoothDevice(String name)
        {
            _name = name;
        }

        /// <summary>
        /// Forces the system to refresh the device information.
        /// </summary>
        public void Refresh()
        {
            _deviceInfo?.Refresh();
        }

        /// <summary>
        /// Forces the system to refresh the device information.
        /// </summary>
        public Task RefreshAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                _deviceInfo?.Refresh();
            });
        }

        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        public String Address
        {
            get
            {
                if (_deviceInfo == null) return _name;
                return _deviceInfo.DeviceAddress.ToString();
            }
        }

        /// <summary>
        /// Gets the name of a device.
        /// </summary>
        public string Name
        {
            get
            {
                if (_deviceInfo == null) return _name;
                return _deviceInfo.DeviceName;
            }
        }

        /// <summary>
        /// Returns a list of services which are already installed for use on the calling machine.
        /// </summary>
        public IReadOnlyCollection<String> InstalledServices
        {
            get
            {
                List<String> list = new List<string>();

                if (_deviceInfo != null)
                {
                    foreach (var item in _deviceInfo.InstalledServices)
                    {
                        list.Add(item.ToString());
                    }
                }

                return new ReadOnlyCollection<String>(list);
            }
        }

        /// <summary>
        /// Specifies whether the device is connected.
        /// </summary>
        public bool Connected
        {
            get
            {
                if (_deviceInfo == null) return true;
                return _deviceInfo.Connected;
            }
        }

        /// <summary>
        /// Specifies whether the device is authenticated, paired, or bonded. All authenticated devices are remembered.
        /// </summary>
        public bool Authenticated
        {
            get
            {
                if (_deviceInfo == null) return false;
                return _deviceInfo.Authenticated;
            }
        }

        /// <summary>
        /// Compares two BluetoothDeviceInfo instances for equality.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(BluetoothDevice other)
        {
            if (other is null)
                return false;

            return Address == other.Address;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is BluetoothDevice)
            {
                return Equals((BluetoothDevice)obj);
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = -768522134;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        /// <summary>
        /// Gets a string representation of this device.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (_deviceInfo == null) return _name;
            return _deviceInfo.DeviceName;
        }

        /// <summary>
        /// Starts a pairing request for this device.
        /// </summary>
        /// <param name="pin">Optional pin code.</param>
        /// <returns>True, if successful.</returns>
        public bool PairRequest(String pin = null)
        {
            if (_deviceInfo == null) return false;
            return BluetoothSecurity.PairRequest(_deviceInfo.DeviceAddress, pin);
        }

        /// <summary>
        /// Starts a pairing request for this device.
        /// </summary>
        /// <param name="pin">Optional pin code.</param>
        /// <returns>True, if successful.</returns>
        public Task<bool> PairRequestAsync(String pin = null)
        {
            return Task.Factory.StartNew<bool>(() => PairRequest(pin));
        }

        /// <summary>
        /// Removes this device from the local system registry.
        /// </summary>
        public bool RemoveDevice()
        {
            if (_deviceInfo == null) return false;
            return BluetoothSecurity.RemoveDevice(_deviceInfo.DeviceAddress);
        }

        /// <summary>
        /// Removes this device from the local system registry.
        /// </summary>
        public Task<bool> RemoveDeviceAsync()
        {
            return Task.Factory.StartNew<bool>(() => RemoveDevice());
        }
    }
}
