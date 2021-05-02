using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Resonance
{
    /// <summary>
    /// Represents a basic Resonance object with <see cref="INotifyPropertyChanged"/> support.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public abstract class ResonanceObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <returns></returns>
        public event PropertyChangedEventHandler PropertyChanged;

        private ILogger _logger;
        /// <summary>
        /// Gets or creates a new ILogger instance from <see cref="ResonanceGlobalSettings.LoggerFactory"/>.
        /// </summary>
        protected ILogger Logger
        {
            get
            {
                if (_logger == null)
                {
                    lock (this)
                    {
                        if (_logger == null)
                        {
                            if (ResonanceGlobalSettings.Default.LoggerFactory != null)
                            {
                                _logger = ResonanceGlobalSettings.Default.LoggerFactory.CreateLogger(this.ToString());
                            }
                            else
                            {
                                _logger = new ResonanceDummyLogger();
                            }
                        }
                    }
                }

                return _logger;
            }
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
        protected virtual void RaisePropertyChangedAuto([CallerMemberName] string caller = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }
    }
}
