using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    public interface IResonanceHubRepository<TServiceInformation>
        where TServiceInformation : IResonanceServiceInformation
    {
        void AddService(ResonanceHubRegisteredService<TServiceInformation> service);

        void RemoveService(ResonanceHubRegisteredService<TServiceInformation> service);

        void AddSession(ResonanceHubSession<TServiceInformation> session);

        void RemoveSession(ResonanceHubSession<TServiceInformation> session);

        IEnumerable<ResonanceHubRegisteredService<TServiceInformation>> GetServices(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression);

        ResonanceHubRegisteredService<TServiceInformation> GetService(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression);

        ResonanceHubRegisteredService<TServiceInformation> GetService(String serviceId);

        IEnumerable<ResonanceHubSession<TServiceInformation>> GetSessions(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression);

        ResonanceHubSession<TServiceInformation> GetSession(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression);

        ResonanceHubSession<TServiceInformation> GetSession(String sessionId);
    }
}
