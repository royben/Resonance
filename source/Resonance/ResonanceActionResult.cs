using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance request handler response.
    /// </summary>
    public class ResonanceActionResult : IResonanceActionResult
    {
        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public object Response { get; set; }

        /// <summary>
        /// Gets the response configuration.
        /// </summary>
        public ResonanceResponseConfig Config { get; set; }
    }

    /// <summary>
    /// Represents a Resonance request handler response.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="Resonance.IResonanceActionResult" />
    public class ResonanceActionResult<T> : ResonanceActionResult, IResonanceActionResult where T : class
    {
        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        public new T Response
        {
            get { return (this as IResonanceActionResult).Response as T; }
            set { (this as IResonanceActionResult).Response = value; }
        }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        object IResonanceActionResult.Response { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceActionResult{T}"/> class.
        /// </summary>
        /// <param name="response">The response message.</param>
        public ResonanceActionResult(T response) : base()
        {
            Response = response;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceActionResult{T}"/> class.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="config">The response configuration.</param>
        public ResonanceActionResult(T response, ResonanceResponseConfig config) : this(response)
        {
            Config = config;
        }

        /// <summary>
        /// Performs an implicit conversion from T" to <see cref="ResonanceActionResult{T}"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator ResonanceActionResult<T>(T message)
        {
            return new ResonanceActionResult<T>(message);
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="ResonanceActionResult{T}"/> to T.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator T(ResonanceActionResult<T> instance)
        {
            return instance.Response;
        }
    }
}
