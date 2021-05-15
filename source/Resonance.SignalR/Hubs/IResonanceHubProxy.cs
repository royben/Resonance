using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents a delegate for invoking a SignalR hub server method dynamically.
    /// </summary>
    /// <param name="methodName">Name of the method.</param>
    /// <param name="connectionId">The connection identifier.</param>
    /// <param name="args">The arguments.</param>
    public delegate void InvokeClientMethodDelegate(String methodName, String connectionId, params object[] args);

    /// <summary>
    /// Represents an <see cref="IResonanceHub{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation}"/> proxy.
    /// HubProxy contains the actual implementation of a ResonanceHub, and is used to unify the behavior of ResonanceHub and ResonanceHubCore.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    /// <typeparam name="TReportedServiceInformation">The type of the reported service information.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
    /// <seealso cref="Resonance.SignalR.Hubs.IResonanceHub{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation}" />
    public interface IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> : 
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        /// <summary>
        /// Initializes this proxy.
        /// </summary>
        /// <param name="invokeClient">Provide the callback that will be used to invoke server methods.</param>
        /// <param name="getConnectionId">Provide the callback that will be used to get the current context connection id.</param>
        void Init(InvokeClientMethodDelegate invokeClient, Func<String> getConnectionId);

        /// <summary>
        /// Called when a client has disconnected.
        /// </summary>
        void ConnectionClosed();
    }
}
