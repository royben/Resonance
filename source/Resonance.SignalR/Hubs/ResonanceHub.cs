using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public abstract class ResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        : Hub,
        IResonanceHub<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        private static List<ResonanceHubRegisteredService<TServiceInformation>> _services = new List<ResonanceHubRegisteredService<TServiceInformation>>();
        private static List<ResonanceHubSession<TServiceInformation>> _sessions = new List<ResonanceHubSession<TServiceInformation>>();

        public void Login(TCredentials credentials)
        {
            Login(credentials, Context.ConnectionId);
        }

        protected abstract void Login(TCredentials credentials, String connectionId);

        protected abstract void Validate(String connectionId);

        public void RegisterService(TServiceInformation serviceInformation)
        {
            Validate(Context.ConnectionId);

            if (serviceInformation == null) throw new NullReferenceException("Error registering null service information.");

            if (!_services.Exists(x => x.ServiceInformation.ServiceId == serviceInformation.ServiceId))
            {
                _services.Add(new ResonanceHubRegisteredService<TServiceInformation>()
                {
                    ConnectionId = Context.ConnectionId,
                    ServiceInformation = serviceInformation
                });
            }
        }

        public void UnregisterService()
        {
            Validate(Context.ConnectionId);

            var service = _services.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);

            if (service == null) throw new InvalidOperationException("The current client is not registered as a service.");

            _services.Remove(service);

            _sessions.ForEach(session => 
            {
                if (session.ConnectedConnectionId != null)
                {
                    Clients.Client(session.ConnectedConnectionId).ServiceDown();
                }

                if (session.AcceptedConnectionId != null)
                {
                    Clients.Client(session.AcceptedConnectionId).ServiceDown();
                }
            });

            _sessions.RemoveAll(x => x.Service == service);
        }

        public List<TReportedServiceInformation> GetAvailableServices()
        {
            Validate(Context.ConnectionId);
            return FilterServicesInformation(_services.Select(x => x.ServiceInformation).ToList());
        }

        protected abstract List<TReportedServiceInformation> FilterServicesInformation(List<TServiceInformation> services);

        public string Connect(string serviceId)
        {
            Validate(Context.ConnectionId);

            var service = _services.FirstOrDefault(x => x.ServiceInformation.ServiceId == serviceId);

            if (service == null) throw new KeyNotFoundException("The specified resonance service was not found.");

            var newPendingSession = new ResonanceHubSession<TServiceInformation>()
            {
                ConnectedConnectionId = Context.ConnectionId,
                Service = service,
            };

            _sessions.Add(newPendingSession);

            TAdapterInformation adapterInformation = GetAdapterInformation(Context.ConnectionId);

            Clients.Client(service.ConnectionId).ConnectionRequest(newPendingSession.SessionId, adapterInformation);

            return newPendingSession.SessionId;
        }

        protected virtual TAdapterInformation GetAdapterInformation(String connectionId)
        {
            return default(TAdapterInformation);
        }

        public void AcceptConnection(string sessionId)
        {
            Validate(Context.ConnectionId);

            var pendingSession = _sessions.FirstOrDefault(x => x.SessionId == sessionId);

            if (pendingSession == null) throw new KeyNotFoundException("The specified session id was not found.");

            pendingSession.AcceptedConnectionId = Context.ConnectionId;

            Clients.Client(pendingSession.ConnectedConnectionId).Connected();
        }

        public void DeclineConnection(string sessionId)
        {
            Validate(Context.ConnectionId);

            var pendingSession = _sessions.FirstOrDefault(x => x.SessionId == sessionId);

            if (pendingSession == null) throw new KeyNotFoundException("The specified session id was not found.");

            _sessions.Remove(pendingSession);

            Clients.Client(pendingSession.ConnectedConnectionId).Declined();
        }

        public void Disconnect()
        {
            Validate(Context.ConnectionId);

            var session = GetContextSession();

            if (session == null) return;

            String otherSideConnectionId = GetOtherSideConnectionId();

            _sessions.Remove(session);

            if (otherSideConnectionId != null)
            {
                Clients.Client(otherSideConnectionId).Disconnected();
            }
        }

        public void Write(byte[] data)
        {
            String otherSideConnectionId = GetOtherSideConnectionId();

            if (otherSideConnectionId != null)
            {
                Clients.Client(otherSideConnectionId).DataAvailable(data);
            }
        }

        protected String GetOtherSideConnectionId()
        {
            var session = GetContextSession();

            if (session == null)
            {
                return null;
            }

            if (session.ConnectedConnectionId == Context.ConnectionId)
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
            var session = _sessions.FirstOrDefault(x => x.ConnectedConnectionId == Context.ConnectionId || x.AcceptedConnectionId == Context.ConnectionId);
            return session;
        }
    }
}
