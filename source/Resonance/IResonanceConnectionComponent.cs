using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Resonance
{
    public interface IResonanceConnectionComponent
    {
        Task Connect();
        Task Disconnect();
    }
}
