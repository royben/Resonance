using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance
{
    internal class ResonanceMessageHandler : IDisposable
    {
        private Action _unregisterAction;
        private bool _disposed;

        public bool HasResponse { get; set; }
        public Type MessageType { get; set; }
        public Func<IResonanceTransporter, Object, Object> Callback { get; set; }
        public Func<Object,Object[],Object> FastDelegate { get; set; }
        public Delegate RegisteredDelegate { get; set; }
        public String RegisteredDelegateDescription { get; set; }

        public ResonanceMessageHandler(Action unregisterAction)
        {
            _unregisterAction = unregisterAction;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _unregisterAction.Invoke();
                _disposed = true;
            }
        }
    }
}
