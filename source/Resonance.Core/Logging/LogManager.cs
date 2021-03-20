using Resonance.Core.Threading;
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

namespace Resonance.Core.Logging
{
    /// <summary>
    /// Represents logging manager for logging information and errors.
    /// </summary>
    public class LogManager
    {
        private ProducerConsumerQueue<LogItemBase> _logs;
        private Thread _loggingThread;
        private bool _isStarted;
        private static Lazy<LogManager> _default = new Lazy<LogManager>(() => new LogManager());

        /// <summary>
        /// Occurs when a new log as been received.
        /// </summary>
        public event EventHandler<LogItemBase> NewLog;

        /// <summary>
        /// Gets the default log manager instance.
        /// </summary>
        public static LogManager Default
        {
            get
            {
                return _default.Value;
            }
        }

        /// <summary>
        /// Initializes the <see cref="LogManager"/> class.
        /// </summary>
        private LogManager()
        {
            _logs = new ProducerConsumerQueue<LogItemBase>();
        }

        /// <summary>
        /// Add new exception log item.
        /// </summary>
        /// <param name="e">Exception.</param>
        /// <param name="description">Error description.</param>
        public Exception Log(Exception e, String description = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return Log(e, LogLevel.Error, description, caller, file, lineNumber);
        }

        /// <summary>
        /// Add new exception log item.
        /// </summary>
        /// <param name="e">Exception.</param>
        /// <param name="description">Error description.</param>
        public Exception Log(Exception e, LogLevel level, String description = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            ExceptionLogItem log = new ExceptionLogItem();
            log.CallerMethodName = caller;
            log.CallerFile = file;
            log.CallerLineNumber = lineNumber;
            log.TimeStamp = DateTime.Now;
            log.Exception = e;
            log.Level = level;
            log.Message = description;

            AppendLog(log);

            return e;
        }

        /// <summary>
        /// Add new message log item.
        /// </summary>
        /// <param name="message">Message.</param>
        public String Log(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return Log(message, LogLevel.Info, caller, file, lineNumber);
        }

        /// <summary>
        /// Add new message log item.
        /// </summary>
        /// <param name="message">Message.</param>
        public String Log(String message, LogLevel level, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            MessageLogItem log = new MessageLogItem();
            log.CallerMethodName = caller;
            log.CallerFile = file;
            log.CallerLineNumber = lineNumber;
            log.TimeStamp = DateTime.Now;
            log.Level = level;
            log.Message = message;

            AppendLog(log);

            return message;
        }

        /// <summary>
        /// Appends the log.
        /// </summary>
        /// <param name="log">The log.</param>
        private void AppendLog(LogItemBase log)
        {
            _logs.BlockEnqueue(log);
            StartLoggingThread();
        }

        /// <summary>
        /// Starts the logging thread.
        /// </summary>
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

        /// <summary>
        /// Loggings thread method.
        /// </summary>
        private void LoggingThreadMethod()
        {
            while (_isStarted)
            {
                LogItemBase log = _logs.BlockDequeue();
                NewLog?.Invoke(this, log);
            }
        }
    }
}
