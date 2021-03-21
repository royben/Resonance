using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.ExtensionMethods
{
    /// <summary>
    /// Contains <see cref="Exception"/> exception methods.
    /// </summary>
    internal static class ExceptionExtensions
    {
        /// <summary>
        /// Flattens the exception by digging on InnerException.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public static String FlattenException(this Exception exception)
        {
            var stringBuilder = new StringBuilder();

            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                exception = exception.InnerException;
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Gets the first exception if this is an aggregated exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public static Exception GetFirstIfAggregate(this Exception exception)
        {
            var ex = exception as AggregateException;

            if (ex != null && ex.InnerExceptions.Count > 0)
            {
                return ex.InnerExceptions.First();
            }

            return exception;
        }

        /// <summary>
        /// Flattens the exception message in case it is an aggregated exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        public static String FlattenMessage(this Exception exception)
        {
            String message = exception.Message;

            if (exception is AggregateException)
            {
                try
                {
                    message = String.Join(Environment.NewLine, (exception as AggregateException).InnerExceptions.Select(x => x.FlattenMessage()));
                }
                catch { }
            }
            else if (exception.InnerException != null)
            {
                message += Environment.NewLine + exception.InnerException.FlattenMessage();
            }

            return message;
        }
    }
}