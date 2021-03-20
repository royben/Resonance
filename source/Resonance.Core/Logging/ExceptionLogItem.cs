using Resonance.Core.ExtensionMethods;
using System;

namespace Resonance.Core.Logging
{
    /// <summary>
    /// Represents an exception log item.
    /// </summary>
    public class ExceptionLogItem : LogItemBase
    {
        public Exception Exception { get; set; }

        protected override string GetToStringMessage()
        {
            return $"{Message}\n{Exception.FlattenException()}";
        }
    }
}
