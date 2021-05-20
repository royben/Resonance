using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalR
{
    public interface ISignalRServer : IDisposable
    {
        void Start();
        void Stop();
    }
}
