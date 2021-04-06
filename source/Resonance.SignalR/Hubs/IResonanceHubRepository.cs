using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.SignalR.Hubs
{
    /// <summary>
    /// Represents an ResonanceHubProxy repository used to query and manage current registered services and sessions.
    /// </summary>
    /// <typeparam name="TServiceInformation">The type of the service information.</typeparam>
    public interface IResonanceHubRepository<TServiceInformation>
        where TServiceInformation : IResonanceServiceInformation
    {
        /// <summary>
        /// Stores a registered service.
        /// </summary>
        /// <param name="service">The service.</param>
        void AddService(ResonanceHubRegisteredService<TServiceInformation> service);

        /// <summary>
        /// Removes the registered service.
        /// </summary>
        /// <param name="service">The service.</param>
        void RemoveService(ResonanceHubRegisteredService<TServiceInformation> service);

        /// <summary>
        /// Adds the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        void AddSession(ResonanceHubSession<TServiceInformation> session);

        /// <summary>
        /// Removes the specified session.
        /// </summary>
        /// <param name="session">The session.</param>
        void RemoveSession(ResonanceHubSession<TServiceInformation> session);

        /// <summary>
        /// Gets a collection of services based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        IEnumerable<ResonanceHubRegisteredService<TServiceInformation>> GetServices(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression);

        /// <summary>
        /// Gets a single service based on the specified expression.
        /// When no such service found, will return null.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        ResonanceHubRegisteredService<TServiceInformation> GetService(Expression<Func<ResonanceHubRegisteredService<TServiceInformation>, bool>> expression);

        /// <summary>
        /// Gets a single service by the specified service id.
        /// When no such service found, will return null.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns></returns>
        ResonanceHubRegisteredService<TServiceInformation> GetService(String serviceId);

        /// <summary>
        /// Gets a collection of sessions based on the specified expression.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        IEnumerable<ResonanceHubSession<TServiceInformation>> GetSessions(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression);

        /// <summary>
        /// Gets a single session based on the specified expression.
        /// When no such session found, will return null.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns></returns>
        ResonanceHubSession<TServiceInformation> GetSession(Expression<Func<ResonanceHubSession<TServiceInformation>, bool>> expression);

        /// <summary>
        /// Gets a single session by the specified session id.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <returns></returns>
        ResonanceHubSession<TServiceInformation> GetSession(String sessionId);
    }
}
