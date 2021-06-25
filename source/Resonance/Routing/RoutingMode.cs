using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Routing
{
    /// <summary>
    /// Represents a <see cref="TransporterRouter"/> routing mode.
    /// </summary>
    public enum RoutingMode
    {
        /// <summary>
        /// Redirects incoming data from source to target and vise versa.
        /// </summary>
        TwoWay,
        /// <summary>
        /// Redirects incoming data only from the source to the target transporter.
        /// </summary>
        OneWayToTarget,
        /// <summary>
        /// Redirects incoming data only from the target to the source transporter.
        /// </summary>
        OneWayToSource,
    }
}
