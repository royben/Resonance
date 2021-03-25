using Resonance.Tcp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Resonance.Discovery
{
    /// <summary>
    /// Represents a discovery service that can broadcast information on another service existance.
    /// </summary>
    /// <typeparam name="TDiscoveryInfo">The type of the discovery information.</typeparam>
    /// <typeparam name="TEncoder">The type of the encoder that should be used to encode the packets.</typeparam>
    /// <seealso cref="System.IDisposable" />
    public interface IResonanceDiscoveryService<TDiscoveryInfo, TEncoder> : IDisposable where TDiscoveryInfo : class, new() where TEncoder : IResonanceEncoder, new()
    {
        /// <summary>
        /// Occurs before broadcasting the discovery message and gives a chance to modify the message.
        /// </summary>
        event EventHandler<TDiscoveryInfo> BeforeBroadcasting;

        /// <summary>
        /// Gets the current discovery information instance.
        /// </summary>
        TDiscoveryInfo DiscoveryInfo { get; }

        /// <summary>
        /// Gets the encoder that is used to encode the discovery information message.
        /// </summary>
        TEncoder Encoder { get; }

        /// <summary>
        /// Gets or sets the interval in which the discovery message will be sent.
        /// </summary>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// Gets a value indicating whether this discovery service has been started.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Starts the discovery service.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the discovery service.
        /// </summary>
        void Stop();
    }
}
