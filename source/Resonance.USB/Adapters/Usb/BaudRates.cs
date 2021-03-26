using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Resonance.Adapters.Usb
{
    /// <summary>
    /// Represents a USB serial port baud rate.
    /// </summary>
    public enum BaudRates
    {
        [Description("9600")]
        BR_9600 = 9600,
        [Description("1200")]
        BR_1200 = 1200,
        [Description("2400")]
        BR_2400 = 2400,
        [Description("4800")]
        BR_4800 = 4800,
        [Description("19200")]
        BR_19200 = 19200,
        [Description("38400")]
        BR_38400 = 38400,
        [Description("57600")]
        BR_57600 = 57600,
        [Description("115200")]
        BR_115200 = 115200,
    }
}
