using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Adapters.SignalR
{
    /// <summary>
    /// Represents a SignalR remote service shut down event exception.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ServiceDownException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDownException"/> class.
        /// </summary>
        public ServiceDownException() : base("The remote service has been shut down.")
        {

        }
    }
}
