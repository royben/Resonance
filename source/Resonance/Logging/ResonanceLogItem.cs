using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Logging
{
    /// <summary>
    /// Represents a Resonance information log item.
    /// </summary>
    public class ResonanceLogItem
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
        public ResonanceLogLevel Level { get; set; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public String Message { get; set; }

        public override string ToString()
        {
            return $"[{TimeStamp.ToString("HH:mm:ss.ff")}] [{Level}] [{CallerFile}] [{CallerMethodName}] [{CallerLineNumber}]: {Message}";
        }
    }
}
