using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a message acknowledgment behavior when using <see cref="IResonanceTransporter.Send(object, ResonanceMessageConfig)"/>
    /// </summary>
    public enum ResonanceMessageAckBehavior
    {
        /// <summary>
        /// Sends the ACK message right after receiving the message.
        /// Any errors while handling the message will not be reported to the sender.
        /// </summary>
        Default,

        /// <summary>
        /// Sends the ACK message after receiving and handling the message.
        /// Any errors will be reported to the sender, given that the sending transporter also defines this setting.
        /// </summary>
        ReportErrors
    }
}
