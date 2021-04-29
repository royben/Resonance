using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resonance.Examples.Common.Messages
{
    public class NotifyAvailableClientsRequest
    {
        public List<String> Clients { get; set; }

        public NotifyAvailableClientsRequest()
        {
            Clients = new List<string>();
        }
    }
}
