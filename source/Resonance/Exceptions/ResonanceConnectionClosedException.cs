using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Exceptions
{
    /// <summary>
    /// Represents an exception that will be raised by a transporter when the other side as sent a disconnection request.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ResonanceConnectionClosedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceConnectionClosedException"/> class.
        /// </summary>
        public ResonanceConnectionClosedException() : base("The remote peer has closed the connection.")
        {

        }
    }
}
