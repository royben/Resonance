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
    /// <summary>
    /// Represents an static In-Memory implementation of a Resonance hub proxy repository.
    /// Will not persist application restarts. Used only for testing. 
    /// </summary>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    /// <seealso cref="Resonance.SignalR.Hubs.IResonanceHubRepository{TServiceInformation}" />
    public class ResonanceHubMemoryRepository<TServiceInformation> :
        IResonanceHubRepository<TServiceInformation>
        where TServiceInformation : IResonanceServiceInformation
    {
        private static ConcurrentList<ResonanceHubRegisteredService<TServiceInformation>> _services = new ConcurrentList<ResonanceHubRegisteredService<TServiceInformation>>();
        private static ConcurrentList<ResonanceHubSession<TServiceInformation>> _sessions = new ConcurrentList<ResonanceHubSession<TServiceInformation>>();

        /// <summary>
        /// Stores a registered service.
        /// </summary>
        /// <param name="service">The service.</param>
        public void AddService(ResonanceHubRegisteredService<TServiceInformation> service)
        {
            _services.Add(service);
        }

        /// <summary>
        /// Adds the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        public void AddSession(ResonanceHubSession<TServiceInformation> session)
        {
            _sessions.Add(session);
        }

        /// <summary>
        /// Gets a single service based on the specified expression.
        /// When no such service found, will return null.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public ResonanceHubRegisteredService<TServiceInformation> GetService(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression)
        {
            return _services.FirstOrDefault(expression.Compile());
        }

        /// <summary>
        /// Gets a single service by the specified service id.
        /// When no such service found, will return null.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns></returns>
        public ResonanceHubRegisteredService<TServiceInformation> GetService(string serviceId)
        {
            return _services.FirstOrDefault(x => x.ServiceInformation.ServiceId == serviceId);
        }

        /// <summary>
        /// Gets a collection of services based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public IEnumerable<ResonanceHubRegisteredService<TServiceInformation>> GetServices(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression)
        {
            return _services.Where(expression.Compile());
        }

        /// <summary>
        /// Gets a single session based on the specified expression.
        /// When no such session found, will return null.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public ResonanceHubSession<TServiceInformation> GetSession(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression)
        {
            return _sessions.FirstOrDefault(expression.Compile());
        }

        /// <summary>
        /// Gets a single session by the specified session id.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns></returns>
        public ResonanceHubSession<TServiceInformation> GetSession(string sessionId)
        {
            return _sessions.FirstOrDefault(x => x.SessionId == sessionId);
        }

        /// <summary>
        /// Gets a collection of sessions based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        public IEnumerable<ResonanceHubSession<TServiceInformation>> GetSessions(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression)
        {
            return _sessions.Where(expression.Compile());
        }

        /// <summary>
        /// Removes the registered service.
        /// </summary>
        /// <param name="service">The service.</param>
        public void RemoveService(ResonanceHubRegisteredService<TServiceInformation> service)
        {
            _services.Remove(service);
        }

        /// <summary>
        /// Removes the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        public void RemoveSession(ResonanceHubSession<TServiceInformation> session)
        {
            _sessions.Remove(session);
        }
    }
}
