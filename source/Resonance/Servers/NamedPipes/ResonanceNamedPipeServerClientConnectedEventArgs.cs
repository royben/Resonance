using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Servers.NamedPipes
{
    public class ResonanceNamedPipeServerClientConnectedEventArgs : EventArgs
    {
        public PipeStream PipeStream { get; set; }

        public ResonanceNamedPipeServerClientConnectedEventArgs(PipeStream pipeStream)
        {
            PipeStream = pipeStream;
        }
    }
}
