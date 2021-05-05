using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a transcoding header message type.
    /// </summary>
    public enum ResonanceTranscodingInformationType
    {
        /// <summary>
        /// Request message that expects a response.
        /// </summary>
        Request,
        /// <summary>
        /// Response message.
        /// </summary>
        Response,
        /// <summary>
        /// Continuous request message
        /// </summary>
        ContinuousRequest,
        /// <summary>
        /// Standard message with no response.
        /// </summary>
        Message,
        /// <summary>
        /// Standard message that requires acknowledgment.
        /// </summary>
        MessageSync,
        /// <summary>
        /// Acknowledgment of a <see cref="MessageSync"/>.
        /// </summary>
        MessageSyncACK,
        /// <summary>
        /// KeepAlive request.
        /// </summary>
        KeepAliveRequest,
        /// <summary>
        /// KeepAlive response.
        /// </summary>
        KeepAliveResponse,
        /// <summary>
        /// Disconnection message.
        /// </summary>
        Disconnect,
    }
}
