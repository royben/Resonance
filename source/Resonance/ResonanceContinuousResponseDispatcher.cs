using Resonance.Threading;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Resonance
{
    /// <summary>
    /// Represents a continuous request response messages dispatcher.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class ResonanceContinuousResponseDispatcher : IDisposable
    {
        private Thread _thread;
        private ProducerConsumerQueue<Action> _actions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceContinuousResponseDispatcher"/> class.
        /// </summary>
        public ResonanceContinuousResponseDispatcher()
        {
            _actions = new ProducerConsumerQueue<Action>();
        }

        /// <summary>
        /// Enqueues the specified action.
        /// </summary>
        /// <param name="action">The action.</param>
        public void Enqueue(Action action)
        {
            if (_thread == null)
            {
                _thread = new Thread(DispatchMessages);
                _thread.IsBackground = true;
                _thread.Start();
            }

            _actions.BlockEnqueue(action);
        }

        private void DispatchMessages()
        {
            while (true)
            {
                var action = _actions.BlockDequeue();
                if (action == null)
                {
                    return;
                }

                action();
            }
        }

        /// <summary>
        /// Terminates the dispatcher thread.
        /// </summary>
        public void Dispose()
        {
            _actions.BlockEnqueue(null);
        }
    }
}
