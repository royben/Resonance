﻿using Resonance.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance decoding/decoding information.
    /// </summary>
    public abstract class ResonanceTranscodingInformation
    {
        /// <summary>
        /// Gets or sets the transcoding name.
        /// </summary>
        public String Transcoding { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable compression/decompression.
        /// </summary>
        public bool IsCompressed { get; set; }

        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        public ResonanceTranscodingInformationType Type { get; set; }

        /// <summary>
        /// Gets or sets the RPC call signature if any.
        /// </summary>
        public RPCSignature RPCSignature { get; set; }

        /// <summary>
        /// Gets or sets the request timeout in seconds.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Gets or sets the message token.
        /// </summary>
        public String Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this continuous request has completed. (Only used for continuous request).
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a response message contains an error.
        /// </summary>
        public bool HasError { get; set; }

        /// <summary>
        /// Gets or sets the response error message.
        /// </summary>
        public String ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the message actual encoded message.
        /// </summary>
        public object Message { get; set; }
    }
}
