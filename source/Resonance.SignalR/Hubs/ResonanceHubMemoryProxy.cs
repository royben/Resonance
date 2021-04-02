using Resonance.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public abstract class ResonanceHubMemoryProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation> :
        ResonanceHubProxy<TCredentials, TServiceInformation, TReportedServiceInformation, TAdapterInformation>
        where TServiceInformation : IResonanceServiceInformation
        where TReportedServiceInformation : IResonanceServiceInformation
    {
        private static ConcurrentList<ResonanceHubRegisteredService<TServiceInformation>> _services = new ConcurrentList<ResonanceHubRegisteredService<TServiceInformation>>();
        private static ConcurrentList<ResonanceHubSession<TServiceInformation>> _sessions = new ConcurrentList<ResonanceHubSession<TServiceInformation>>();
        private static ConcurrentDictionary<String, TCredentials> _loggedInClients = new ConcurrentDictionary<string, TCredentials>();

        protected override void AddService(ResonanceHubRegisteredService<TServiceInformation> service)
        {
            _services.Add(service);
        }

        protected override void AddSession(ResonanceHubSession<TServiceInformation> session)
        {
            _sessions.Add(session);
        }

        protected override IEnumerable<ResonanceHubRegisteredService<TServiceInformation>> GetServices(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression)
        {
            return _services.Where(expression.Compile());
        }

        protected override IEnumerable<ResonanceHubSession<TServiceInformation>> GetSessions(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression)
        {
            return _sessions.Where(expression.Compile());
        }

        protected override void Login(TCredentials credentials, string connectionId)
        {
            OnLogin(credentials);
            _loggedInClients[connectionId] = credentials;
        }

        protected abstract void OnLogin(TCredentials credentials);

        protected override void RemoveService(ResonanceHubRegisteredService<TServiceInformation> service)
        {
            _services.Remove(service);
        }

        protected override void RemoveSession(ResonanceHubSession<TServiceInformation> session)
        {
            _sessions.Remove(session);
        }

        protected override void Validate(string connectionId)
        {
            if (!_loggedInClients.ContainsKey(connectionId))
            {
                throw new AuthenticationException("The current client was not logged in.");
            }
        }
    }
}
