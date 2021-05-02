using Microsoft.Extensions.Logging;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Resonance.ExtensionMethods;

namespace Resonance.Adapters.Usb
{
    /// <summary>
    /// Represents a USB resonance adapter capable of sending/receiving data over a serial port.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceAdapter" />
    public class UsbAdapter : ResonanceAdapter
    {
        private SerialPort _serialPort; //Serial port instance used to communicate over the serial port.

        #region Properties

        /// <summary>
        /// Gets or sets the serial port baud rate.
        /// </summary>
        public int BaudRate { get; set; }

        /// <summary>
        /// Gets or sets the COM port.
        /// </summary>
        public String Port { get; set; }

        /// <summary>
        /// Gets or sets the maximum expected incoming data.
        /// Anything beyond will be discarded.
        /// Default 50000 (bytes).
        /// </summary>
        public int MaxExpectedSize { get; set; } = 50000;

        /// <summary>
        /// Gets or sets the maximum receive/send.
        /// Default 1024.
        /// </summary>
        public int MaxBufferSize { get; set; } = 1024;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbAdapter"/> class.
        /// </summary>
        public UsbAdapter()
        {
            Port = "COM1";
            BaudRate = (int)BaudRates.BR_9600;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbAdapter"/> class.
        /// </summary>
        /// <param name="port">The COM port name (e.g COM1).</param>
        /// <param name="baudRate">The serial baud rate.</param>
        public UsbAdapter(String port, int baudRate) : this()
        {
            Port = port;
            BaudRate = baudRate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbAdapter"/> class.
        /// </summary>
        /// <param name="port">The COM port name (e.g COM1).</param>
        /// <param name="baudRate">The serial baud rate.</param>
        public UsbAdapter(String port, BaudRates baudRate) : this(port, (int)baudRate)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbAdapter"/> class.
        /// </summary>
        /// <param name="device">The USB serial device.</param>
        /// <param name="baudRate">The serial baud rate.</param>
        public UsbAdapter(UsbDevice device, int baudRate) : this(device.Port, baudRate)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UsbAdapter"/> class.
        /// </summary>
        /// <param name="device">The USB serial device.</param>
        /// <param name="baudRate">The serial baud rate.</param>
        public UsbAdapter(UsbDevice device, BaudRates baudRate) : this(device.Port, baudRate)
        {

        }

        #endregion

        #region Connect / Disconnect / Write

        /// <summary>
        /// Called when the adapter is connecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnConnect()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();

            ThreadFactory.StartNew(() =>
            {
                try
                {
                    if (_serialPort != null)
                    {
                        _serialPort.DataReceived -= OnSerialPortDataReceived;
                    }

                    _serialPort = new SerialPort();
                    _serialPort.BaudRate = BaudRate;
                    _serialPort.PortName = Port;
                    _serialPort.ReadBufferSize = MaxBufferSize;
                    _serialPort.WriteBufferSize = MaxBufferSize;
                    _serialPort.Open();

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    _serialPort.DataReceived += OnSerialPortDataReceived;

                    if (!source.Task.IsCompleted)
                    {
                        source.SetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    if (!source.Task.IsCompleted)
                    {
                        source.SetException(ex);
                    }
                }
            });

            TimeoutTask.StartNew(() =>
            {
                if (!source.Task.IsCompleted)
                {
                    source.SetException(Logger.LogErrorThrow(new IOException($"The serial port seems to be in a froze state. Reinitialize the port and try again.")));
                }

            }, TimeSpan.FromSeconds(5));

            return source.Task;
        }

        /// <summary>
        /// Called when the adapter is disconnecting.
        /// </summary>
        /// <returns></returns>
        protected override Task OnDisconnect()
        {
            TaskCompletionSource<object> source = new TaskCompletionSource<object>();

            ThreadFactory.StartNew(() =>
            {
                try
                {
                    if (_serialPort != null)
                    {
                        _serialPort.DataReceived -= OnSerialPortDataReceived;
                    }

                    _serialPort.Close();
                    _serialPort.Dispose();
                    _serialPort.DataReceived -= OnSerialPortDataReceived;

                    if (!source.Task.IsCompleted)
                    {
                        source.SetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    if (!source.Task.IsCompleted)
                    {
                        source.SetException(ex);
                    }
                }
            });

            TimeoutTask.StartNew(() =>
            {

                if (!source.Task.IsCompleted)
                {
                    Logger.LogCritical(new IOException($"The serial port seems to be in a froze state. Reinitialize the port and try again."));
                    source.SetResult(true);
                }

            }, TimeSpan.FromSeconds(5));

            return source.Task;
        }

        /// <summary>
        /// Called when the adapter is writing.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnWrite(byte[] data)
        {
            data = PrependDataSize(data);
            _serialPort.Write(data, 0, data.Length);
        }

        #endregion

        #region Data Received

        /// <summary>
        /// Called when internal serial port has received data.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SerialDataReceivedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Eof)
                {
                    return;
                }

                if (_serialPort.BytesToRead > 4)
                {
                    byte[] size = new byte[4];
                    _serialPort.Read(size, 0, size.Length);
                    int expectedSize = BitConverter.ToInt32(size, 0);

                    if (expectedSize > MaxExpectedSize || expectedSize < 1)
                    {
                        Logger.LogWarning($"Invalid expected size received on USB adapter ({expectedSize} bytes). Discarding buffers...");

                        byte[] falseData = new byte[_serialPort.BytesToRead];
                        _serialPort.Read(falseData, 0, falseData.Length);

                        try
                        {
                            _serialPort.DiscardInBuffer();
                            _serialPort.DiscardOutBuffer();
                        }
                        catch { }
                        return;
                    }

                    byte[] data = new byte[expectedSize];
                    int read = 0;

                    while (read < expectedSize)
                    {
                        read += _serialPort.Read(data, read, Math.Min(_serialPort.BytesToRead, expectedSize - read));

                        if (State != ResonanceComponentState.Connected)
                        {
                            if (_serialPort != null)
                            {
                                _serialPort.DataReceived -= OnSerialPortDataReceived;
                            }
                            return;
                        }
                    }

                    OnDataAvailable(data);
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error occurred while trying to read from the serial port.");
            }
        }

        #endregion

        #region Finalize

        /// <summary>
        /// Finalizes an instance of the <see cref="UsbAdapter"/> class.
        /// </summary>
        ~UsbAdapter()
        {
            if (_serialPort != null)
            {
                try
                {
                    _serialPort.Close();
                    _serialPort.Dispose();
                }
                catch { }
            }
        }
        #endregion
    }
}
