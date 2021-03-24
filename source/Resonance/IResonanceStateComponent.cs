using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a Resonance component with support for state change notifications.
    /// </summary>
    public interface IResonanceStateComponent : IResonanceComponent
    {
        /// <summary>
        /// Occurs when the current state of the component has changed.
        /// </summary>
        event EventHandler<ResonanceComponentStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Gets the current state of the component.
        /// </summary>
        ResonanceComponentState State { get; }

        /// <summary>
        /// Gets the last failed state exception of this component.
        /// </summary>
        Exception FailedStateException { get; }
    }
}
