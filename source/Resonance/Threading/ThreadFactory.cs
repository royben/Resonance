using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resonance.Threading
{
    /// <summary>
    /// Represents a legacy thread helper.
    /// </summary>
    public class ThreadFactory
    {
        /// <summary>
        /// Starts a new background thread and returns that thread.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static Thread StartNew(Action action)
        {
            Thread t = new Thread(() => 
            {
                action();
            });
            t.IsBackground = true;
            t.Start();
            return t;
        }
    }
}
