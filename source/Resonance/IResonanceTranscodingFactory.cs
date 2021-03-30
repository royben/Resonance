using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a decoding factory capable of instantiating a decoder based on transcoding name.
    /// </summary>
    public interface IResonanceTranscodingFactory
    {
        /// <summary>
        /// Returns a decoder instance based on the transcoding name (e.g json, bson).
        /// </summary>
        /// <param name="name">The transcoding name.</param>
        IResonanceDecoder GetDecoder(String name);

        /// <summary>
        /// Returns an encoder instance based on the transcoding name (e.g json, bson).
        /// </summary>
        /// <param name="name">The transcoding name.</param>
        IResonanceEncoder GetEncoder(String name);
    }
}
