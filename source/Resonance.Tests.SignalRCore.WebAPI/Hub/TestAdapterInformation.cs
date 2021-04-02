using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Tests.SignalRCore.WebAPI.Hub
{
    public class TestAdapterInformation
    {
        public String Information { get; set; } //Nothing.

        public TestAdapterInformation()
        {
            Information = "No information on the remote adapter";
        }
    }
}
