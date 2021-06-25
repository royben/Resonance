using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.RPC
{
    /// <summary>
    /// Represents an RPC service member configuration attribute.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public class ResonanceRpcAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the message queue priority.
        /// </summary>
        public QueuePriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the message logging mode.
        /// </summary>
        public ResonanceMessageLoggingMode LoggingMode { get; set; }

        /// <summary>
        /// Gets or sets the timeout in seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to require reception confirmation from the remote peer.
        /// </summary>
        public bool RequireACK { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceRpcAttribute"/> class.
        /// </summary>
        public ResonanceRpcAttribute()
        {
            RequireACK = true;
        }

        /// <summary>
        /// Converts this instance to request configuration.
        /// </summary>
        public ResonanceRequestConfig ToRequestConfig()
        {
            return new ResonanceRequestConfig()
            {
                Timeout = Timeout > 0 ? (TimeSpan?)TimeSpan.FromSeconds(Timeout) : null,
                Priority = Priority,
                LoggingMode = LoggingMode
            };
        }

        /// <summary>
        /// Converts this instance to continuous request configuration.
        /// </summary>
        public ResonanceContinuousRequestConfig ToContinuousRequestConfig()
        {
            return new ResonanceContinuousRequestConfig()
            {
                Timeout = Timeout > 0 ? (TimeSpan?)TimeSpan.FromSeconds(Timeout) : null,
                Priority = Priority,
                LoggingMode = LoggingMode
            };
        }

        /// <summary>
        /// Converts this instance to response configuration.
        /// </summary>
        public ResonanceResponseConfig ToResponseConfig()
        {
            return new ResonanceResponseConfig()
            {
                Priority = Priority,
                LoggingMode = LoggingMode
            };
        }

        /// <summary>
        /// Converts this instance to message configuration.
        /// </summary>
        public ResonanceMessageConfig ToMessageConfig()
        {
            return new ResonanceMessageConfig()
            {
                LoggingMode = LoggingMode,
                Priority = Priority,
                Timeout = Timeout > 0 ? (TimeSpan?)TimeSpan.FromSeconds(Timeout) : null,
                RequireACK = RequireACK,
            };
        }
    }
}
