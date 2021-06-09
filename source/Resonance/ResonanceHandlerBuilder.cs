using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static Resonance.ResonanceHandlerBuilder;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance transporter message or request handler builder.
    /// </summary>
    /// <seealso cref="Resonance.ResonanceHandlerBuilder.IMessageTypeBuilder" />
    /// <seealso cref="Resonance.ResonanceHandlerBuilder.IRequestTypeBuilder" />
    public interface IResonanceHandlerBuilder : IMessageTypeBuilder, IRequestTypeBuilder
    {

    }

    public class ResonanceHandlerBuilder : IResonanceHandlerBuilder
    {
        public interface IMessageTypeBuilder
        {
            /// <summary>
            /// Register a one-way message handler.
            /// </summary>
            /// <typeparam name="TMessage">The type of the message.</typeparam>
            IIncludeTransporterBuilder<TMessage> ForMessage<TMessage>() where TMessage : class;
        }

        public interface IRequestTypeBuilder
        {
            /// <summary>
            /// Register a request handler that should return a response.
            /// </summary>
            /// <typeparam name="TRequest">The type of the request.</typeparam>
            /// <returns></returns>
            IResponseTypeBuilder<TRequest> ForRequest<TRequest>() where TRequest : class;
        }

        public interface IResponseTypeBuilder<TRequest> :
            IAsyncSyncWithTransporterBuilder<TRequest>
            where TRequest : class
        {
            /// <summary>
            /// The handler method should have a return value that will be sent back to the sender.
            /// </summary>
            /// <typeparam name="TResponse">The type of the response.</typeparam>
            /// <returns></returns>
            IIncludeTransporterBuilder<TRequest, TResponse> WithResponse<TResponse>() where TResponse : class;
        }

        public interface IIncludeTransporterBuilder<TMessage> where TMessage : class
        {
            /// <summary>
            /// The handler will have the receiving transporter as an parameter.
            /// </summary>
            IAsyncSyncWithTransporterBuilder<TMessage> IncludeTransporter();

            /// <summary>
            /// The handler should be asynchronous.
            /// </summary>
            IAsyncBuilder<TMessage> IsAsync();

            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(MessageHandlerDelegate<TMessage> callback);
        }

        public interface IIncludeTransporterBuilder<TRequest, TResponse> where TRequest : class where TResponse : class
        {
            /// <summary>
            /// The handler will have the receiving transporter as an parameter.
            /// </summary>
            IAsyncSyncWithTransporterBuilder<TRequest, TResponse> IncludeTransporter();

            /// <summary>
            /// The handler should be asynchronous.
            /// </summary>
            IAsyncBuilder<TRequest, TResponse> IsAsync();

            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(RequestHandlerDelegate<TRequest, TResponse> callback);
        }

        public interface IAsyncSyncWithTransporterBuilder<TMessage> where TMessage : class
        {
            /// <summary>
            /// The handler should be asynchronous.
            /// </summary>
            IAsyncWithTransporterBuilder<TMessage> IsAsync();

            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(MessageWithTransporterHandlerDelegate<TMessage> callback);
        }

        public interface IAsyncSyncWithTransporterBuilder<TRequest, TResponse> where TRequest : class where TResponse : class
        {
            /// <summary>
            /// The handler should be asynchronous.
            /// </summary>
            IAsyncWithTransporterBuilder<TRequest, TResponse> IsAsync();

            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(RequestWithTransporterHandlerDelegate<TRequest, TResponse> callback);
        }

        public interface IAsyncBuilder<TMessage> where TMessage : class
        {
            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(AsyncMessageHandlerDelegate<TMessage> callback);
        }

        public interface IAsyncBuilder<TRequest, TResponse> where TRequest : class where TResponse : class
        {
            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(AsyncRequestHandlerDelegate<TRequest, TResponse> callback);
        }

        public interface IAsyncWithTransporterBuilder<TMessage> where TMessage : class
        {
            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(AsyncMessageWithTransporterHandlerDelegate<TMessage> callback);
        }

        public interface IAsyncWithTransporterBuilder<TRequest, TResponse> where TRequest : class where TResponse : class
        {
            /// <summary>
            /// Builds the handler.
            /// </summary>
            /// <param name="callback">Specify the method name a let Visual Studio to generate the handler using smart tag.</param>
            /// <returns>The disposable handler registration. When disposed, will unregister this handler.</returns>
            IDisposable Build(AsyncRequestWithTransporterHandlerDelegate<TRequest, TResponse> callback);
        }

        protected IResonanceTransporter _transporter;

        private ResonanceHandlerBuilder(IResonanceTransporter transporter)
        {
            _transporter = transporter;
        }

        public static IResonanceHandlerBuilder CreateNew(IResonanceTransporter transporter)
        {
            return new ResonanceHandlerBuilder(transporter);
        }

        public IIncludeTransporterBuilder<TMessage> ForMessage<TMessage>() where TMessage : class
        {
            return new ResonanceMessageHandlerBuilder<TMessage>(_transporter);
        }

        public IResponseTypeBuilder<TRequest> ForRequest<TRequest>() where TRequest : class
        {
            return new ResonanceRequestResponseHandlerBuilder<TRequest>(_transporter);
        }

        public class ResonanceMessageHandlerBuilder<TMessage> :
            ResonanceHandlerBuilder,
            IIncludeTransporterBuilder<TMessage>,
            IAsyncSyncWithTransporterBuilder<TMessage>,
            IAsyncBuilder<TMessage>,
            IAsyncWithTransporterBuilder<TMessage>
            where TMessage : class
        {
            internal ResonanceMessageHandlerBuilder(IResonanceTransporter transporter) : base(transporter)
            {
            }

            public IAsyncSyncWithTransporterBuilder<TMessage> IncludeTransporter()
            {
                return this;
            }

            public IAsyncBuilder<TMessage> IsAsync()
            {
                return this;
            }

            IAsyncWithTransporterBuilder<TMessage> IAsyncSyncWithTransporterBuilder<TMessage>.IsAsync()
            {
                return this;
            }

            public IDisposable Build(MessageHandlerDelegate<TMessage> callback)
            {
                return _transporter.RegisterMessageHandler<TMessage>(callback);
            }

            public IDisposable Build(MessageWithTransporterHandlerDelegate<TMessage> callback)
            {
                return _transporter.RegisterMessageHandler<TMessage>(callback);
            }

            public IDisposable Build(AsyncMessageHandlerDelegate<TMessage> callback)
            {
                return _transporter.RegisterMessageHandler<TMessage>(callback);
            }

            public IDisposable Build(AsyncMessageWithTransporterHandlerDelegate<TMessage> callback)
            {
                return _transporter.RegisterMessageHandler<TMessage>(callback);
            }
        }

        public class ResonanceRequestResponseHandlerBuilder<TRequest> :
            ResonanceHandlerBuilder,
            IResponseTypeBuilder<TRequest>,
            IAsyncSyncWithTransporterBuilder<TRequest>,
            IAsyncWithTransporterBuilder<TRequest>
            where TRequest : class
        {
            internal ResonanceRequestResponseHandlerBuilder(IResonanceTransporter transporter) : base(transporter)
            {

            }

            public IIncludeTransporterBuilder<TRequest, TResponse> WithResponse<TResponse>() where TResponse : class
            {
                return new ResonanceRequestWithResponseHandlerBuilder<TRequest, TResponse>(_transporter);
            }

            public IAsyncWithTransporterBuilder<TRequest> IsAsync()
            {
                return this;
            }

            public IDisposable Build(MessageWithTransporterHandlerDelegate<TRequest> callback)
            {
                return _transporter.RegisterRequestHandler<TRequest>(callback);
            }

            public IDisposable Build(AsyncMessageWithTransporterHandlerDelegate<TRequest> callback)
            {
                return _transporter.RegisterRequestHandler<TRequest>(callback);
            }
        }

        public class ResonanceRequestWithResponseHandlerBuilder<TRequest, TResponse> :
            ResonanceHandlerBuilder,
            IIncludeTransporterBuilder<TRequest, TResponse>,
            IAsyncSyncWithTransporterBuilder<TRequest, TResponse>,
            IAsyncBuilder<TRequest, TResponse>,
            IAsyncWithTransporterBuilder<TRequest, TResponse>
            where TRequest : class where TResponse : class
        {
            internal ResonanceRequestWithResponseHandlerBuilder(IResonanceTransporter transporter) : base(transporter)
            {
            }

            public IAsyncSyncWithTransporterBuilder<TRequest, TResponse> IncludeTransporter()
            {
                return this;
            }

            public IAsyncBuilder<TRequest, TResponse> IsAsync()
            {
                return this;
            }

            IAsyncWithTransporterBuilder<TRequest, TResponse> IAsyncSyncWithTransporterBuilder<TRequest, TResponse>.IsAsync()
            {
                return this;
            }

            public IDisposable Build(RequestHandlerDelegate<TRequest, TResponse> callback)
            {
                return _transporter.RegisterRequestHandler<TRequest,TResponse>(callback);
            }

            public IDisposable Build(RequestWithTransporterHandlerDelegate<TRequest, TResponse> callback)
            {
                return _transporter.RegisterRequestHandler<TRequest, TResponse>(callback);
            }

            public IDisposable Build(AsyncRequestHandlerDelegate<TRequest, TResponse> callback)
            {
                return _transporter.RegisterRequestHandler<TRequest, TResponse>(callback);
            }

            public IDisposable Build(AsyncRequestWithTransporterHandlerDelegate<TRequest, TResponse> callback)
            {
                return _transporter.RegisterRequestHandler<TRequest, TResponse>(callback);
            }
        }
    }
}
