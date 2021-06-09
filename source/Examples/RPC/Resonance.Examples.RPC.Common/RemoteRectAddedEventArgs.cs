using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.RPC.Common
{
    public class RemoteRectAddedEventArgs : EventArgs
    {
        public RemoteRect Rect { get; set; }
    }
}
