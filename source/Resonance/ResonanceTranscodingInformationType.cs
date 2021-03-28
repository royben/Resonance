using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a <see cref="ResonanceTranscodingInformation"/> type.
    /// </summary>
    public enum ResonanceTranscodingInformationType
    {
        Request,
        Response,
        ContinuousRequest,
        KeepAliveRequest,
        KeepAliveResponse,
        Disconnect,
    }
}
