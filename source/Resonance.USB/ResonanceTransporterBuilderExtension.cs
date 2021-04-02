using Resonance;
using Resonance.Adapters.Usb;
using Resonance.USB.BuilderExtension;
using System;
using System.Reflection;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtension      
{
    /// <summary> 
    /// Sets the transporter adapter to <see cref="UsbAdapter"/>.   
    /// </summary>
    /// <param name="adapterBuilder">The adapter builder.</param>
    /// <returns></returns>
    public static UsbAdapterBuilder WithUsbAdapter(this IAdapterBuilder adapterBuilder)
    {
        return new UsbAdapterBuilder(adapterBuilder as ResonanceTransporterBuilder);
    }
}

namespace Resonance.USB.BuilderExtension    
{
    public class UsbAdapterBuilderBase
    {
        protected ResonanceTransporterBuilder _builder;

        internal UsbAdapterBuilderBase(ResonanceTransporterBuilder builder)
        {
            _builder = builder;
        }
    }

    public class UsbAdapterBuilder : UsbAdapterBuilderBase  
    {
        internal UsbAdapterBuilder(ResonanceTransporterBuilder builder) : base(builder)
        {
        }

        /// <summary>
        /// Sets the USB adapter serial port.
        /// </summary>
        /// <param name="port">The port.</param>
        public UsbAdapterBaudRateBuilder WithPort(String port) 
        {
            return new UsbAdapterBaudRateBuilder(_builder, port);
        }
    }

    public class UsbAdapterBaudRateBuilder : UsbAdapterBuilderBase
    {
        private String _com;

        internal UsbAdapterBaudRateBuilder(ResonanceTransporterBuilder builder, String com) : base(builder)
        {
            _com = com;
        }

        /// <summary>
        /// Sets the USB adapter serial baud rate.
        /// </summary>
        /// <param name="rate">The rate.</param>
        public ITranscodingBuilder WithBaudRate(BaudRates rate)
        {
            return WithBaudRate((int)rate);
        }

        /// <summary>
        /// Sets the USB adapter serial baud rate.
        /// </summary>
        /// <param name="rate">The rate.</param>
        public ITranscodingBuilder WithBaudRate(int rate)
        {
            IResonanceTransporter transporter = _builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_builder) as IResonanceTransporter;
            transporter.Adapter = new UsbAdapter(_com, rate);
            return _builder;
        }
    }
}
