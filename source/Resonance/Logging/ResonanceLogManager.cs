using Resonance.ExtensionMethods;
using Resonance.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Logging
{
    /// <summary>
    /// Represents the Resonance library internal logs manager.
    /// </summary>
#pragma warning disable CS1573
    public class ResonanceLogManager
    {
        private readonly ProducerConsumerQueue<ResonanceLogItem> _logs;
        private Thread _loggingThread;
        private bool _isStarted;
        private static readonly Lazy<ResonanceLogManager> _default = new Lazy<ResonanceLogManager>(() => new ResonanceLogManager());

        /// <summary>
        /// Occurs when a new log as been received.
        /// </summary>
        public event EventHandler<ResonanceLogItemAvailableEventArgs> LogItemAvailable;

        /// <summary>
        /// Gets the default log manager instance.
        /// </summary>
        public static ResonanceLogManager Default
        {
            get
            {
                return _default.Value;
            }
        }

        /// <summary>
        /// Gets or sets the log level that will be processed.
        /// </summary>
        public ResonanceLogLevel LogLevel { get; set; }

        /// <summary>
        /// Initializes the <see cref="ResonanceLogManager"/> class.
        /// </summary>
        private ResonanceLogManager()
        {
            LogLevel = ResonanceLogLevel.Warning;
            _logs = new ProducerConsumerQueue<ResonanceLogItem>();
        }

        /// <summary>
        /// Returns true of the current <see cref="LogLevel"/> determines that the specified log level should be logged.
        /// </summary>
        /// <param name="level">The level.</param>
        public bool HasLevel(ResonanceLogLevel level)
        {
            return level >= LogLevel;
        }

        /// <summary>
        /// Submits a debug level log item.
        /// </summary>
        /// <param name="message">The message.</param>
        public String Debug(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, ResonanceLogLevel.Debug, caller, file, lineNumber);
            return message;
        }

        /// <summary>
        /// Submits an information level log item.
        /// </summary>
        /// <param name="message">The message.</param>
        public String Info(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, ResonanceLogLevel.Info, caller, file, lineNumber);
            return message;
        }

        /// <summary>
        /// Submits a warning level log item.
        /// </summary>
        /// <param name="message">The message.</param>
        public String Warning(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, ResonanceLogLevel.Warning, caller, file, lineNumber);
            return message;
        }

        /// <summary>
        /// Submits a warning level log item.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">Optional message.</param>
        public Exception Warning(Exception exception, String message = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return LogException(exception, ResonanceLogLevel.Warning, message, caller, file, lineNumber);
        }

        /// <summary>
        /// Submits an error level log item.
        /// </summary>
        /// <param name="message">The message.</param>
        public String Error(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, ResonanceLogLevel.Error, caller, file, lineNumber);
            return message;
        }

        /// <summary>
        /// Submits an error level log item.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">Optional message.</param>
        public Exception Error(Exception exception, String message = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return LogException(exception, ResonanceLogLevel.Error, message, caller, file, lineNumber);
        }

        /// <summary>
        /// Submits a fatal error level log item.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="message">Optional message.</param>
        public Exception Fatal(Exception exception, String message = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return LogException(exception, ResonanceLogLevel.Fatal, message, caller, file, lineNumber);
        }

        private Exception LogException(Exception exception, ResonanceLogLevel level, String description, string caller, string file, int lineNumber)
        {
            String message = String.Empty;

            if (description != null)
            {
                message += description + "\n";
            }

            message += exception.FlattenMessage();

            Log(message, level, caller, file, lineNumber);

            return exception;
        }

        private void Log(String message, ResonanceLogLevel level, String caller, String file, int lineNumber)
        {
            if (level >= LogLevel)
            {
                ResonanceLogItem log = new ResonanceLogItem();
                log.CallerFile = Path.GetFileNameWithoutExtension(file);
                log.CallerMethodName = caller;
                log.CallerLineNumber = lineNumber;
                log.TimeStamp = DateTime.Now;
                log.Level = level;
                log.Message = message;
                AppendLog(log);
            }
        }

        private void AppendLog(ResonanceLogItem log)
        {
            _logs.BlockEnqueue(log);
            StartLoggingThread();
        }

        private void StartLoggingThread()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _loggingThread = new Thread(LoggingThreadMethod);
                _loggingThread.IsBackground = true;
                _loggingThread.Start();
            }
        }

        private void LoggingThreadMethod()
        {
            while (_isStarted)
            {
                ResonanceLogItem log = _logs.BlockDequeue();
                LogItemAvailable?.Invoke(this, new ResonanceLogItemAvailableEventArgs() { LogItem = log });
            }
        }
    }
}
