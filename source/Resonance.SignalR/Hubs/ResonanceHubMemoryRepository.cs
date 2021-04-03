using Resonance.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public class ResonanceHubMemoryRepository<TServiceInformation> :
        IResonanceHubRepository<TServiceInformation>
        where TServiceInformation : IResonanceServiceInformation
    {
        private static ConcurrentList<ResonanceHubRegisteredService<TServiceInformation>> _services = new ConcurrentList<ResonanceHubRegisteredService<TServiceInformation>>();
        private static ConcurrentList<ResonanceHubSession<TServiceInformation>> _sessions = new ConcurrentList<ResonanceHubSession<TServiceInformation>>();

        public void AddService(ResonanceHubRegisteredService<TServiceInformation> service)
        {
            _services.Add(service);
        }

        public void AddSession(ResonanceHubSession<TServiceInformation> session)
        {
            _sessions.Add(session);
        }

        public ResonanceHubRegisteredService<TServiceInformation> GetService(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression)
        {
            return _services.FirstOrDefault(expression.Compile());
        }

        public ResonanceHubRegisteredService<TServiceInformation> GetService(string serviceId)
        {
            return _services.FirstOrDefault(x => x.ServiceInformation.ServiceId == serviceId);
        }

        public IEnumerable<ResonanceHubRegisteredService<TServiceInformation>> GetServices(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression)
        {
            return _services.Where(expression.Compile());
        }

        public ResonanceHubSession<TServiceInformation> GetSession(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression)
        {
            return _sessions.FirstOrDefault(expression.Compile());
        }

        public ResonanceHubSession<TServiceInformation> GetSession(string sessionId)
        {
            return _sessions.FirstOrDefault(x => x.SessionId == sessionId);
        }

        public IEnumerable<ResonanceHubSession<TServiceInformation>> GetSessions(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression)
        {
            return _sessions.Where(expression.Compile());
        }

        public void RemoveService(ResonanceHubRegisteredService<TServiceInformation> service)
        {
            _services.Remove(service);
        }

        public void RemoveSession(ResonanceHubSession<TServiceInformation> session)
        {
            _sessions.Remove(session);
        }
    }
}
