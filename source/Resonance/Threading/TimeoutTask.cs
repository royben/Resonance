using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Threading
{
    /// <summary>
    /// Represents a simple task with a delay mechanism.
    /// </summary>
    public class TimeoutTask
    {
        private Action _action;
        private TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeoutTask"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="timeout">The timeout.</param>
        public TimeoutTask(Action action, TimeSpan timeout)
        {
            _action = action;
            _timeout = timeout;
        }

        /// <summary>
        /// Starts the task after the specified timeout.
        /// </summary>
        public void Start()
        {
            Thread t = new Thread(() => 
            {
                try
                {
                    Thread.Sleep(_timeout);
                    _action();
                }
                catch (ThreadAbortException)
                {
                    //Ignore
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        /// <summary>
        /// Starts a new <see cref="TimeoutTask"/> with the specified timeout.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="timeout">The timeout.</param>
        public static void StartNew(Action action,TimeSpan timeout)
        {
            new TimeoutTask(action, timeout).Start();
        }
    }
}
