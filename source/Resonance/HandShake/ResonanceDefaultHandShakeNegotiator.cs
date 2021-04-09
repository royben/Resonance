using Resonance.Cryptography;
using Resonance.ExtensionMethods;
using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.HandShake
{
    /// <summary>
    /// Represents the default <see cref="IResonanceHandShakeNegotiator"/> implementation.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceObject" />
    /// <seealso cref="Resonance.HandShake.IResonanceHandShakeNegotiator" />
    public class ResonanceDefaultHandShakeNegotiator : ResonanceObject, IResonanceHandShakeNegotiator
    {
        private string _privateKey;
        private string _publicKey;
        private string _symmetricPassword;
        private IResonanceCryptographyProvider _cryptographyProvider;
        private bool _wasReset;
        private object _lock = new object();
        private Object _loggingTag;

        /// <summary>
        /// Occurs when some data needs to be written to the other side.
        /// </summary>
        public event EventHandler<ResonanceHandShakeWriteEventArgs> WriteHandShake;

        /// <summary>
        /// Occurs when the symmetric encryption password is available.
        /// </summary>
        public event EventHandler<ResonanceHandShakeSymmetricPasswordAvailableEventArgs> SymmetricPasswordAvailable;

        /// <summary>
        /// Occurs when the hand shake has completed.
        /// </summary>
        public event EventHandler Completed;

        /// <summary>
        /// Gets the current state of the negotiation.
        /// </summary>
        public ResonanceHandShakeState State { get; private set; }

        /// <summary>
        /// Gets or sets the hand shake transcoder used to encode a <see cref="ResonanceHandShakeMessage" />.
        /// </summary>
        public IResonanceHandShakeTranscoder HandShakeTranscoder { get; set; }

        /// <summary>
        /// Gets the negotiator random client id.
        /// </summary>
        public int ClientID { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to enable encryption as part of the negotiation.
        /// </summary>
        public bool EncryptionEnabled { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceDefaultHandShakeNegotiator"/> class.
        /// </summary>
        public ResonanceDefaultHandShakeNegotiator()
        {
            HandShakeTranscoder = new ResonanceDefaultHandShakeTranscoder();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceDefaultHandShakeNegotiator"/> class.
        /// </summary>
        /// <param name="loggingTag">Set a prefix for the handshake logging.</param>
        public ResonanceDefaultHandShakeNegotiator(Object loggingTag) : this()
        {
            _loggingTag = loggingTag;
        }

        /// <summary>
        /// Initializes the negotiation. must be called before any other method.
        /// </summary>
        /// <param name="enableEncryption">Enable encryption.</param>
        /// <param name="cryptographyProvider">The cryptography provider.</param>
        public void Reset(bool enableEncryption, IResonanceCryptographyProvider cryptographyProvider)
        {
            ClientID = Guid.NewGuid().GetHashCode();

            EncryptionEnabled = enableEncryption;

            State = ResonanceHandShakeState.Idle;

            _cryptographyProvider = cryptographyProvider;
            var keys = _cryptographyProvider.CreateKeys();
            _privateKey = keys.PrivateKey;
            _publicKey = keys.PublicKey;

            _wasReset = true;
        }

        /// <summary>
        /// Begins the hand shake.
        /// This method will block execution until the handshake has completed.
        /// </summary>
        /// <exception cref="InvalidOperationException">Must call reset before attempting to begin a handshake.</exception>
        /// <exception cref="TimeoutException">Could not initiate a handshake within the given timeout.</exception>
        public void BeginHandShake()
        {
            if (!_wasReset) throw new InvalidOperationException("Must call reset before attempting to begin a handshake.");

            if (State == ResonanceHandShakeState.Idle)
            {
                Log.Debug($"{this}: Starting handshake...");
                State = ResonanceHandShakeState.InProgress;
                Log.Info($"{this}: Sending Handshake Request...");
                ResonanceHandShakeMessage request = new ResonanceHandShakeMessage();
                request.Type = ResonanceHandShakeMessageType.Request;
                request.ClientId = ClientID;
                request.RequireEncryption = EncryptionEnabled;
                request.EncryptionPublicKey = _publicKey;

                OnWriteHandShake(HandShakeTranscoder.Encode(request));
            }

            bool cancel = false;

            TimeoutTask.StartNew(() =>
            {
                cancel = true;
            }, TimeSpan.FromSeconds(10));

            while (State != ResonanceHandShakeState.Completed && !cancel)
            {
                Thread.Sleep(10);
            }

            if (cancel)
            {
                throw new TimeoutException("Could not initiate a handshake within the given timeout.");
            }
        }

        /// <summary>
        /// Begins the hand shake asynchronously.
        /// </summary>
        /// <returns></returns>
        public Task BeginHandShakeAsync()
        {
            return Task.Factory.StartNew(() =>
            {
                BeginHandShake();
            });
        }

        /// <summary>
        /// To be called when a new data is available from the other side and the handshake did not complete.
        /// </summary>
        /// <param name="data">Adapter incoming data.</param>
        /// <exception cref="InvalidOperationException">Must call reset before attempting to receive a handshake message.</exception>
        public void HandShakeMessageDataReceived(byte[] data)
        {
            if (!_wasReset) throw new InvalidOperationException("Must call reset before attempting to receive a handshake message.");

            lock (_lock)
            {
                bool shouldSendRequest = State == ResonanceHandShakeState.Idle;

                State = ResonanceHandShakeState.InProgress;

                ResonanceHandShakeMessage message = HandShakeTranscoder.Decode(data);

                if (message.Type == ResonanceHandShakeMessageType.Request)
                {
                    if (shouldSendRequest)
                    {
                        Log.Info($"{this}: Sending Handshake Request...");
                        ResonanceHandShakeMessage r = new ResonanceHandShakeMessage();
                        r.Type = ResonanceHandShakeMessageType.Request;
                        r.ClientId = ClientID;
                        r.RequireEncryption = EncryptionEnabled;
                        r.EncryptionPublicKey = _publicKey;

                        OnWriteHandShake(HandShakeTranscoder.Encode(r));
                        Thread.Sleep(10);
                    }
                    else if (ClientID > message.ClientId)
                    {
                        Thread.Sleep(10);
                    }

                    var request = message;

                    Log.Info($"{this}: Handshake Request Received...");

                    ResonanceHandShakeMessage response = new ResonanceHandShakeMessage();
                    response.Type = ResonanceHandShakeMessageType.Response;
                    response.ClientId = ClientID;

                    if (EncryptionEnabled && request.RequireEncryption)
                    {
                        response.EncryptionPublicKey = _publicKey;
                        response.RequireEncryption = true;

                        if (ClientID > request.ClientId && State != ResonanceHandShakeState.Completed)
                        {
                            _symmetricPassword = Guid.NewGuid().ToString();
                            OnSymmetricPasswordAvailable(_symmetricPassword);
                            response.SymmetricPassword = _cryptographyProvider.Encrypt(_symmetricPassword, request.EncryptionPublicKey);
                            OnWriteHandShake(HandShakeTranscoder.Encode(response));
                            Log.Info($"{this}: Handshake Response Sent...");
                        }
                    }
                    else
                    {
                        if (ClientID > request.ClientId && State != ResonanceHandShakeState.Completed)
                        {
                            OnWriteHandShake(HandShakeTranscoder.Encode(response));
                            Log.Info($"{this}: Handshake Response Sent...");
                        }
                    }
                }
                else if (message.Type == ResonanceHandShakeMessageType.Response)
                {
                    var response = message;

                    Log.Info($"{this}: Handshake Response Received...");

                    if (response.RequireEncryption && EncryptionEnabled)
                    {
                        if (response.ClientId > ClientID)
                        {
                            _symmetricPassword = _cryptographyProvider.Decrypt(response.SymmetricPassword, _privateKey);
                            OnSymmetricPasswordAvailable(_symmetricPassword);
                        }
                    }

                    if (response.ClientId > ClientID)
                    {
                        State = ResonanceHandShakeState.Completed;
                        OnWriteHandShake(HandShakeTranscoder.Encode(new ResonanceHandShakeMessage() { Type = ResonanceHandShakeMessageType.Complete, ClientId = ClientID }));
                        Log.Info($"{this}: Handshake completed.");
                        Completed?.Invoke(this, new EventArgs());
                    }
                }
                else
                {
                    State = ResonanceHandShakeState.Completed;
                    Log.Info($"{this}: Handshake completed.");
                    Completed?.Invoke(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="WriteHandShake"/> event.
        /// </summary>
        /// <param name="data">The data.</param>
        protected virtual void OnWriteHandShake(byte[] data)
        {
            WriteHandShake?.Invoke(this, new ResonanceHandShakeWriteEventArgs(data));
        }

        /// <summary>
        /// Raises the <see cref="SymmetricPasswordAvailable"/> event.
        /// </summary>
        /// <param name="symmetricPassword">The symmetric password.</param>
        protected virtual void OnSymmetricPasswordAvailable(String symmetricPassword)
        {
            SymmetricPasswordAvailable?.Invoke(this, new ResonanceHandShakeSymmetricPasswordAvailableEventArgs(symmetricPassword));
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (_loggingTag != null)
            {
                return $"{_loggingTag}";
            }
            else
            {
                return $"HandShake Negotiator {ClientID}";
            }
        }
    }
}
