using Resonance;
using Resonance.WebRTC.Messages;
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// IResonance Transporter WebRTC extension methods.
/// </summary>
public static class IResonanceTransporterExtensionMethods
{
    /// <summary>
    /// Registers for a WebRTC offer request.
    /// Should be used only from a 'Signaling' transporter.
    /// </summary>
    /// <param name="transporter">The transporter.</param>
    /// <param name="callback">Callback method with the Offer request.</param>
    public static void OnWebRtcOffer(this IResonanceTransporter transporter, Action<ResonanceMessage<WebRTCOfferRequest>> callback)
    {
        transporter.RegisterRequestHandler<WebRTCOfferRequest>((_, request) =>
        {
            callback?.Invoke(request);
        });
    }
}
