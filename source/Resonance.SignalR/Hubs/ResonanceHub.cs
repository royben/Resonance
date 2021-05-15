using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents a SignalR legacy Resonance hub base class.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    /// <typeparam name="TReportedServiceInformation">The type of the reported service information.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
    /// <typeparam name="THub">The type of the hub.</typeparam>
    /// <seealso cref="Microsoft.AspNet.SignalR.Hub" />
    /// <seealso cref="Resonance.SignalR.Hubs.IResonanceHub{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation}" />
    public abstract class ResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub>
        : Hub,
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
        where THub : ResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub>
    {
        private IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> _proxy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceHub{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation, THub}"/> class.
        /// </summary>
        /// <param name="proxy">An instance of hub proxy.</param>
        public ResonanceHub(IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> proxy)
        {
            _proxy = proxy;
            _proxy.Init(InvokeClient, GetConnectionId);
        }

        /// <summary>
        /// Authenticates the specified service or adapter with the system.
        /// </summary>
        /// <param name="credentials">The service/adapter credentials.</param>
        public void Login(TCredentials credentials)
        {
            _proxy.Login(credentials);
        }

        /// <summary>
        /// Registers a service using the specified service information.
        /// </summary>
        /// <param name="serviceInformation">The service information.</param>
        public void RegisterService(TServiceInformation serviceInformation)
        {
            _proxy.RegisterService(serviceInformation);
        }

        /// <summary>
        /// Unregisters a service by the current connection id.
        /// Closes all relevant adapter sessions.
        /// </summary>
        public void UnregisterService()
        {
            _proxy.UnregisterService();
        }

        /// <summary>
        /// Gets the available services for the current connected client.
        /// </summary>
        /// <param name="credentials">Credentials used to authenticate the requesting user.</param>
        /// <returns></returns>
        public List<TReportedServiceInformation> GetAvailableServices(TCredentials credentials)
        {
            return _proxy.GetAvailableServices(credentials);
        }

        /// <summary>
        /// Creates a new "pending session" and sends a connection request to the specified service.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns>
        /// The new pending session id.
        /// </returns>
        public string Connect(string serviceId)
        {
            return _proxy.Connect(serviceId);
        }

        /// <summary>
        /// Accepts a connection request triggered by <see cref="M:Resonance.SignalR.Hubs.IResonanceHub`4.Connect(System.String)" />, completes the "pending session" and notifies the requesting adapter.
        /// </summary>
        /// <param name="sessionId">The pending session identifier.</param>
        public void AcceptConnection(string sessionId)
        {
            _proxy.AcceptConnection(sessionId);
        }

        /// <summary>
        /// Declines the connection request and drops any pending session and notifies the requesting adapter.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        public void DeclineConnection(string sessionId)
        {
            _proxy.DeclineConnection(sessionId);
        }

        /// <summary>
        /// Disconnects the current connected adapter, closes the session and notify the other-side adapter.
        /// </summary>
        public void Disconnect()
        {
            _proxy.Disconnect();
        }

        /// <summary>
        /// Writes the specified data to the other side adapter.
        /// </summary>
        /// <param name="data">The data.</param>
        public void Write(byte[] data)
        {
            _proxy.Write(data);
        }

        private string GetConnectionId()
        {
            return Context.ConnectionId;
        }

        private void InvokeClient(string methodName, string connectionId, object[] args)
        {
            var ctx = GlobalHost.ConnectionManager.GetHubContext<THub>();
            IClientProxy proxy = ctx.Clients.Client(connectionId);
            proxy.Invoke(methodName, args);
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            _proxy.ConnectionClosed();
            return base.OnDisconnected(stopCalled);
        }
    }
}
