using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.Usb
{
    /// <summary>
    /// Represents a USB serial device.
    /// </summary>
    public class UsbDevice
    {
        /// <summary>
        /// Gets or sets the port name.
        /// </summary>
        public String Port { get; set; }

        /// <summary>
        /// Gets or sets the device description.
        /// </summary>
        public String Description { get; set; }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Description;
        }

        /// <summary>
        /// Gets the available USB serial devices.
        /// </summary>
        /// <returns></returns>
        public static Task<List<UsbDevice>> GetAvailableDevices()
        {
            return Task.Factory.StartNew<List<UsbDevice>>(() =>
            {
                List<UsbDevice> devices = new List<UsbDevice>();

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
                {
                    var portnames = SerialPort.GetPortNames();

                    List<String> portsInfo = new List<String>();

                    foreach (var item in searcher.Get())
                    {
                        try
                        {
                            ManagementBaseObject mObj = item as ManagementBaseObject;

                            if (mObj != null)
                            {
                                Object caption = mObj["Caption"];
                                if (caption != null)
                                {
                                    portsInfo.Add(caption.ToString());
                                }
                            }
                        }
                        catch { }
                    }

                    foreach (var port in portnames)
                    {
                        var info = portsInfo.FirstOrDefault(x => x.Contains(port));

                        if (info != null)
                        {
                            devices.Add(new UsbDevice()
                            {
                                Port = port,
                                Description = info
                            });
                        }
                    }
                }

                return devices;
            });
        }
    }
}
