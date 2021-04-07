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
    /// Represents the Resonance library log manager.
    /// </summary>
#pragma warning disable CS1573
    public class LogManager
    {
        private readonly ProducerConsumerQueue<LogItem> _logs;
        private Thread _loggingThread;
        private bool _isStarted;
        private static readonly Lazy<LogManager> _default = new Lazy<LogManager>(() => new LogManager());

        /// <summary>
        /// Occurs when a new log as been received.
        /// </summary>
        public event EventHandler<LogItem> NewLog;

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
            _logs = new ProducerConsumerQueue<LogItem>();
        }

        public String Debug(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Debug, caller, file, lineNumber);
            return message;
        }

        public String Info(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Info, caller, file, lineNumber);
            return message;
        }

        public String Warning(String message, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            Log(message, LogLevel.Warning, caller, file, lineNumber);
            return message;
        }

        public Exception Warning(Exception exception, String description = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return LogException(exception, LogLevel.Warning, description, caller, file, lineNumber);
        }

        public Exception Error(Exception exception, String description = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return LogException(exception, LogLevel.Error, description, caller, file, lineNumber);
        }

        public Exception Fatal(Exception exception, String description = null, [CallerMemberName] string caller = null, [CallerFilePath] string file = null, [CallerLineNumber] int lineNumber = 0)
        {
            return LogException(exception, LogLevel.Fatal, description, caller, file, lineNumber);
        }

        private Exception LogException(Exception exception, LogLevel level, String description, string caller, string file, int lineNumber)
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

        private void Log(String message, LogLevel level, String caller, String file, int lineNumber)
        {
            LogItem log = new LogItem();
            log.CallerFile = Path.GetFileNameWithoutExtension(file);
            log.CallerMethodName = caller;
            log.CallerLineNumber = lineNumber;
            log.TimeStamp = DateTime.Now;
            log.Level = level;
            log.Message = message;
            AppendLog(log);
        }

        /// <summary>
        /// Appends the log.
        /// </summary>
        /// <param name="log">The log.</param>
        private void AppendLog(LogItem log)
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
                LogItem log = _logs.BlockDequeue();
                NewLog?.Invoke(this, log);
            }
        }
    }
}
