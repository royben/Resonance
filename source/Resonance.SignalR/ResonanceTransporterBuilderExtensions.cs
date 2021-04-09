using Resonance;
using Resonance.Adapters.SignalR;
using Resonance.SignalR;
using Resonance.SignalR.BuilderExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Resonance.ResonanceTransporterBuilder;

public static class ResonanceTransporterBuilderExtensions
{
    /// <summary>
    /// Sets the transporter adapter to <see cref="SignalRAdapter{TCredentials}"/>.
    /// </summary>
    /// <param name="adapterBuilder">The adapter builder.</param>
    /// <param name="mode">The SinglaR mode (legacy/core).</param>
    public static SignalRAdapterBuilder WithSignalRAdapter(this IAdapterBuilder adapterBuilder, SignalRMode mode)
    {
        return new SignalRAdapterBuilder(adapterBuilder as ResonanceTransporterBuilder, mode);
    }
}

namespace Resonance.SignalR.BuilderExtension
{
    public class SignalRAdapterBuilderBase
    {
        protected ResonanceTransporterBuilder _builder;
        protected SignalRMode _mode;

        internal SignalRAdapterBuilderBase(ResonanceTransporterBuilder builder, SignalRMode mode)
        {
            _builder = builder;
            _mode = mode;
        }
    }

    public class SignalRAdapterBuilder : SignalRAdapterBuilderBase
    {
        internal SignalRAdapterBuilder(ResonanceTransporterBuilder builder, SignalRMode mode) : base(builder, mode)
        {
        }

        /// <summary>
        /// Sets the adapter credentials used to authenticate with the remote hub.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="credentials">The credentials.</param>
        /// <returns></returns>
        public SignalRAdapterHubUrlBuilder<T> WithCredentials<T>(T credentials)
        {
            return new SignalRAdapterHubUrlBuilder<T>(_builder, _mode, credentials);
        }
    }

    public class SignalRAdapterHubUrlBuilder<T> : SignalRAdapterBuilderBase
    {
        private T _credetials;

        internal SignalRAdapterHubUrlBuilder(ResonanceTransporterBuilder builder, SignalRMode mode, T credentials) : base(builder, mode)
        {
            _credetials = credentials;
        }

        /// <summary>
        /// Sets the remote Resonance SignalR service id to connect to.
        /// </summary>
        /// <param name="serviceId">The service identifier.</param>
        /// <returns></returns>
        public SignalRAdapterBuilderServiceId<T> WithServiceId(String serviceId)
        {
            return new SignalRAdapterBuilderServiceId<T>(_builder, _mode, _credetials, serviceId);
        }
    }

    public class SignalRAdapterBuilderServiceId<T> : SignalRAdapterBuilderBase
    {
        private T _credentials;
        private String _serviceId;

        internal SignalRAdapterBuilderServiceId(ResonanceTransporterBuilder builder, SignalRMode mode, T credentials, String serviceId) : base(builder, mode)
        {
            _credentials = credentials;
            _serviceId = serviceId;
        }

        /// <summary>
        /// Sets the remote hub url.
        /// </summary>
        /// <param name="url">The URL.</param>
        public ITranscodingBuilder WithUrl(String url)
        {
            IResonanceTransporter transporter = _builder.GetType().GetProperty("Transporter", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_builder) as IResonanceTransporter;
            transporter.Adapter = new SignalRAdapter<T>(_credentials, url, _serviceId, _mode);
            return _builder;
        }
    }
}
