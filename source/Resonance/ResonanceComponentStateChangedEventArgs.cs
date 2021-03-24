using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents an <see cref="IResonanceStateComponent.StateChanged"/> event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class ResonanceComponentStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the previous component state.
        /// </summary>
        public ResonanceComponentState PreviousState { get; set; }

        /// <summary>
        /// Gets or sets the new component state.
        /// </summary>
        public ResonanceComponentState NewState { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResonanceComponentStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">Previous components state.</param>
        /// <param name="newState">New component state.</param>
        public ResonanceComponentStateChangedEventArgs(ResonanceComponentState previousState, ResonanceComponentState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}
