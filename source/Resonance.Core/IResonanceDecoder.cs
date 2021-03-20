using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.Core
{
    public interface IResonanceDecoder
    {
        /// <summary>
        /// Decodes the specified data to an <see cref="ITangoMessage"/>.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        ResonanceTranscodingInformation Decode(byte[] data);
    }
}
