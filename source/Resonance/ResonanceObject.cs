using Resonance.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Resonance
{
    public class ResonanceObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the default log manager.
        /// </summary>
        public LogManager LogManager
        {
            get { return LogManager.Default; }
        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        protected virtual void RaisePropertyChanged(String propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        protected virtual void RaisePropertyChangedAuto([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
