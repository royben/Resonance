using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Routing
{
    /// <summary>
    /// Represents a transporter router capable of routing between two transporters.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceObject" />
    /// <seealso cref="Resonance.IResonanceComponent" />
    /// <seealso cref="System.IDisposable" />
    public class TransporterRouter : ResonanceObject, IResonanceComponent, IDisposable
    {
        private int _componentNumber;

        #region Events

        /// <summary>
        /// Occurs when incoming data has arrived from the source transporter.
        /// </summary>
        public event EventHandler<ResonancePreviewDecodingInfoEventArgs> PreviewSourceDecodingInformation;

        /// <summary>
        /// Occurs when incoming data has arrived from the target transporter.
        /// </summary>
        public event EventHandler<ResonancePreviewDecodingInfoEventArgs> PreviewTargetDecodingInformation;

        #endregion

        #region Properties

        private IResonanceTransporter _sourceTransporter;
        /// <summary>
        /// Gets or sets the source transporter.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot change source transporter while bounded.</exception>
        public IResonanceTransporter SourceTransporter
        {
            get { return _sourceTransporter; }
            set
            {
                if (IsBound)
                {
                    throw new InvalidOperationException("Cannot change source transporter while bounded.");
                }

                _sourceTransporter = value;
            }
        }

        private IResonanceTransporter _targetTransporter;
        /// <summary>
        /// Gets or sets the target transporter.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot change target transporter while bounded.</exception>
        public IResonanceTransporter TargetTransporter
        {
            get { return _targetTransporter; }
            set
            {
                if (IsBound)
                {
                    throw new InvalidOperationException("Cannot change target transporter while bounded.");
                }

                _targetTransporter = value;
            }
        }

        /// <summary>
        /// Gets or sets whether data is routed one-way or two-way.
        /// </summary>
        public RoutingMode RoutingMode { get; set; }

        /// <summary>
        /// Gets or sets how data is going to be submitted when routing occurs.
        /// </summary>
        public WritingMode WritingMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to send a disconnection the transporter when the other one has failed.
        /// Default value is true.
        /// </summary>
        public bool DisconnectOnFailure { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to disconnect the transporter when the other one has disconnected.
        /// Default value is true.
        /// </summary>
        public bool RouteDisconnection { get; set; }

        /// <summary>
        /// Gets a value indicating whether the source and target transporters are currently bounded.
        /// </summary>
        public bool IsBound { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TransporterRouter"/> class.
        /// </summary>
        public TransporterRouter() : base()
        {
            _componentNumber = ResonanceComponentCounterManager.Default.GetIncrement(this);
            RouteDisconnection = true;
            DisconnectOnFailure = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransporterRouter"/> class.
        /// </summary>
        /// <param name="sourceTransporter">The source transporter.</param>
        /// <param name="targetTransporter">The target transporter.</param>
        /// <param name="routingMode">The routing mode.</param>
        /// <param name="writingMode">The writing mode.</param>
        public TransporterRouter(IResonanceTransporter sourceTransporter, IResonanceTransporter targetTransporter, RoutingMode routingMode, WritingMode writingMode) : this()
        {
            SourceTransporter = sourceTransporter;
            TargetTransporter = targetTransporter;
            RoutingMode = routingMode;
            WritingMode = writingMode;
        }

        #endregion

        #region Bind / Unbind

        /// <summary>
        /// Binds the source and target transporters and starts the routing.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// SourceTransporter
        /// or
        /// TargetTransporter
        /// </exception>
        /// <exception cref="InvalidOperationException">Source and target transporters must be different.</exception>
        public void Bind()
        {
            if (SourceTransporter == null) throw new ArgumentNullException(nameof(SourceTransporter));
            if (TargetTransporter == null) throw new ArgumentNullException(nameof(TargetTransporter));
            if (SourceTransporter == TargetTransporter) throw new InvalidOperationException("Source and target transporters must be different.");

            Logger.LogInformation($"Binding transporters '{SourceTransporter}' <=> '{TargetTransporter}'...");

            SourceTransporter.PreviewDecodingInformation -= SourceTransporter_PreviewDecodingInformation;
            TargetTransporter.PreviewDecodingInformation -= TargetTransporter_PreviewDecodingInformation;
            SourceTransporter.PreviewDecodingInformation += SourceTransporter_PreviewDecodingInformation;
            TargetTransporter.PreviewDecodingInformation += TargetTransporter_PreviewDecodingInformation;

            SourceTransporter.StateChanged -= SourceTransporter_StateChanged;
            TargetTransporter.StateChanged -= TargetTransporter_StateChanged;
            SourceTransporter.StateChanged += SourceTransporter_StateChanged;
            TargetTransporter.StateChanged += TargetTransporter_StateChanged;

            IsBound = true;
        }

        /// <summary>
        /// Unbinds the source and target transporters.
        /// </summary>
        public void Unbind()
        {
            Logger.LogInformation($"Unbinding transporters '{SourceTransporter}' <=> '{TargetTransporter}'...");

            if (SourceTransporter != null)
            {
                SourceTransporter.PreviewDecodingInformation -= SourceTransporter_PreviewDecodingInformation;
                SourceTransporter.StateChanged -= SourceTransporter_StateChanged;
            }

            if (TargetTransporter != null)
            {
                TargetTransporter.PreviewDecodingInformation -= TargetTransporter_PreviewDecodingInformation;
                TargetTransporter.StateChanged -= TargetTransporter_StateChanged;
            }

            IsBound = false;
        }

        #endregion

        #region Event Handlers

        private void SourceTransporter_PreviewDecodingInformation(object sender, ResonancePreviewDecodingInfoEventArgs e)
        {
            if (SourceTransporter.CheckPending(e.DecodingInformation.Token)) return;
            DoRouting(e, TargetTransporter);
        }

        private void TargetTransporter_PreviewDecodingInformation(object sender, ResonancePreviewDecodingInfoEventArgs e)
        {
            if (TargetTransporter.CheckPending(e.DecodingInformation.Token)) return;
            DoRouting(e, SourceTransporter);
        }

        private void SourceTransporter_StateChanged(object sender, ResonanceComponentStateChangedEventArgs e)
        {
            if (e.NewState == ResonanceComponentState.Failed && DisconnectOnFailure && TargetTransporter.State == ResonanceComponentState.Connected)
            {
                Task.Factory.StartNew(async () =>
                {
                    Logger.LogInformation("Source transporter has failed. disconnecting target transporter...");

                    try
                    {
                        await TargetTransporter.DisconnectAsync($"The remote routed source transporter has failed: {SourceTransporter.FailedStateException.Message}.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error occurred while trying to disconnect the target transporter.");
                    }
                });
            }
        }

        private void TargetTransporter_StateChanged(object sender, ResonanceComponentStateChangedEventArgs e)
        {
            if (e.NewState == ResonanceComponentState.Failed && DisconnectOnFailure && SourceTransporter.State == ResonanceComponentState.Connected)
            {
                Task.Factory.StartNew(async () =>
                {
                    Logger.LogInformation("Target transporter has failed. disconnecting target transporter...");

                    try
                    {
                        await SourceTransporter.DisconnectAsync($"The remote routed target transporter has failed: {TargetTransporter.FailedStateException.Message}.");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error occurred while trying to disconnect the source transporter.");
                    }
                });
            }
        }

        #endregion

        #region Routing

        private void DoRouting(ResonancePreviewDecodingInfoEventArgs e, IResonanceTransporter transporter)
        {
            try
            {
                if (transporter.State == ResonanceComponentState.Connected)
                {
                    if (RoutingMode == RoutingMode.TwoWay ||
                        (transporter == SourceTransporter && RoutingMode == RoutingMode.OneWayToSource) ||
                        (transporter == TargetTransporter && RoutingMode == RoutingMode.OneWayToTarget))
                    {
                        if (e.DecodingInformation.Type == ResonanceTranscodingInformationType.KeepAliveRequest) return;
                        if (e.DecodingInformation.Type == ResonanceTranscodingInformationType.KeepAliveResponse) return;

                        if (e.DecodingInformation.Type != ResonanceTranscodingInformationType.Disconnect)
                        {
                            e.Handled = true;

                            var args = new ResonancePreviewDecodingInfoEventArgs();
                            args.DecodingInformation = e.DecodingInformation;
                            args.RawData = e.RawData;

                            if (transporter == SourceTransporter)
                            {
                                OnPreviewSourceDecodingInformation(args);
                            }
                            else
                            {
                                OnPreviewTargetDecodingInformation(args);
                            }

                            if (args.Handled) return;

                            if (WritingMode == WritingMode.Standard)
                            {
                                ResonanceEncodingInformation info = new ResonanceEncodingInformation();
                                info.Completed = e.DecodingInformation.Completed;
                                info.ErrorMessage = e.DecodingInformation.ErrorMessage;
                                info.HasError = e.DecodingInformation.HasError;
                                info.IsCompressed = e.DecodingInformation.IsCompressed;
                                info.Message = e.DecodingInformation.Message;
                                info.RPCSignature = e.DecodingInformation.RPCSignature;
                                info.Timeout = e.DecodingInformation.Timeout;
                                info.Token = e.DecodingInformation.Token;
                                info.Transcoding = e.DecodingInformation.Transcoding;
                                info.Type = e.DecodingInformation.Type;

                                transporter.SubmitEncodingInformation(info);
                            }
                            else
                            {
                                transporter.Adapter.Write(e.RawData);
                            }
                        }
                        else if (RouteDisconnection)
                        {
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    if (transporter == SourceTransporter)
                                    {
                                        Logger.LogInformation($"Disconnection notification was received by source transporter. disconnecting target transporter...");
                                    }
                                    else
                                    {
                                        Logger.LogInformation($"Disconnection notification was received by target transporter. disconnecting source transporter...");
                                    }

                                    transporter.Disconnect();
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogError(ex, "Error occurred while trying to disconnect the transporter.");
                                }
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected routing error has occurred.");
            }
        }

        #endregion

        #region Virtual Methods

        /// <summary>
        /// Raises the <see cref="E:PreviewSourceDecodingInformation" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ResonancePreviewDecodingInfoEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPreviewSourceDecodingInformation(ResonancePreviewDecodingInfoEventArgs args)
        {
            PreviewSourceDecodingInformation?.Invoke(SourceTransporter, args);
        }

        /// <summary>
        /// Raises the <see cref="E:PreviewTargetDecodingInformation" /> event.
        /// </summary>
        /// <param name="args">The <see cref="ResonancePreviewDecodingInfoEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPreviewTargetDecodingInformation(ResonancePreviewDecodingInfoEventArgs args)
        {
            PreviewTargetDecodingInformation?.Invoke(SourceTransporter, args);
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Unbinds the source and target transporters.
        /// </summary>
        public void Dispose()
        {
            Unbind();
        }

        #endregion

        #region ToString

        /// <summary>
        /// Gets the string representation of this router.
        /// </summary>
        public override string ToString()
        {
            return $"{this.GetType().Name} {_componentNumber}";
        }

        #endregion
    }
}
