using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public delegate void InvokeClientMethodDelegate(String methodName, String connectionId, params object[] args);

    public abstract class ResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> :
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        private InvokeClientMethodDelegate _invoke;
        private Func<String> _getConnectionId;

        public void Init(InvokeClientMethodDelegate invokeClient, Func<String> getConnectionId)
        {
            _invoke = invokeClient;
            _getConnectionId = getConnectionId;
        }

        public void Login(TCredentials credentials)
        {
            Login(credentials, _getConnectionId());
        }

        protected abstract void Login(TCredentials credentials, String connectionId);

        protected abstract void Validate(String connectionId);

        protected abstract IEnumerable<ResonanceHubRegisteredService<TServiceInformation>> GetServices(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression);

        protected abstract IEnumerable<ResonanceHubSession<TServiceInformation>> GetSessions(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression);

        protected abstract void AddService(ResonanceHubRegisteredService<TServiceInformation> service);

        protected abstract void RemoveService(ResonanceHubRegisteredService<TServiceInformation> service);

        protected abstract void AddSession(ResonanceHubSession<TServiceInformation> session);

        protected abstract void RemoveSession(ResonanceHubSession<TServiceInformation> session);

        public void RegisterService(TServiceInformation serviceInformation)
        {
            Validate(_getConnectionId());

            if (serviceInformation == null) throw new NullReferenceException("Error registering null service information.");

            var service = GetServices(x => x.ServiceInformation.ServiceId == serviceInformation.ServiceId).FirstOrDefault();

            if (service == null)
            {
                service = new ResonanceHubRegisteredService<TServiceInformation>()
                {
                    ConnectionId = _getConnectionId(),
                    ServiceInformation = serviceInformation
                };

                AddService(service);
            }
            else
            {
                service.ConnectionId = _getConnectionId();
                service.ServiceInformation = serviceInformation;
            }
        }

        public void UnregisterService()
        {
            Validate(_getConnectionId());

            var service = GetServices(x => x.ConnectionId == _getConnectionId()).FirstOrDefault();

            if (service == null) throw new InvalidOperationException("The current client is not registered as a service.");

            RemoveService(service);

            var serviceSessions = GetSessions(x => x.Service == service).ToList();

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

                RemoveSession(session);
            });
        }

        public List<TReportedServiceInformation> GetAvailableServices()
        {
            Validate(_getConnectionId());
            return FilterServicesInformation(GetServices(x => true).Select(x => x.ServiceInformation).ToList());
        }

        protected abstract List<TReportedServiceInformation> FilterServicesInformation(List<TServiceInformation> services);

        public string Connect(string serviceId)
        {
            Validate(_getConnectionId());

            var service = GetServices(x => x.ServiceInformation.ServiceId == serviceId).FirstOrDefault();

            if (service == null) throw new KeyNotFoundException("The specified resonance service was not found.");

            var newPendingSession = new ResonanceHubSession<TServiceInformation>()
            {
                ConnectedConnectionId = _getConnectionId(),
                Service = service,
            };

            AddSession(newPendingSession);

            TAdapterInformation adapterInformation = GetAdapterInformation(_getConnectionId());

            _invoke(ResonanceHubMethods.ConnectionRequest, service.ConnectionId, newPendingSession.SessionId, adapterInformation);

            return newPendingSession.SessionId;
        }

        protected abstract TAdapterInformation GetAdapterInformation(String connectionId);

        public void AcceptConnection(string sessionId)
        {
            Validate(_getConnectionId());

            var pendingSession = GetSessions(x => x.SessionId == sessionId).FirstOrDefault();

            if (pendingSession == null) throw new KeyNotFoundException("The specified session id was not found.");

            pendingSession.AcceptedConnectionId = _getConnectionId();

            _invoke(ResonanceHubMethods.Connected, pendingSession.ConnectedConnectionId);
        }

        public void DeclineConnection(string sessionId)
        {
            Validate(_getConnectionId());

            var pendingSession = GetSessions(x => x.SessionId == sessionId).FirstOrDefault();

            if (pendingSession == null) throw new KeyNotFoundException("The specified session id was not found.");

            RemoveSession(pendingSession);

            _invoke(ResonanceHubMethods.Declined, pendingSession.ConnectedConnectionId);
        }

        public void Disconnect()
        {
            Validate(_getConnectionId());

            var session = GetContextSession();

            if (session == null) return;

            String otherSideConnectionId = GetOtherSideConnectionId();

            RemoveSession(session);

            if (otherSideConnectionId != null)
            {
                _invoke(ResonanceHubMethods.Disconnect, otherSideConnectionId);
            }
        }

        public void Write(byte[] data)
        {
            String otherSideConnectionId = GetOtherSideConnectionId();

            if (otherSideConnectionId != null)
            {
                _invoke(ResonanceHubMethods.DataAvailable, otherSideConnectionId, data);
            }
        }

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

        protected ResonanceHubSession<TServiceInformation> GetContextSession()
        {
            var session = GetSessions(x => x.ConnectedConnectionId == _getConnectionId() || x.AcceptedConnectionId == _getConnectionId()).FirstOrDefault();
            return session;
        }
    }
}
