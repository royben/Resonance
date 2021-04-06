using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents a Resonance hub proxy base class.
    /// </summary>
    /// <typeparam name="TCredentials">The type of the credentials.</typeparam>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    /// <typeparam name="TReportedServiceInformation">The type of the reported service information.</typeparam>
    /// <typeparam name="TAdapterInformation">The type of the adapter information.</typeparam>
    /// <seealso cref="Resonance.SignalR.Hubs.IResonanceHubProxy{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation}" />
    public abstract class ResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> :
        IResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        private InvokeClientMethodDelegate _invoke;
        private Func<String> _getConnectionId;

        /// <summary>
        /// Gets the hub repository.
        /// </summary>
        protected IResonanceHubRepository<TServiceInformation> Repository { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceHubProxy{TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation}"/> class.
        /// </summary>
        /// <param name="repository">Instance hub repository.</param>
        public ResonanceHubProxy(IResonanceHubRepository<TServiceInformation> repository)
        {
            Repository = repository;
        }

        /// <summary>
        /// Initializes this proxy.
        /// </summary>
        /// <param name="invokeClient">Provide the callback that will be used to invoke server methods.</param>
        /// <param name="getConnectionId">Provide the callback that will be used to get the current context connection id.</param>
        public virtual void Init(InvokeClientMethodDelegate invokeClient, Func<String> getConnectionId)
        {
            _invoke = invokeClient;
            _getConnectionId = getConnectionId;
        }

        /// <summary>
        /// Authenticates the specified service or adapter with the system.
        /// </summary>
        /// <param name="credentials">The service/adapter credentials.</param>
        public virtual void Login(TCredentials credentials)
        {
            Login(credentials, _getConnectionId());
        }

        /// <summary>
        /// Authenticates the specified service or adapter with the system.
        /// </summary>
        /// <param name="credentials">The credentials.</param>
        /// <param name="connectionId">The current connection identifier.</param>
        protected abstract void Login(TCredentials credentials, String connectionId);

        /// <summary>
        /// Validates the specified connection identifier (should be done as quickly as possible).
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        protected abstract void Validate(String connectionId);

        /// <summary>
        /// Registers a service using the specified service information.
        /// </summary>
        /// <param name="serviceInformation">The service information.</param>
        /// <exception cref="NullReferenceException">Error registering null service information.</exception>
        public virtual void RegisterService(TServiceInformation serviceInformation)
        {
            Validate(_getConnectionId());

            if (serviceInformation == null) throw new NullReferenceException("Error registering null service information.");

            var service = Repository.GetService(serviceInformation.ServiceId);

            if (service == null)
            {
                service = new ResonanceHubRegisteredService<TServiceInformation>()
                {
                    ConnectionId = _getConnectionId(),
                    ServiceInformation = serviceInformation
                };

                Repository.AddService(service);
            }
            else
            {
                service.ConnectionId = _getConnectionId();
                service.ServiceInformation = serviceInformation;
            }
        }

        /// <summary>
        /// Unregisters a service by the current connection id.
        /// Closes all relevant adapter sessions.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current client is not registered as a service.</exception>
        public virtual void UnregisterService()
        {
            Validate(_getConnectionId());

            var service = Repository.GetService(x => x.ConnectionId == _getConnectionId());

            if (service == null) throw new InvalidOperationException("The current client is not registered as a service.");

            Repository.RemoveService(service);

            var serviceSessions = Repository.GetSessions(x => x.Service == service).ToList();

            serviceSessions.ForEach(session =>
            {
                if (session.ConnectedConnectionId != null)
                {
                    _invoke(ResonanceHubMethods.ServiceDown, session.ConnectedConnectionId);
                }

                if (session.AcceptedConnectionId != null)
                {
                    _invoke(ResonanceHubMethods.ServiceDown, session.AcceptedConnectionId);
                }

                Repository.RemoveSession(session);
            });
        }

        /// <summary>
        /// Gets the available services for the current connected client.
        /// </summary>
        /// <returns></returns>
        public virtual List<TReportedServiceInformation> GetAvailableServices()
        {
            Validate(_getConnectionId());
            return FilterServicesInformation(Repository.GetServices(x => true).Select(x => x.ServiceInformation).ToList(), _getConnectionId());
        }

        /// <summary>
        /// Maps and filters the collection of service information to reported service information based on the specified connection id.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="connectionId">The current connection identifier.</param>
        /// <returns></returns>
        protected abstract List<TReportedServiceInformation> FilterServicesInformation(List<TServiceInformation> services, String connectionId);

        /// <summary>
        /// Creates a new "pending session" and sends a connection request to the specified service.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns>
        /// The new pending session id.
        /// </returns>
        /// <exception cref="KeyNotFoundException">The specified resonance service was not found.</exception>
        public virtual string Connect(string serviceId)
        {
            Validate(_getConnectionId());

            var service = Repository.GetService(serviceId);

            if (service == null) throw new KeyNotFoundException("The specified resonance service was not found.");

            var newPendingSession = new ResonanceHubSession<TServiceInformation>()
            {
                ConnectedConnectionId = _getConnectionId(),
                Service = service,
            };

            Repository.AddSession(newPendingSession);

            TAdapterInformation adapterInformation = GetAdapterInformation(_getConnectionId());

            _invoke(ResonanceHubMethods.ConnectionRequest, service.ConnectionId, newPendingSession.SessionId, adapterInformation);

            return newPendingSession.SessionId;
        }

        /// <summary>
        /// Returns the remote adapter information based on the specified connection id.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        /// <returns></returns>
        protected abstract TAdapterInformation GetAdapterInformation(String connectionId);

        /// <summary>
        /// Accepts a connection request triggered by <see cref="M:Resonance.SignalR.Hubs.IResonanceHub`4.Connect(System.String)" />, completes the "pending session" and notifies the requesting adapter.
        /// </summary>
        /// <param name="sessionId">The pending session identifier.</param>
        /// <exception cref="KeyNotFoundException">The specified session id was not found.</exception>
        public virtual void AcceptConnection(string sessionId)
        {
            Validate(_getConnectionId());

            var pendingSession = Repository.GetSession(sessionId);

            if (pendingSession == null) throw new KeyNotFoundException("The specified session id was not found.");

            pendingSession.AcceptedConnectionId = _getConnectionId();

            _invoke(ResonanceHubMethods.Connected, pendingSession.ConnectedConnectionId);
        }

        /// <summary>
        /// Declines the connection request and drops any pending session and notifies the requesting adapter.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <exception cref="KeyNotFoundException">The specified session id was not found.</exception>
        public virtual void DeclineConnection(string sessionId)
        {
            Validate(_getConnectionId());

            var pendingSession = Repository.GetSession(sessionId);

            if (pendingSession == null) throw new KeyNotFoundException("The specified session id was not found.");

            Repository.RemoveSession(pendingSession);

            _invoke(ResonanceHubMethods.Declined, pendingSession.ConnectedConnectionId);
        }

        /// <summary>
        /// Disconnects the current connected adapter, closes the session and notify the other-side adapter.
        /// </summary>
        public virtual void Disconnect()
        {
            Validate(_getConnectionId());

            var session = GetContextSession();

            if (session == null) return;

            String otherSideConnectionId = GetOtherSideConnectionId();

            Repository.RemoveSession(session);

            if (otherSideConnectionId != null)
            {
                _invoke(ResonanceHubMethods.Disconnect, otherSideConnectionId);
            }
        }

        /// <summary>
        /// Writes the specified data to the other side adapter.
        /// </summary>
        /// <param name="data">The data.</param>
        public virtual void Write(byte[] data)
        {
            String otherSideConnectionId = GetOtherSideConnectionId();

            if (otherSideConnectionId != null)
            {
                _invoke(ResonanceHubMethods.DataAvailable, otherSideConnectionId, data);
            }
        }

        /// <summary>
        /// Gets the other side connection identifier if the current connection is in session.
        /// </summary>
        /// <returns></returns>
        protected String GetOtherSideConnectionId()
        {
            var session = GetContextSession();

            if (session == null)
            {
                return null;
            }

            if (session.ConnectedConnectionId == _getConnectionId())
            {
                return session.AcceptedConnectionId;
            }
            else
            {
                return session.ConnectedConnectionId;
            }
        }

        /// <summary>
        /// Gets the current connection id session if any.
        /// </summary>
        /// <returns></returns>
        protected ResonanceHubSession<TServiceInformation> GetContextSession()
        {
            var session = Repository.GetSession(x => x.ConnectedConnectionId == _getConnectionId() || x.AcceptedConnectionId == _getConnectionId());
            return session;
        }

        /// <summary>
        /// Gets the current connection identifier.
        /// </summary>
        /// <returns></returns>
        protected String GetConnectionId()
        {
            return _getConnectionId();
        }

        /// <summary>
        /// Invokes the specified hub event.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="connectionId">The connection identifier.</param>
        /// <param name="args">The arguments.</param>
        protected void Invoke(String methodName, String connectionId, params object[] args)
        {
            _invoke(methodName, connectionId, args);
        }
    }
}
