using Resonance;
using Resonance.Adapters.Bluetooth;
using Resonance.Bluetooth.BuilderExtension;
using System;
using System.Reflection;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtension      
{
    /// <summary> 
    /// Sets the transporter adapter to <see cref="BluetoothAdapter"/>.   
    /// </summary>
    /// <param name="adapterBuilder">The adapter builder.</param>
    /// <returns></returns>
    public static BluetoothAdapterBuilder WithBluetoothAdapter(this IAdapterBuilder adapterBuilder)
    {
        return new BluetoothAdapterBuilder(adapterBuilder as ResonanceTransporterBuilder);
    }
}

namespace Resonance.Bluetooth.BuilderExtension    
{
    public class BluetoothAdapterBuilderBase
    {
        protected ResonanceTransporterBuilder _builder;

        internal BluetoothAdapterBuilderBase(ResonanceTransporterBuilder builder)
        {
            _builder = builder;
        }
    }

    public class BluetoothAdapterBuilder : BluetoothAdapterBuilderBase  
    {
        internal BluetoothAdapterBuilder(ResonanceTransporterBuilder builder) : base(builder)
        {
        }

        /// <summary>
        /// Sets the remote Bluetooth device address.
        /// </summary>
        /// <param name="address">The remote device address.</param>
        public ITranscodingBuilder WithAddress(String address) 
        {
            IResonanceTransporter transporter = _builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_builder) as IResonanceTransporter;
            transporter.Adapter = new BluetoothAdapter(address);
            return _builder;
        }

        /// <summary>
        /// Sets the remote Bluetooth device.
        /// </summary>
        /// <param name="device">The remote device.</param>
        public ITranscodingBuilder WithDevice(BluetoothDevice device)
        {
            IResonanceTransporter transporter = _builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_builder) as IResonanceTransporter;
            transporter.Adapter = new BluetoothAdapter(device);
            return _builder;
        }
    }
}
