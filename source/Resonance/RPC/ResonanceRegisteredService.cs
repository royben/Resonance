using System;
using System.Collections.Generic;
using System.Text;

namespace Resonance.RPC
{
    internal class ResonanceRegisteredService
    {
        public Type InterfaceType { get; set; }

        public Type ServiceType { get; set; }

        public Object Service { get; set; }

        public RpcServiceCreationType CreationType { get; set; }

        public Func<Object> CreationFunc { get; set; }

        public IResonanceTransporter Transporter { get; set; }

        public List<Action> EventHandlersDisposeActions { get; set; }

        public ResonanceRegisteredService()
        {
            EventHandlersDisposeActions = new List<Action>();
        }
    }
}
