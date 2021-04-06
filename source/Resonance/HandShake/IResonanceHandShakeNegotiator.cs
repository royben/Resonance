using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents a Resonance handshake negotiator to.
    /// </summary>
    public interface IResonanceHandShakeNegotiator
    {
        /// <summary>
        /// Occurs when some data needs to be written to the other side.
        /// </summary>
        event EventHandler<ResonanceHandShakeWriteEventArgs> WriteHandShake;

        /// <summary>
        /// Occurs when the symmetric encryption password is available.
        /// </summary>
        event EventHandler<ResonanceHandShakeSymmetricPasswordAvailableEventArgs> SymmetricPasswordAvailable;

        /// <summary>
        /// Gets the negotiator random client id.
        /// </summary>
        int ClientID { get; }

        /// <summary>
        /// Gets a value indicating whether to enable encryption as part of the negotiation.
        /// </summary>
        bool EncryptionEnabled { get; }

        /// <summary>
        /// Gets the current state of the negotiation.
        /// </summary>
        ResonanceHandShakeState State { get; }

        /// <summary>
        /// Gets or sets the hand shake transcoder used to encode a <see cref="ResonanceHandShakeMessage"/>.
        /// </summary>
        IResonanceHandShakeTranscoder HandShakeTranscoder { get; set; }

        /// <summary>
        /// Initializes the negotiation. must be called before any other method.
        /// </summary>
        /// <param name="enableEncryption">Enable encryption.</param>
        /// <param name="cryptographyProvider">The cryptography provider.</param>
        void Reset(bool enableEncryption, IResonanceCryptographyProvider cryptographyProvider);

        /// <summary>
        /// Begins the hand shake.
        /// This method will block execution until the handshake has completed.
        /// </summary>
        void BeginHandShake();

        /// <summary>
        /// Begins the hand shake asynchronously.
        /// </summary>
        /// <returns></returns>
        Task BeginHandShakeAsync();

        /// <summary>
        /// To be called when a new data is available from the other side and the handshake did not complete.
        /// </summary>
        /// <param name="data">Adapter incoming data.</param>
        void HandShakeMessageDataReceived(byte[] data);
    }
}
