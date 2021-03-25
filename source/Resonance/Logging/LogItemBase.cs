using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Logging
{
    /// <summary>
    /// Represents a Resonance log item base class.
    /// </summary>
    public abstract class LogItemBase
    {
        /// <summary>
        /// Gets or sets the caller method.
        /// </summary>
        public String CallerMethodName { get; set; }

        /// <summary>
        /// Gets or sets the caller file.
        /// </summary>
        public String CallerFile { get; set; }

        /// <summary>
        /// Gets or sets the caller line number.
        /// </summary>
        public int CallerLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the DateTime for the log.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public String Message { get; set; }

        /// <summary>
        /// Gets the <see cref="ToString"/> message.
        /// </summary>
        /// <returns></returns>
        protected virtual String GetToStringMessage()
        {
            return Message;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"[{TimeStamp.ToString("HH:mm:ss.ff")}] [{Level}] [{CallerFile}] [{CallerMethodName}] [{CallerLineNumber}]: {GetToStringMessage()}";
        }
    }
}
