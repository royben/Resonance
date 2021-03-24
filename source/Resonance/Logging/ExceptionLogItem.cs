using Resonance.ExtensionMethods;
using System;

namespace Resonance.Logging
{
    /// <summary>
    /// Represents a Resonance exception log item.
    /// </summary>
    public class ExceptionLogItem : LogItemBase
    {
        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Gets the <see cref="M:Resonance.Logging.LogItemBase.ToString" /> message.
        /// </summary>
        /// <returns></returns>
        protected override string GetToStringMessage()
        {
            return $"{Message}\n{Exception.FlattenException()}";
        }
    }
}
