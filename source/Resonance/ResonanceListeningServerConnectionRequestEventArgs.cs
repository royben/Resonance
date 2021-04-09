using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a generic <see cref="IResonanceListeningServer{TAdapter}.ConnectionRequest"/> event arguments.
    /// </summary>
    /// <typeparam name="TAdapter">The type of the adapter.</typeparam>
    public class ResonanceListeningServerConnectionRequestEventArgs<TAdapter> where TAdapter : IResonanceAdapter
    {
        private Func<TAdapter> _acceptFunc;
        private Action _declineAction;

        /// <summary>
        /// Approves the connection request and return an initialized adapter.
        /// </summary>
        /// <returns></returns>
        public TAdapter Accept()
        {
            return _acceptFunc.Invoke();
        }

        /// <summary>
        /// Declines the connection request.
        /// </summary>
        public void Decline()
        {
            _declineAction.Invoke();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceListeningServerConnectionRequestEventArgs{TAdapter}"/> class.
        /// </summary>
        /// <param name="acceptFunc">The accept function.</param>
        /// <param name="declineAction">The decline action.</param>
        public ResonanceListeningServerConnectionRequestEventArgs(Func<TAdapter> acceptFunc,Action declineAction)
        {
            _acceptFunc = acceptFunc;
            _declineAction = declineAction;
        }
    }
}
