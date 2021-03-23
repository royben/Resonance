using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    /// <summary>
    /// Represents a Transport message encoder capable of encoding or decoding <see cref="ITangoMessage">Tango Messages</see>.
    /// </summary>
    public interface IResonanceEncoder : IResonanceComponent
    {
        /// <summary>
        /// Encodes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        byte[] Encode(ResonanceEncodingInformation message);
    }
}
