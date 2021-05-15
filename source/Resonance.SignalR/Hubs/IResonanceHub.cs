using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents the basic Resonance SignalR hub interface.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials used to authenticate adapters and services.</typeparam>
    /// <typeparam name="TServiceInformation">The type of the service information used to register a remote service.</typeparam>
    /// <typeparam name="TReportedServiceInformation">The type of the reported service information that will be provided to remote adapters.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information that will be provided in the connection request to remote services.</typeparam>
    public interface IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        /// <summary>
        /// Authenticates the specified service or adapter with the system.
        /// </summary>
        /// <param name="credentials">The service/adapter credentials.</param>
        void Login(TCredentials credentials);

        /// <summary>
        /// Registers a service using the specified service information.
        /// </summary>
        /// <param name="serviceInformation">The service information.</param>
        void RegisterService(TServiceInformation serviceInformation);

        /// <summary>
        /// Unregisters a service by the current connection id.
        /// Closes all relevant adapter sessions.
        /// </summary>
        void UnregisterService();

        /// <summary>
        /// Registers a discovery client.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        void RegisterDiscoveryClient(TCredentials credentials);

        /// <summary>
        /// Gets the available services for the current connected client.
        /// </summary>
        /// <param name="credentials">Credentials used to authenticate the requesting user.</param>
        /// <returns></returns>
        List<TReportedServiceInformation> GetAvailableServices(TCredentials credentials);

        /// <summary>
        /// Creates a new "pending session" and sends a connection request to the specified service.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns>The new pending session id.</returns>
        String Connect(String serviceId);

        /// <summary>
        /// Accepts a connection request triggered by <see cref="Connect(string)"/>, completes the "pending session" and notifies the requesting adapter.
        /// </summary>
        /// <param name="sessionId">The pending session identifier.</param>
        void AcceptConnection(String sessionId);

        /// <summary>
        /// Declines the connection request and drops any pending session and notifies the requesting adapter.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        void DeclineConnection(String sessionId);

        /// <summary>
        /// Disconnects the current connected adapter, closes the session and notify the other-side adapter.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Writes the specified data to the other side adapter.
        /// </summary>
        /// <param name="data">The data.</param>
        void Write(byte[] data);
    }
}
