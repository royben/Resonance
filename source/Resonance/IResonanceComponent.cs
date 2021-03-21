using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a transport component.
    /// </summary>
    public interface IResonanceComponent
    {
        /// <summary>
        /// Gets or sets the name of the transport component.
        /// </summary>
        String ComponentName { get; set; }
    }
}
